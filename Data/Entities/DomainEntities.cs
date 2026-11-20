namespace AriasEmpireERP.Data.Entities;

public class Supplier
{
    public int Id { get; set; }
    public string LegalName { get; set; } = "";
    public string TaxId { get; set; } = "";
    public string ContactName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Services { get; set; } = "";
    public decimal AverageScore { get; set; }
    public int EvaluationCount { get; set; }
    public ICollection<SupplierEvaluation> Evaluations { get; set; } = new List<SupplierEvaluation>();
}

public class SupplierEvaluation
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public int Score { get; set; }
    public string Observation { get; set; } = "";
    public DateTime EvaluatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class BusinessDeal
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string PropertyInterest { get; set; } = "";
    public decimal Value { get; set; }
    public DateTime? NextAppointment { get; set; }
    public string Stage { get; set; } = "Contacto";
    public string AdvisorId { get; set; } = "";
    public string? DiscountRequest { get; set; }
    public string DiscountStatus { get; set; } = "No solicitado";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAtUtc { get; set; }
    public ICollection<CrmNote> Notes { get; set; } = new List<CrmNote>();
}

public class CrmNote
{
    public int Id { get; set; }
    public int BusinessDealId { get; set; }
    public BusinessDeal? BusinessDeal { get; set; }
    public string Text { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string AuthorId { get; set; } = "";
}

public class ExpenseRequest
{
    public int Id { get; set; }
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public string Status { get; set; } = "Pendiente";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<ExpensePayment> Payments { get; set; } = new List<ExpensePayment>();
}

public class ExpensePayment
{
    public int Id { get; set; }
    public int ExpenseRequestId { get; set; }
    public ExpenseRequest? ExpenseRequest { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Reference { get; set; } = "";
    public DateTime PaidAtUtc { get; set; } = DateTime.UtcNow;
}

public class Invoice
{
    public int Id { get; set; }
    public int BusinessDealId { get; set; }
    public BusinessDeal? BusinessDeal { get; set; }
    public string Number { get; set; } = "";
    public decimal Subtotal { get; set; }
    public decimal Vat { get; set; }
    public decimal Withholding { get; set; }
    public decimal Total { get; set; }
    public decimal Balance { get; set; }
    public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;
}

public class InvoicePayment
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAtUtc { get; set; } = DateTime.UtcNow;
}

public class InvoiceAdjustment
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public string Type { get; set; } = "Nota crédito";
    public decimal Amount { get; set; }
    public string Detail { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class PurchaseOrder
{
    public int Id { get; set; }
    public string Number { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Aprobada";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class Employee
{
    public int Id { get; set; }
    public string NationalId { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Position { get; set; } = "";
    public decimal BaseSalary { get; set; }
    public DateTime BirthDate { get; set; }
    public DateTime ContractEndDate { get; set; }
    public string BankAccount { get; set; } = "";
    public string? ContractPath { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PayrollPayment
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateTime PeriodStart { get; set; }
    public int PeriodDays { get; set; }
    public int DaysWorked { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal GrossAmount { get; set; }
    public int IncapacityDays { get; set; }
    public decimal DeductionAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string Reference { get; set; } = "";
    public DateTime PaidAtUtc { get; set; } = DateTime.UtcNow;
}

public class HrRequest
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string Type { get; set; } = "Vacaciones";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "Pendiente";
}

public class InventoryProduct
{
    public int Id { get; set; }
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public int StockCurrent { get; set; }
    public int StockMinimum { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public class InventoryMovement
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public InventoryProduct? Product { get; set; }
    public string Type { get; set; } = "";
    public int Quantity { get; set; }
    public string Reference { get; set; } = "";
    public int BalanceAfter { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class CompanySetting
{
    public int Id { get; set; }
    public string LegalName { get; set; } = "Arias Empire ERP";
    public string TaxId { get; set; } = "";
    public string Address { get; set; } = "";
    public string? LogoPath { get; set; }
    public decimal MonthlyCommissionGoal { get; set; } = 5000000m;
    public decimal CommissionRate { get; set; } = 0.02m;
    public decimal WithholdingRate { get; set; } = 0.025m;
}

public class AuditLog
{
    public long Id { get; set; }
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string Role { get; set; } = "";
    public string Action { get; set; } = "";
    public string EntityName { get; set; } = "";
    public string Detail { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
