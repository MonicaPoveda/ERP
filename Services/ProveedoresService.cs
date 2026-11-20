using AriasEmpireERP.Data;
using AriasEmpireERP.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AriasEmpireERP.Services;

public sealed class ProveedoresService(IDbContextFactory<ApplicationDbContext> factory, AuditService audit)
{
    public async Task<List<Supplier>> ListAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Suppliers.Include(x => x.Evaluations).AsNoTracking().OrderBy(x => x.LegalName).ToListAsync();
    }

    public async Task SaveAsync(Supplier supplier, string userId, string userName)
    {
        await using var db = await factory.CreateDbContextAsync();
        var isNew = supplier.Id == 0;
        if (isNew) db.Suppliers.Add(supplier);
        else db.Suppliers.Update(supplier);
        await db.SaveChangesAsync();
        await audit.RecordAsync(userId, userName, "CoordinadorProveedores", isNew ? "Crear" : "Editar", nameof(Supplier), supplier.LegalName);
    }

    public async Task DeleteAsync(int id, string userId, string userName)
    {
        await using var db = await factory.CreateDbContextAsync();
        var supplier = await db.Suppliers.FindAsync(id) ?? throw new InvalidOperationException("Proveedor no encontrado.");
        db.Suppliers.Remove(supplier);
        await db.SaveChangesAsync();
        await audit.RecordAsync(userId, userName, "CoordinadorProveedores", "Eliminar", nameof(Supplier), supplier.LegalName);
    }

    public async Task EvaluateAsync(int supplierId, int score, string observation, string userId, string userName)
    {
        if (score is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(score), "La calificación debe estar entre 1 y 5.");
        await using var db = await factory.CreateDbContextAsync();
        var supplier = await db.Suppliers.Include(x => x.Evaluations).SingleAsync(x => x.Id == supplierId);
        supplier.Evaluations.Add(new SupplierEvaluation { Score = score, Observation = observation });
        supplier.EvaluationCount = supplier.Evaluations.Count;
        supplier.AverageScore = (decimal)supplier.Evaluations.Average(x => x.Score);
        await db.SaveChangesAsync();
        await audit.RecordAsync(userId, userName, "CoordinadorProveedores", "Crear", nameof(SupplierEvaluation), supplier.LegalName + ": " + score);
    }
}
