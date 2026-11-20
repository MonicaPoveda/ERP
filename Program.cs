using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AriasEmpireERP.Components;
using AriasEmpireERP.Components.Account;
using AriasEmpireERP.Data;
using AriasEmpireERP.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Administracion", policy => policy.RequireRole("SuperAdmin"));
    options.AddPolicy("Comercial", policy => policy.RequireRole("AsesorComercial", "DirectorComercial"));
    options.AddPolicy("Finanzas", policy => policy.RequireRole("Tesorero"));
    options.AddPolicy("RrhhContracts", policy => policy.RequireRole("RRHH"));
    options.AddPolicy("Inventario", policy => policy.RequireRole("Almacenista"));
});
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<ProveedoresService>();
builder.Services.AddScoped<FinanzasService>();
builder.Services.AddScoped<RrhhService>();
builder.Services.AddScoped<InventarioService>();
builder.Services.AddScoped<ComercialService>();
builder.Services.AddScoped<DocumentosService>();
builder.Services.AddSingleton<PdfReceiptService>();
builder.Services.AddSingleton<DbInitializer>();

var app = builder.Build();

await app.Services.GetRequiredService<DbInitializer>().InitializeAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapGet("/documentos/facturas/{id:int}/pdf", async (int id, DocumentosService documents) =>
    await DownloadPdfAsync(() => documents.InvoiceAsync(id), $"Factura-{id:D6}.pdf"))
    .RequireAuthorization("Finanzas");
app.MapGet("/documentos/pagos-factura/{id:int}/pdf", async (int id, DocumentosService documents) =>
    await DownloadPdfAsync(() => documents.InvoicePaymentAsync(id), $"Recibo-pago-{id:D6}.pdf"))
    .RequireAuthorization("Finanzas");
app.MapGet("/documentos/notas-factura/{id:int}/pdf", async (int id, DocumentosService documents) =>
    await DownloadPdfAsync(() => documents.InvoiceAdjustmentAsync(id), $"Nota-factura-{id:D6}.pdf"))
    .RequireAuthorization("Finanzas");
app.MapGet("/documentos/movimientos-inventario/{id:int}/pdf", async (int id, DocumentosService documents) =>
    await DownloadPdfAsync(() => documents.InventoryMovementAsync(id), $"Movimiento-inventario-{id:D6}.pdf"))
    .RequireAuthorization("Inventario");
app.MapGet("/documentos/pagos-egreso/{id:int}/pdf", async (int id, DocumentosService documents) =>
    await DownloadPdfAsync(() => documents.ExpensePaymentAsync(id), $"Recibo-egreso-{id:D6}.pdf"))
    .RequireAuthorization("Finanzas");
app.MapGet("/documentos/pagos-nomina/{id:int}/pdf", async (int id, DocumentosService documents) =>
    await DownloadPdfAsync(() => documents.PayrollPaymentAsync(id), $"Comprobante-nomina-{id:D6}.pdf"))
    .RequireAuthorization("RrhhContracts");

app.MapGet("/rrhh/contracts/{fileName}", (string fileName, IWebHostEnvironment environment) =>
{
    if (Path.GetFileName(fileName) != fileName || !string.Equals(Path.GetExtension(fileName), ".pdf", StringComparison.OrdinalIgnoreCase))
        return Results.NotFound();
    var filePath = Path.Combine(environment.WebRootPath, "uploads", "contratos", fileName);
    return File.Exists(filePath) ? Results.File(filePath, "application/pdf", fileDownloadName: fileName) : Results.NotFound();
}).RequireAuthorization("RrhhContracts");

app.MapGet("/company/logo/{fileName}", (string fileName, IWebHostEnvironment environment) =>
{
    if (Path.GetFileName(fileName) != fileName || !new[] { ".png", ".jpg", ".jpeg", ".webp" }.Contains(Path.GetExtension(fileName).ToLowerInvariant()))
        return Results.NotFound();
    var filePath = Path.Combine(environment.WebRootPath, "uploads", "logos", fileName);
    var contentType = Path.GetExtension(fileName).ToLowerInvariant() switch { ".png" => "image/png", ".jpg" or ".jpeg" => "image/jpeg", _ => "image/webp" };
    return File.Exists(filePath) ? Results.File(filePath, contentType) : Results.NotFound();
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();

static async Task<IResult> DownloadPdfAsync(Func<Task<byte[]>> createPdf, string fileName)
{
    try
    {
        return Results.File(await createPdf(), "application/pdf", fileDownloadName: fileName);
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
}
