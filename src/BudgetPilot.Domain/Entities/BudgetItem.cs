using BudgetPilot.Domain.Enums;

namespace BudgetPilot.Domain.Entities;

/// <summary>
/// Fachliche Hauptposition (z. B. Miete, Gehalt). Trägt nur langlebige Stammdaten;
/// Betrag/Frequenz/Gültigkeit leben in den <see cref="Versions"/>.
/// </summary>
public class BudgetItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Optionaler Inhaber/Person (z. B. "Mann", "Frau", "Gemeinsam") für Auswertung pro Person.</summary>
    public string? Owner { get; set; }

    public BudgetItemType Type { get; set; }
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<BudgetItemVersion> Versions { get; set; } = new();
}
