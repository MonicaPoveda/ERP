using AriasEmpireERP.Data;
using AriasEmpireERP.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AriasEmpireERP.Services;

public sealed record InventoryStockDto(int Id, string Sku, string Name, int StockCurrent, int StockMinimum);

public sealed class InventarioService(IDbContextFactory<ApplicationDbContext> factory, AuditService audit)
{
    public async Task<List<InventoryStockDto>> ProductsAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.InventoryProducts.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new InventoryStockDto(x.Id, x.Sku, x.Name, x.StockCurrent, x.StockMinimum)).ToListAsync();
    }

    public async Task<List<InventoryMovement>> KardexAsync(int productId, DateTime? from, DateTime? to)
    {
        await using var db = await factory.CreateDbContextAsync();
        var query = db.InventoryMovements.AsNoTracking().Where(x => x.ProductId == productId);
        if (from.HasValue) query = query.Where(x => x.CreatedAtUtc >= from.Value);
        if (to.HasValue) query = query.Where(x => x.CreatedAtUtc < to.Value.AddDays(1));
        return await query.OrderByDescending(x => x.CreatedAtUtc).ToListAsync();
    }

    public async Task RecordMovementAsync(int productId, int quantity, bool isEntry, string reference, string userId, string userName)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "La cantidad debe ser mayor que cero.");
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var product = await db.InventoryProducts.SingleAsync(x => x.Id == productId);
        if (isEntry)
        {
            var approvedOrder = await db.PurchaseOrders.AnyAsync(x => x.Number == reference && x.Status == "Aprobada");
            if (!approvedOrder) throw new InvalidOperationException("La entrada requiere una orden de compra aprobada.");
            product.StockCurrent = checked(product.StockCurrent + quantity);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(reference)) throw new InvalidOperationException("Indique el número del acta de salida.");
            if (quantity > product.StockCurrent) throw new InvalidOperationException("No hay existencias suficientes.");
            product.StockCurrent -= quantity;
        }
        db.InventoryMovements.Add(new InventoryMovement { ProductId = productId, Quantity = quantity, Type = isEntry ? "Entrada" : "Salida", Reference = reference, BalanceAfter = product.StockCurrent });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        await audit.RecordAsync(userId, userName, "Almacenista", "Crear", nameof(InventoryMovement), $"{product.Sku}: {quantity} { (isEntry ? "Entrada" : "Salida") }");
    }
}
