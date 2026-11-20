using AriasEmpireERP.Data;
using AriasEmpireERP.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AriasEmpireERP.Services;

public sealed record AdvisorMetric(string AdvisorId, decimal Sold, int Pipeline, int Closed, int Total);

public sealed class ComercialService(IDbContextFactory<ApplicationDbContext> factory, AuditService audit)
{
    public async Task<List<BusinessDeal>> DealsAsync(string advisorId, bool isDirector)
    {
        await using var db = await factory.CreateDbContextAsync();
        var query = db.BusinessDeals.AsNoTracking().AsQueryable();
        if (!isDirector) query = query.Where(x => x.AdvisorId == advisorId);
        return await query.OrderByDescending(x => x.CreatedAtUtc).ToListAsync();
    }

    public async Task SaveDealAsync(BusinessDeal deal, string currentAdvisorId, bool isDirector, string userId, string userName)
    {
        await using var db = await factory.CreateDbContextAsync();
        var isNew = deal.Id == 0;
        if (isNew)
        {
            if (!isDirector) deal.AdvisorId = currentAdvisorId;
            if (!string.IsNullOrWhiteSpace(deal.DiscountRequest)) deal.DiscountStatus = "Pendiente";
            if (deal.Stage == "Cierre") deal.ClosedAtUtc = DateTime.UtcNow;
            db.BusinessDeals.Add(deal);
        }
        else
        {
            var existing = await db.BusinessDeals.SingleAsync(x => x.Id == deal.Id && (isDirector || x.AdvisorId == currentAdvisorId));
            existing.Name = deal.Name;
            existing.Phone = deal.Phone;
            existing.PropertyInterest = deal.PropertyInterest;
            existing.Value = deal.Value;
            existing.NextAppointment = deal.NextAppointment;
            if (existing.Stage != "Cierre" && deal.Stage == "Cierre") existing.ClosedAtUtc = DateTime.UtcNow;
            else if (deal.Stage != "Cierre") existing.ClosedAtUtc = null;
            existing.Stage = deal.Stage;
            if (isDirector) existing.AdvisorId = deal.AdvisorId;
            else if (deal.DiscountRequest != existing.DiscountRequest)
            {
                existing.DiscountRequest = deal.DiscountRequest;
                existing.DiscountStatus = string.IsNullOrWhiteSpace(deal.DiscountRequest) ? "No solicitado" : "Pendiente";
            }
        }
        if (deal.Stage is not ("Contacto" or "Negociación" or "Cierre"))
            throw new ArgumentException("Etapa no válida.", nameof(deal));
        await db.SaveChangesAsync();
        await audit.RecordAsync(userId, userName, isDirector ? "DirectorComercial" : "AsesorComercial", isNew ? "Crear" : "Editar", nameof(BusinessDeal), deal.Name);
    }

    public async Task DeleteDealAsync(int id, string advisorId, string userId, string userName)
    {
        await using var db = await factory.CreateDbContextAsync();
        var deal = await db.BusinessDeals.SingleAsync(x => x.Id == id && x.AdvisorId == advisorId);
        db.BusinessDeals.Remove(deal);
        await db.SaveChangesAsync();
        await audit.RecordAsync(userId, userName, "AsesorComercial", "Eliminar", nameof(BusinessDeal), deal.Name);
    }

    public async Task SetStageAsync(int id, string stage, string advisorId, bool isDirector, string userId, string userName)
    {
        if (stage is not ("Contacto" or "Negociación" or "Cierre")) throw new ArgumentException("Etapa no válida.", nameof(stage));
        await using var db = await factory.CreateDbContextAsync();
        var deal = await db.BusinessDeals.SingleAsync(x => x.Id == id && (isDirector || x.AdvisorId == advisorId));
        deal.Stage = stage;
        deal.ClosedAtUtc = stage == "Cierre" ? deal.ClosedAtUtc ?? DateTime.UtcNow : null;
        await db.SaveChangesAsync();
        await audit.RecordAsync(userId, userName, isDirector ? "DirectorComercial" : "AsesorComercial", "Editar", nameof(BusinessDeal), $"{deal.Name}: {stage}");
    }

    public async Task<List<CrmNote>> NotesAsync(int dealId, string advisorId, bool isDirector)
    {
        await using var db = await factory.CreateDbContextAsync();
        var authorized = await db.BusinessDeals.AnyAsync(x => x.Id == dealId && (isDirector || x.AdvisorId == advisorId));
        if (!authorized) throw new UnauthorizedAccessException("No tiene acceso a este prospecto.");
        return await db.CrmNotes.AsNoTracking().Where(x => x.BusinessDealId == dealId).OrderByDescending(x => x.CreatedAtUtc).ToListAsync();
    }

    public async Task AddNoteAsync(int dealId, string text, string advisorId, bool isDirector, string userId, string userName)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("La nota no puede estar vacía.", nameof(text));
        await using var db = await factory.CreateDbContextAsync();
        var deal = await db.BusinessDeals.SingleAsync(x => x.Id == dealId && (isDirector || x.AdvisorId == advisorId));
        db.CrmNotes.Add(new CrmNote { BusinessDealId = deal.Id, Text = text, AuthorId = userId });
        await db.SaveChangesAsync();
        await audit.RecordAsync(userId, userName, isDirector ? "DirectorComercial" : "AsesorComercial", "Crear", nameof(CrmNote), deal.Name);
    }

    public async Task DecideDiscountAsync(int id, bool approve, string userId, string userName)
    {
        await using var db = await factory.CreateDbContextAsync();
        var deal = await db.BusinessDeals.SingleAsync(x => x.Id == id && x.Stage == "Negociación" && x.DiscountRequest != null && x.DiscountStatus == "Pendiente");
        deal.DiscountStatus = approve ? "Aprobado" : "Rechazado";
        await db.SaveChangesAsync();
        await audit.RecordAsync(userId, userName, "DirectorComercial", approve ? "Aprobar" : "Rechazar", nameof(BusinessDeal), "Descuento " + deal.Name);
    }

    public async Task<List<AdvisorMetric>> TeamMetricsAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.BusinessDeals.AsNoTracking().GroupBy(x => x.AdvisorId)
            .Select(g => new AdvisorMetric(g.Key, g.Where(x => x.Stage == "Cierre").Sum(x => x.Value), g.Count(x => x.Stage != "Cierre"), g.Count(x => x.Stage == "Cierre"), g.Count()))
            .ToListAsync();
    }
}
