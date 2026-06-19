using BudgetPilot.Application.Dtos;
using BudgetPilot.Domain.Enums;

namespace BudgetPilot.Application.Abstractions;

/// <summary>
/// Schreibt und liest das Änderungsprotokoll. Das Schreiben ist „best effort" und ermittelt
/// den Akteur selbst über <see cref="ICurrentUser"/>; Aufrufer geben nur fachliche Angaben.
/// </summary>
public interface IAuditLog
{
    Task RecordAsync(
        AuditAction action,
        string entityType,
        Guid entityId,
        string entityName,
        string? details = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<AuditEntryDto>> GetRecentAsync(int max = 200, CancellationToken ct = default);
}
