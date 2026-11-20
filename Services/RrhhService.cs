using AriasEmpireERP.Data;
using AriasEmpireERP.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AriasEmpireERP.Services;

public sealed record PayrollEstimate(decimal GrossAmount, int IncapacityDays, decimal DeductionAmount, decimal NetAmount);

public sealed class RrhhService(IDbContextFactory<ApplicationDbContext> factory, AuditService audit)
{
    public async Task<List<Employee>> EmployeesAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Employees.AsNoTracking().OrderBy(x => x.FullName).ToListAsync();
    }

    public async Task<List<HrRequest>> RequestsAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.HrRequests.Include(x => x.Employee).AsNoTracking().OrderByDescending(x => x.Id).ToListAsync();
    }

    public async Task<List<Employee>> ContractAlertsAsync()
    {
        var until = DateTime.Today.AddDays(30);
        await using var db = await factory.CreateDbContextAsync();
        return await db.Employees.AsNoTracking().Where(x => x.IsActive && x.ContractEndDate.Date <= until && x.ContractEndDate.Date >= DateTime.Today).ToListAsync();
    }

    public async Task SaveEmployeeAsync(Employee employee, string userId, string userName)
    {
        await using var db = await factory.CreateDbContextAsync();
        var isNew = employee.Id == 0;
        if (isNew) db.Employees.Add(employee); else db.Employees.Update(employee);
        await db.SaveChangesAsync();
        await audit.RecordAsync(userId, userName, "RRHH", isNew ? "Crear" : "Editar", nameof(Employee), employee.FullName);
    }

    public async Task DeleteEmployeeAsync(int id, string userId, string userName)
    {
        await using var db = await factory.CreateDbContextAsync();
        var employee = await db.Employees.FindAsync(id) ?? throw new InvalidOperationException("Empleado no encontrado.");
        employee.IsActive = false;
        await db.SaveChangesAsync();
        await audit.RecordAsync(userId, userName, "RRHH", "Editar", nameof(Employee), "Baja: " + employee.FullName);
    }

    public async Task ReviewRequestAsync(int id, bool approve, string userId, string userName)
    {
        await using var db = await factory.CreateDbContextAsync();
        var request = await db.HrRequests.FindAsync(id) ?? throw new InvalidOperationException("Solicitud no encontrada.");
        if (request.Status != "Pendiente") throw new InvalidOperationException("La solicitud ya fue resuelta.");
        request.Status = approve ? "Aprobada" : "Rechazada";
        await db.SaveChangesAsync();
        await audit.RecordAsync(userId, userName, "RRHH", approve ? "Aprobar" : "Rechazar", nameof(HrRequest), request.Type);
    }

    public async Task<List<PayrollPayment>> PayrollPaymentsAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.PayrollPayments.Include(x => x.Employee).AsNoTracking().OrderByDescending(x => x.PaidAtUtc).ToListAsync();
    }

    public async Task<PayrollEstimate> PayrollEstimateAsync(int employeeId, int daysWorked, DateTime periodStart, int periodDays)
    {
        await using var db = await factory.CreateDbContextAsync();
        var employee = await db.Employees.SingleAsync(x => x.Id == employeeId);
        return await CalculatePayrollAsync(db, employee, daysWorked, periodStart, periodDays);
    }

    public async Task<PayrollPayment> RegisterPayrollPaymentAsync(int employeeId, int daysWorked, DateTime periodStart, int periodDays, string userId, string userName)
    {
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var employee = await db.Employees.SingleAsync(x => x.Id == employeeId && x.IsActive);
        var existingPayment = await db.PayrollPayments.AnyAsync(x => x.EmployeeId == employeeId && x.PeriodStart == periodStart.Date && x.PeriodDays == periodDays);
        if (existingPayment) throw new InvalidOperationException("Ya existe un pago de nómina para este empleado y periodo.");
        var estimate = await CalculatePayrollAsync(db, employee, daysWorked, periodStart, periodDays);
        var payment = new PayrollPayment
        {
            EmployeeId = employee.Id,
            PeriodStart = periodStart.Date,
            PeriodDays = periodDays,
            DaysWorked = daysWorked,
            BaseSalary = employee.BaseSalary,
            GrossAmount = estimate.GrossAmount,
            IncapacityDays = estimate.IncapacityDays,
            DeductionAmount = estimate.DeductionAmount,
            NetAmount = estimate.NetAmount,
            Reference = "NOM-PENDING-" + Guid.NewGuid().ToString("N")
        };
        db.PayrollPayments.Add(payment);
        await db.SaveChangesAsync();
        payment.Reference = $"NOM-{payment.Id:D6}";
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        await audit.RecordAsync(userId, userName, "RRHH", "Crear", nameof(PayrollPayment), $"{payment.Reference}: {employee.FullName} {payment.NetAmount:C}");
        return payment;
    }

    private static async Task<PayrollEstimate> CalculatePayrollAsync(ApplicationDbContext db, Employee employee, int daysWorked, DateTime periodStart, int periodDays)
    {
        if (periodDays is not (15 or 30)) throw new ArgumentOutOfRangeException(nameof(periodDays), "El periodo debe ser quincenal o mensual.");
        if (daysWorked < 0 || daysWorked > periodDays) throw new ArgumentOutOfRangeException(nameof(daysWorked), "Los días trabajados deben estar dentro del periodo.");
        var periodEnd = periodStart.Date.AddDays(periodDays - 1);
        var leaves = await db.HrRequests.AsNoTracking()
            .Where(x => x.EmployeeId == employee.Id && x.Type == "Incapacidad" && x.Status == "Aprobada" && x.StartDate <= periodEnd && x.EndDate >= periodStart.Date)
            .Select(x => new { x.StartDate, x.EndDate })
            .ToListAsync();
        var unpaidLeave = leaves.Sum(x =>
        {
            var overlapStart = x.StartDate.Date > periodStart.Date ? x.StartDate.Date : periodStart.Date;
            var overlapEnd = x.EndDate.Date < periodEnd ? x.EndDate.Date : periodEnd;
            return (overlapEnd - overlapStart).Days + 1;
        });
        var gross = decimal.Round(employee.BaseSalary / 30m * daysWorked, 2);
        var incapacityDays = Math.Min(unpaidLeave, daysWorked);
        var deduction = decimal.Round(employee.BaseSalary / 30m * incapacityDays, 2);
        return new PayrollEstimate(gross, incapacityDays, deduction, Math.Max(0, gross - deduction));
    }
}
