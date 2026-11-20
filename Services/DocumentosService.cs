using AriasEmpireERP.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AriasEmpireERP.Services;

public sealed class DocumentosService(IDbContextFactory<ApplicationDbContext> factory, PdfReceiptService pdf)
{
    public async Task<byte[]> InvoiceAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var invoice = await db.Invoices.Include(x => x.BusinessDeal).SingleOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("Factura no encontrada.");
        var company = await db.CompanySettings.AsNoTracking().FirstOrDefaultAsync();
        return pdf.Create("FACTURA DE VENTA", invoice.Number, invoice.IssuedAtUtc,
        [
            new("Razón social", company?.LegalName ?? "Arias Empire"),
            new("NIT", company?.TaxId ?? ""),
            new("Cliente", invoice.BusinessDeal?.Name ?? ""),
            new("Inmueble", invoice.BusinessDeal?.PropertyInterest ?? ""),
            new("Subtotal", PdfReceiptService.Currency(invoice.Subtotal)),
            new("IVA", PdfReceiptService.Currency(invoice.Vat)),
            new("Retención", PdfReceiptService.Currency(invoice.Withholding)),
            new("Total neto", PdfReceiptService.Currency(invoice.Total)),
            new("Saldo pendiente", PdfReceiptService.Currency(invoice.Balance))
        ]);
    }

    public async Task<byte[]> InvoicePaymentAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var payment = await db.InvoicePayments.Include(x => x.Invoice).ThenInclude(x => x!.BusinessDeal)
            .SingleOrDefaultAsync(x => x.Id == id) ?? throw new KeyNotFoundException("Pago de factura no encontrado.");
        return pdf.Create("RECIBO DE PAGO DE FACTURA", $"RPF-{payment.Id:D6}", payment.PaidAtUtc,
        [
            new("Factura", payment.Invoice?.Number ?? ""),
            new("Cliente", payment.Invoice?.BusinessDeal?.Name ?? ""),
            new("Valor recibido", PdfReceiptService.Currency(payment.Amount)),
            new("Saldo actual", PdfReceiptService.Currency(payment.Invoice?.Balance ?? 0)),
            new("Referencia", $"Pago {payment.Id:D6}")
        ]);
    }

    public async Task<byte[]> InvoiceAdjustmentAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var adjustment = await db.InvoiceAdjustments.Include(x => x.Invoice).ThenInclude(x => x!.BusinessDeal)
            .SingleOrDefaultAsync(x => x.Id == id) ?? throw new KeyNotFoundException("Nota de ajuste no encontrada.");
        return pdf.Create(adjustment.Type.ToUpperInvariant(), $"N-{adjustment.Id:D6}", adjustment.CreatedAtUtc,
        [
            new("Factura relacionada", adjustment.Invoice?.Number ?? ""),
            new("Cliente", adjustment.Invoice?.BusinessDeal?.Name ?? ""),
            new("Valor", PdfReceiptService.Currency(adjustment.Amount)),
            new("Detalle", adjustment.Detail),
            new("Saldo actual", PdfReceiptService.Currency(adjustment.Invoice?.Balance ?? 0))
        ]);
    }

    public async Task<byte[]> InventoryMovementAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var movement = await db.InventoryMovements.Include(x => x.Product).SingleOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("Movimiento de inventario no encontrado.");
        var isEntry = movement.Type is "Entrada" or "Ajuste inicial";
        return pdf.Create(isEntry ? "COMPROBANTE DE ENTRADA DE PRODUCTOS" : "ACTA DE SALIDA DE PRODUCTOS", $"INV-{movement.Id:D6}", movement.CreatedAtUtc,
        [
            new("SKU", movement.Product?.Sku ?? ""),
            new("Producto", movement.Product?.Name ?? ""),
            new("Tipo de movimiento", movement.Type),
            new("Cantidad", movement.Quantity.ToString(CultureInfo.InvariantCulture)),
            new("Documento de referencia", movement.Reference),
            new("Saldo resultante", movement.BalanceAfter.ToString(CultureInfo.InvariantCulture))
        ]);
    }

    public async Task<byte[]> ExpensePaymentAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var payment = await db.ExpensePayments.Include(x => x.ExpenseRequest).SingleOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("Pago de egreso no encontrado.");
        return pdf.Create("COMPROBANTE DE PAGO DE EGRESO", $"EGR-{payment.Id:D6}", payment.PaidAtUtc,
        [
            new("Concepto", payment.ExpenseRequest?.Description ?? ""),
            new("Valor pagado", PdfReceiptService.Currency(payment.Amount)),
            new("Saldo pendiente", PdfReceiptService.Currency(payment.BalanceAfter)),
            new("Referencia", payment.Reference)
        ]);
    }

    public async Task<byte[]> PayrollPaymentAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var payment = await db.PayrollPayments.Include(x => x.Employee).SingleOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("Comprobante de nómina no encontrado.");
        return pdf.Create("COMPROBANTE DE PAGO DE NÓMINA", $"NOM-{payment.Id:D6}", payment.PaidAtUtc,
        [
            new("Empleado", payment.Employee?.FullName ?? ""),
            new("Identificación", payment.Employee?.NationalId ?? ""),
            new("Cargo", payment.Employee?.Position ?? ""),
            new("Periodo", $"{payment.PeriodStart:yyyy-MM-dd} · {payment.PeriodDays} días"),
            new("Días trabajados", payment.DaysWorked.ToString(CultureInfo.InvariantCulture)),
            new("Salario base", PdfReceiptService.Currency(payment.BaseSalary)),
            new("Devengado", PdfReceiptService.Currency(payment.GrossAmount)),
            new("Días de incapacidad", payment.IncapacityDays.ToString(CultureInfo.InvariantCulture)),
            new("Descuentos", PdfReceiptService.Currency(payment.DeductionAmount)),
            new("Neto pagado", PdfReceiptService.Currency(payment.NetAmount)),
            new("Referencia", payment.Reference)
        ]);
    }
}
