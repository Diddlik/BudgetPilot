using BudgetPilot.Application.Abstractions;
using BudgetPilot.Application.Dtos;
using BudgetPilot.Domain.Entities;
using BudgetPilot.Domain.Enums;
using BudgetPilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BudgetPilot.Infrastructure.Auditing;

/// <summary>
/// Schreibt Protokolleinträge in dieselbe Datenbank. Das Schreiben ist „best effort":
/// schlägt es fehl, wird nur gewarnt – die bereits gespeicherte fachliche Aktion bleibt gültig.
/// </summary>
public sealed class AuditLog : IAuditLog
{
    private readonly BudgetPilotDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<AuditLog> _logger;

    public AuditLog(BudgetPilotDbContext db, ICurrentUser currentUser, ILogger<AuditLog> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task RecordAsync(
        AuditAction action,
        string entityType,
        Guid entityId,
        string entityName,
        string? details = null,
        CancellationToken ct = default)
    {
        try
        {
            var actor = await _currentUser.GetAsync(ct).ConfigureAwait(false);
            _db.Set<AuditEntry>().Add(new AuditEntry
            {
                Id = Guid.NewGuid(),
                TimestampUtc = DateTime.UtcNow,
                UserName = actor.DisplayName,
                UserId = actor.UserId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                EntityName = entityName,
                Details = details
            });
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Audit-Eintrag konnte nicht geschrieben werden ({Action} {EntityType} {EntityId}).",
                action, entityType, entityId);
        }
    }

    public async Task<IReadOnlyList<AuditEntryDto>> GetRecentAsync(int max = 200, CancellationToken ct = default)
    {
        return await _db.Set<AuditEntry>()
            .AsNoTracking()
            .OrderByDescending(e => e.TimestampUtc)
            .Take(max)
            .Select(e => new AuditEntryDto(
                e.Id, e.TimestampUtc, e.UserName, e.Action, e.EntityType, e.EntityId, e.EntityName, e.Details))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
