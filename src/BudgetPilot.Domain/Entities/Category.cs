namespace BudgetPilot.Domain.Entities;

/// <summary>Gruppierung von Budgetpositionen (z. B. Wohnen, Energie, Einkommen).</summary>
public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<BudgetItem> BudgetItems { get; set; } = new();
}
