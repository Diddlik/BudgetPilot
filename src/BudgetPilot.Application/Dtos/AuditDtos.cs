using BudgetPilot.Domain.Enums;

namespace BudgetPilot.Application.Dtos;

/// <summary>Ein Protokolleintrag für die Anzeige (Aktivitäts-/Änderungsprotokoll).</summary>
public sealed record AuditEntryDto(
    Guid Id,
    DateTime TimestampUtc,
    string UserName,
    AuditAction Action,
    string EntityType,
    Guid EntityId,
    string EntityName,
    string? Details);
