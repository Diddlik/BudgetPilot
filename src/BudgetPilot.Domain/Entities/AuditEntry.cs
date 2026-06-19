using BudgetPilot.Domain.Enums;

namespace BudgetPilot.Domain.Entities;

/// <summary>
/// Ein Eintrag im Änderungsprotokoll: hält fest, wer wann welche Entität wie geändert hat.
/// Der Benutzername wird denormalisiert gespeichert, damit das Protokoll lesbar bleibt,
/// auch wenn das zugehörige Konto später gelöscht wird.
/// </summary>
public class AuditEntry
{
    public Guid Id { get; set; }

    /// <summary>Zeitpunkt der Änderung (UTC; Anzeige in lokaler Zeit).</summary>
    public DateTime TimestampUtc { get; set; }

    /// <summary>Anzeigename (E-Mail) des Akteurs; "System" bei Vorgängen ohne angemeldeten Benutzer.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Identity-UserId, falls vorhanden (für spätere Auswertungen).</summary>
    public string? UserId { get; set; }

    public AuditAction Action { get; set; }

    /// <summary>Logischer Typ der betroffenen Entität, z. B. "BudgetItem", "Category", "User".</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Id der betroffenen Entität (kann bei Benutzern leer sein).</summary>
    public Guid EntityId { get; set; }

    /// <summary>Sprechender Name der Entität zum Zeitpunkt der Änderung (z. B. "Miete").</summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>Optionaler, menschenlesbarer Detailtext, z. B. "1.200,00 € → 1.250,00 €".</summary>
    public string? Details { get; set; }
}
