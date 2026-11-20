using AriasEmpireERP.Data;
using AriasEmpireERP.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AriasEmpireERP.Services;

public sealed class AuditService(IDbContextFactory<ApplicationDbContext> factory)
{
    public async Task RecordAsync(string userId, string userName, string role, string action, string entity, string detail)
    {
        await using var db = await factory.CreateDbContextAsync();
        db.AuditLogs.Add(new AuditLog { UserId = userId, UserName = userName, Role = role, Action = action, EntityName = entity, Detail = detail });
        await db.SaveChangesAsync();
    }

    public async Task<List<AuditLog>> RecentAsync(int count = 250)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.AuditLogs.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Take(count).ToListAsync();
    }
}
