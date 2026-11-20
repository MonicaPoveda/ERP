using AriasEmpireERP.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AriasEmpireERP.Data;

public sealed class DbInitializer(IServiceScopeFactory scopeFactory, ILogger<DbInitializer> logger)
{
    private static readonly string[] Roles =
    [
        "SuperAdmin", "CoordinadorProveedores", "Tesorero", "RRHH",
        "Almacenista", "AsesorComercial", "DirectorComercial"
    ];

    public async Task InitializeAsync()
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        await db.BusinessDeals
            .Where(x => x.Stage == "Cierre" && x.ClosedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ClosedAtUtc, x => x.CreatedAtUtc));

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                EnsureSuccess(result, $"crear el rol {role}");
            }
        }

        var accounts = new (string Email, string Name, string Role)[]
        {
            ("admin@ariasempire.com", "Administrador", "SuperAdmin"),
            ("proveedores@ariasempire.com", "Coordinador de Proveedores", "CoordinadorProveedores"),
            ("tesoreria@ariasempire.com", "Tesorería", "Tesorero"),
            ("rrhh@ariasempire.com", "Recursos Humanos", "RRHH"),
            ("almacen@ariasempire.com", "Almacén", "Almacenista"),
            ("asesor1@ariasempire.com", "Asesor Uno", "AsesorComercial"),
            ("asesor2@ariasempire.com", "Asesor Dos", "AsesorComercial"),
            ("director@ariasempire.com", "Director Comercial", "DirectorComercial")
        };
        foreach (var account in accounts)
        {
            var user = await userManager.FindByEmailAsync(account.Email);
            if (user is null)
            {
                user = new ApplicationUser { UserName = account.Email, Email = account.Email, EmailConfirmed = true };
                var result = await userManager.CreateAsync(user, "Arias2026*");
                EnsureSuccess(result, $"crear el usuario {account.Email}");
            }
            if (!await userManager.IsInRoleAsync(user, account.Role))
            {
                var result = await userManager.AddToRoleAsync(user, account.Role);
                EnsureSuccess(result, $"asignar el rol {account.Role} a {account.Email}");
            }
        }

        if (!await db.CompanySettings.AnyAsync())
            db.CompanySettings.Add(new CompanySetting { LegalName = "Arias Empire", TaxId = "900123456-7", Address = "Bogotá D.C., Colombia" });

        if (!await db.Suppliers.AnyAsync())
        {
            db.Suppliers.Add(new Supplier
            {
                LegalName = "Servicios Integrales Andinos S.A.S.", TaxId = "901234567-8",
                ContactName = "Laura Méndez", Phone = "+57 601 555 0180", Email =                 "contacto@andinos.example",
                Services = "Mantenimiento y adecuaciones"
            });
        }

        if (!await db.BusinessDeals.AnyAsync())
        {
            var advisorOne = await userManager.FindByEmailAsync("asesor1@ariasempire.com");
            var advisorTwo = await userManager.FindByEmailAsync("asesor2@ariasempire.com");
            db.BusinessDeals.AddRange(
                new BusinessDeal { Name = "Cliente Inicial", Phone = "+57 300 555 0101", PropertyInterest = "Apartamento 302", Value = 420000000m, Stage = "Cierre", AdvisorId = advisorOne!.Id, ClosedAtUtc = DateTime.UtcNow },
                new BusinessDeal { Name = "Prospecto Empresarial", Phone = "+57 300 555 0102", PropertyInterest = "Oficina Centro", Value = 280000000m, Stage = "Negociación", AdvisorId = advisorTwo!.Id, DiscountRequest = "Solicitud inicial", DiscountStatus = "Pendiente" });
        }

        if (!await db.PurchaseOrders.AnyAsync())
            db.PurchaseOrders.Add(new PurchaseOrder { Number = "OC-INICIAL-001", Description = "Dotación inicial", Amount = 1200000m, Status = "Aprobada" });

        if (!await db.InventoryProducts.AnyAsync())
            db.InventoryProducts.AddRange(
                new InventoryProduct { Sku = "ASEO-001", Name = "Kit de aseo", StockCurrent = 24, StockMinimum = 8 },
                new InventoryProduct { Sku = "PAP-001", Name = "Resma de papel", StockCurrent = 5, StockMinimum = 10 });

        if (!await db.Employees.AnyAsync())
            db.Employees.Add(new Employee
            {
                NationalId = "1020304050", FullName = "María Fernanda Arias", Position = "Asistente administrativa",
                BaseSalary = 2600000m, BirthDate = new DateTime(1992, 5, 12), ContractEndDate = DateTime.Today.AddDays(20),
                BankAccount = "****1234"
            });

        if (!await db.ExpenseRequests.AnyAsync())
            db.ExpenseRequests.Add(new ExpenseRequest { Description = "Servicios públicos sede principal", Amount = 680000m });

        await db.SaveChangesAsync();

        if (!await db.SupplierEvaluations.AnyAsync())
        {
            var supplier = await db.Suppliers.FirstAsync();
            db.SupplierEvaluations.Add(new SupplierEvaluation { SupplierId = supplier.Id, Score = 4, Observation = "Cumplió los tiempos y el alcance acordado." });
            supplier.AverageScore = 4;
            supplier.EvaluationCount = 1;
        }

        if (!await db.CrmNotes.AnyAsync())
        {
            var deal = await db.BusinessDeals.FirstOrDefaultAsync();
            var noteAuthor = await userManager.FindByEmailAsync(accounts[5].Email);
            if (deal is not null && noteAuthor is not null) db.CrmNotes.Add(new CrmNote { BusinessDealId = deal.Id, Text = "Se confirmó el interés y se registró el seguimiento inicial.", AuthorId = noteAuthor.Id });
        }

        if (!await db.Invoices.AnyAsync())
        {
            var deal = await db.BusinessDeals.FirstOrDefaultAsync(x => x.Stage == "Cierre");
            if (deal is not null)
            {
                var subtotal = deal.Value;
                var vat = decimal.Round(subtotal * 0.19m, 2);
                var company = await db.CompanySettings.SingleAsync();
                var withholding = decimal.Round(subtotal * company.WithholdingRate, 2);
                db.Invoices.Add(new Invoice
                {
                    BusinessDealId = deal.Id, Number = "FE-INICIAL-001", Subtotal = subtotal,
                    Vat = vat, Withholding = withholding, Total = subtotal + vat - withholding,
                    Balance = subtotal + vat - withholding
                });
            }
        }

        if (!await db.HrRequests.AnyAsync())
        {
            var employee = await db.Employees.FirstOrDefaultAsync();
            if (employee is not null)
                db.HrRequests.Add(new HrRequest { EmployeeId = employee.Id, Type = "Vacaciones", StartDate = DateTime.Today.AddDays(45), EndDate = DateTime.Today.AddDays(49) });
        }

        if (!await db.InventoryMovements.AnyAsync())
        {
            var products = await db.InventoryProducts.ToListAsync();
            db.InventoryMovements.AddRange(products.Select(x => new InventoryMovement
            {
                ProductId = x.Id, Type = "Ajuste inicial", Quantity = x.StockCurrent,
                Reference = "SALDO-INICIAL", BalanceAfter = x.StockCurrent
            }));
        }

        if (!await db.AuditLogs.AnyAsync())
        {
            db.AuditLogs.Add(new AuditLog
            {
                UserName = "Sistema", Role = "System", Action = "Crear",
                EntityName = "DbInitializer", Detail = "Datos iniciales de la aplicación verificados."
            });
        }
        await db.SaveChangesAsync();

        var seededInvoice = await db.Invoices.FirstOrDefaultAsync();
        if (seededInvoice is not null && !await db.InvoicePayments.AnyAsync())
        {
            var paymentAmount = Math.Min(5_000_000m, seededInvoice.Balance);
            if (paymentAmount > 0)
            {
                seededInvoice.Balance -= paymentAmount;
                db.InvoicePayments.Add(new InvoicePayment { InvoiceId = seededInvoice.Id, Amount = paymentAmount });
            }
        }
        if (seededInvoice is not null && !await db.InvoiceAdjustments.AnyAsync())
        {
            var creditAmount = Math.Min(250_000m, seededInvoice.Balance);
            if (creditAmount > 0)
            {
                seededInvoice.Balance -= creditAmount;
                seededInvoice.Total -= creditAmount;
                db.InvoiceAdjustments.Add(new InvoiceAdjustment { InvoiceId = seededInvoice.Id, Type = "Nota crédito", Amount = creditAmount, Detail = "Ajuste inicial de facturación." });
            }
        }
        await db.SaveChangesAsync();
        logger.LogInformation("Roles, cuentas y datos iniciales de Arias Empire ERP verificados.");
    }

    private static void EnsureSuccess(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException($"No se pudo {operation}: {string.Join("; ", result.Errors.Select(x => x.Description))}");
    }
}
