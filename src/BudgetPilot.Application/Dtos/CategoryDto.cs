namespace BudgetPilot.Application.Dtos;

/// <summary>Kategorie inkl. Anzahl zugeordneter Positionen (für Kategorie-Karten & Drilldown).</summary>
public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Anzahl zugeordneter BudgetItems (aktiv + inaktiv).</summary>
    public int ItemCount { get; set; }
}
