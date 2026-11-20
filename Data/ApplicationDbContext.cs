using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AriasEmpireERP.Data.Entities;

namespace AriasEmpireERP.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<SupplierEvaluation> SupplierEvaluations => Set<SupplierEvaluation>();
    public DbSet<BusinessDeal> BusinessDeals => Set<BusinessDeal>();
    public DbSet<CrmNote> CrmNotes => Set<CrmNote>();
    public DbSet<ExpenseRequest> ExpenseRequests => Set<ExpenseRequest>();
    public DbSet<ExpensePayment> ExpensePayments => Set<ExpensePayment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoicePayment> InvoicePayments => Set<InvoicePayment>();
    public DbSet<InvoiceAdjustment> InvoiceAdjustments => Set<InvoiceAdjustment>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<PayrollPayment> PayrollPayments => Set<PayrollPayment>();
    public DbSet<HrRequest> HrRequests => Set<HrRequest>();
    public DbSet<InventoryProduct> InventoryProducts => Set<InventoryProduct>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<CompanySetting> CompanySettings => Set<CompanySetting>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Supplier>().Property(x => x.AverageScore).HasPrecision(4, 2);
        builder.Entity<BusinessDeal>().Property(x => x.Value).HasPrecision(18, 2);
        builder.Entity<ExpenseRequest>().Property(x => x.Amount).HasPrecision(18, 2);
        builder.Entity<ExpenseRequest>().Property(x => x.PaidAmount).HasPrecision(18, 2);
        builder.Entity<ExpensePayment>().Property(x => x.Amount).HasPrecision(18, 2);
        builder.Entity<ExpensePayment>().Property(x => x.BalanceAfter).HasPrecision(18, 2);
        builder.Entity<Invoice>().Property(x => x.Subtotal).HasPrecision(18, 2);
        builder.Entity<Invoice>().Property(x => x.Vat).HasPrecision(18, 2);
        builder.Entity<Invoice>().Property(x => x.Withholding).HasPrecision(18, 2);
        builder.Entity<Invoice>().Property(x => x.Total).HasPrecision(18, 2);
        builder.Entity<Invoice>().Property(x => x.Balance).HasPrecision(18, 2);
        builder.Entity<InvoiceAdjustment>().Property(x => x.Amount).HasPrecision(18, 2);
        builder.Entity<InvoicePayment>().Property(x => x.Amount).HasPrecision(18, 2);
        builder.Entity<PurchaseOrder>().Property(x => x.Amount).HasPrecision(18, 2);
        builder.Entity<Employee>().Property(x => x.BaseSalary).HasPrecision(18, 2);
        builder.Entity<PayrollPayment>().Property(x => x.BaseSalary).HasPrecision(18, 2);
        builder.Entity<PayrollPayment>().Property(x => x.GrossAmount).HasPrecision(18, 2);
        builder.Entity<PayrollPayment>().Property(x => x.DeductionAmount).HasPrecision(18, 2);
        builder.Entity<PayrollPayment>().Property(x => x.NetAmount).HasPrecision(18, 2);
        builder.Entity<CompanySetting>().Property(x => x.MonthlyCommissionGoal).HasPrecision(18, 2);
        builder.Entity<CompanySetting>().Property(x => x.CommissionRate).HasPrecision(6, 4);
        builder.Entity<CompanySetting>().Property(x => x.WithholdingRate).HasPrecision(6, 4).HasDefaultValue(0.025m);
        builder.Entity<Supplier>().HasMany(x => x.Evaluations).WithOne(x => x.Supplier).HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<BusinessDeal>().HasMany(x => x.Notes).WithOne(x => x.BusinessDeal).HasForeignKey(x => x.BusinessDealId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Invoice>().HasOne(x => x.BusinessDeal).WithMany().HasForeignKey(x => x.BusinessDealId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<InvoiceAdjustment>().HasOne(x => x.Invoice).WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ExpenseRequest>().HasMany(x => x.Payments).WithOne(x => x.ExpenseRequest).HasForeignKey(x => x.ExpenseRequestId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<HrRequest>().HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<PayrollPayment>().HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<InventoryMovement>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<InventoryProduct>().HasIndex(x => x.Sku).IsUnique();
        builder.Entity<InventoryProduct>().Property(x => x.RowVersion).IsRowVersion();
        builder.Entity<Employee>().HasIndex(x => x.NationalId).IsUnique();
        builder.Entity<Invoice>().HasIndex(x => x.Number).IsUnique();
        builder.Entity<PurchaseOrder>().HasIndex(x => x.Number).IsUnique();
        builder.Entity<PayrollPayment>().HasIndex(x => new { x.EmployeeId, x.PeriodStart, x.PeriodDays }).IsUnique();
    }
}
