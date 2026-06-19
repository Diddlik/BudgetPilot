using BudgetPilot.Application.Abstractions;
using BudgetPilot.Application.Dtos;
using BudgetPilot.Domain.Enums;

namespace BudgetPilot.Integration.Tests;

/// <summary>Test-Double: protokolliert nichts (Audit ist nicht Gegenstand dieser Tests).</summary>
public sealed class NoopAuditLog : IAuditLog
{
    public Task RecordAsync(AuditAction action, string entityType, Guid entityId, string entityName,
        string? details = null, CancellationToken ct = default) => Task.CompletedTask;

    public Task<IReadOnlyList<AuditEntryDto>> GetRecentAsync(int max = 200, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<AuditEntryDto>>(Array.Empty<AuditEntryDto>());
}
