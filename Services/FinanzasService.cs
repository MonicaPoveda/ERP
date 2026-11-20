using AriasEmpireERP.Data;
using AriasEmpireERP.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AriasEmpireERP.Services;

public sealed record FinanceSummary(decimal Receivable, decimal Payable, decimal NetCash);

public sealed class FinanzasService(IDbContextFactory<ApplicationDbContext> factory, AuditService audit)
{
    public async Task<FinanceSummary> SummaryAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        var receivable = await db.Invoices.SumAsync(x => (decimal?)x.Balance) ?? 0;
        var payable = await db.ExpenseRequests
            .Where(x => x.Status == "Aprobada" || x.Status == "Pago parcial")
            .SumAsync(x => (decimal?)(x.Amount - x.PaidAmount)) ?? 0;
        var collected = await db.InvoicePayments.SumAsync(x => (decimal?)x.Amount) ?? 0;
        var expensesPaid = await db.ExpensePayments.SumAsync(x => (decimal?)x.Amount) ?? 0;
        var payrollPaid = await db.PayrollPayments.SumAsync(x => (decimal?)x.NetAmount) ?? 0;
        return new FinanceSummary(receivable, payable, collected - expensesPaid - payrollPaid);
    }

    public async Task<List<ExpenseRequest>> ExpensesAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.ExpenseRequests.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).ToListAsync();
    }

    public async Task<List<BusinessDeal>> ClosedDealsAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.BusinessDeals.AsNoTracking().Where(x => x.Stage == "Cierre").OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<List<Invoice>> InvoicesAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Invoices.Include(x => x.BusinessDeal).AsNoTracking().OrderByDescending(x => x.IssuedAtUtc).ToListAsync();
    }

    public async Task<List<InvoicePayment>> InvoicePaymentsAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.InvoicePayments.Include(x => x.Invoice).AsNoTracking().OrderByDescending(x => x.PaidAtUtc).ToListAsync();
    }

    public async Task<List<InvoiceAdjustment>> InvoiceAdjustmentsAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.InvoiceAdjustments.Include(x => x.Invoice).AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).ToListAsync();
    }

    public async Task ReviewExpenseAsync(int id, bool approve, string userId, string userName)
    {
        await using var db = await factory.CreateDbContextAsync();
        var expense = await db.ExpenseRequests.FindAsync(id) ?? throw new InvalidOperationException("Solicitud de gasto no encontrada.");
        if (expense.Status != "Pendiente") throw new InvalidOperationException("La solicitud ya fue resuelta.");
        expense.Status = approve ? "Aprobada" : "Rechazada";
        await db.SaveChangesAsync();
        await audit.RecordAsync(userId, userName, "Tesorero", approve ? "Aprobar" : "Rechazar", nameof(ExpenseRequest), expense.Description);
    }

    public async Task<Invoice> CreateInvoiceAsync(int dealId, string userId, string userName)
    {
        await using var db = await factory.CreateDbContextAsync();
        var deal = await db.BusinessDeals.SingleAsync(x => x.Id == dealId && x.Stage == "Cierre");
        if (await db.Invoices.AnyAsync(x => x.BusinessDealId == dealId)) throw new InvalidOperationException("Este negocio ya tiene factura.");
        var company = await db.CompanySettings.AsNoTracking().SingleOrDefaultAsync() ?? throw new InvalidOperationException("Configure los datos de la empresa antes de facturar.");
        var subtotal = deal.Value;
        var vat = decimal.Round(subtotal * 0.19m, 2);
        var withholding = decimal.Round(subtotal * company.WithholdingRate, 2);
        var invoice = new Invoice { BusinessDealId = dealId, Number = "FE-" + DateTime.UtcNow.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(), Subtotal = subtotal, Vat = vat, Withholding = withholding, Total = subtotal + vat - withholding, Balance = subtotal + vat - withholding };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        await audit.RecordAsync(userId, userName, "Tesorero", "Crear", nameof(Invoice), invoice.Number);
        return invoice;
    }

    public async Task<InvoicePayment> RegisterPaymentAsync(int invoiceId, decimal amount, string userId, string userName)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "El pago debe ser mayor que cero.");
        await using var db = await factory.CreateDbContextAsync();
        var invoice = await db.Invoices.SingleAsync(x => x.Id == invoiceId);
        if (amount > invoice.Balance) throw new InvalidOperationException("El pago excede el saldo de la factura.");
        invoice.Balance -= amount;
        var payment = new InvoicePayment { InvoiceId = invoiceId, Amount = amount };
        db.InvoicePayments.Add(payment);
        await db.SaveChangesAsync();
        await audit.RecordAsync(userId, userName, "Tesorero", "Crear", nameof(InvoicePayment), $"{invoice.Number}: {amount:C}");
        return payment;
    }

    public async Task<InvoiceAdjustment> CreateAdjustmentAsync(int invoiceId, string type, decimal amount, string detail, string userId, string userName)
    {
        if (type is not ("Nota crédito" or "Nota débito")) throw new ArgumentException("Tipo de nota no válido.", nameof(type));
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "El valor debe ser mayor que cero.");
        await using var db = await factory.CreateDbContextAsync();
        var invoice = await db.Invoices.SingleAsync(x => x.Id == invoiceId);
        if (type == "Nota crédito" && amount > invoice.Balance) throw new InvalidOperationException("La nota crédito no puede superar el saldo pendiente.");
        invoice.Balance += type == "Nota crédito" ? -amount : amount;
        invoice.Total += type == "Nota crédito" ? -amount : amount;
        var adjustment = new InvoiceAdjustment { InvoiceId = invoiceId, Type = type, Amount = amount, Detail = detail };
        db.InvoiceAdjustments.Add(adjustment);
        await db.SaveChangesAsync();
        await audit.RecordAsync(userId, userName, "Tesorero", "Crear", nameof(InvoiceAdjustment), $"{invoice.Number}: {type} {amount:C}");
        return adjustment;
    }

    public async Task<ExpensePayment> RegisterExpensePaymentAsync(int expenseId, decimal amount, string userId, string userName)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "El pago debe ser mayor que cero.");
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var expense = await db.ExpenseRequests.SingleAsync(x => x.Id == expenseId);
        if (expense.Status is not ("Aprobada" or "Pago parcial"))
            throw new InvalidOperationException("Solo se pueden pagar solicitudes aprobadas.");
        var balance = expense.Amount - expense.PaidAmount;
        if (amount > balance) throw new InvalidOperationException("El pago excede el saldo pendiente del egreso.");
        expense.PaidAmount += amount;
        var payment = new ExpensePayment
        {
            ExpenseRequestId = expense.Id,
            Amount = amount,
            BalanceAfter = expense.Amount - expense.PaidAmount,
            Reference = "EG-PENDING-" + Guid.NewGuid().ToString("N")
        };
        expense.Status = payment.BalanceAfter == 0 ? "Pagada" : "Pago parcial";
        db.ExpensePayments.Add(payment);
        await db.SaveChangesAsync();
        payment.Reference = $"EG-{payment.Id:D6}";
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        await audit.RecordAsync(userId, userName, "Tesorero", "Crear", nameof(ExpensePayment), $"{payment.Reference}: {expense.Description} {amount:C}");
        return payment;
    }

    public async Task<PurchaseOrder> CreatePurchaseOrderAsync(string description, decimal amount, string userId, string userName)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var order = new PurchaseOrder { Number = "OC-PENDING-" + Guid.NewGuid().ToString("N"), Description = description, Amount = amount, Status = "Aprobada" };
        db.PurchaseOrders.Add(order);
        await db.SaveChangesAsync();
        order.Number = $"OC-{order.Id:D6}";
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        await audit.RecordAsync(userId, userName, "Tesorero", "Crear", nameof(PurchaseOrder), order.Number);
        return order;
    }
}
