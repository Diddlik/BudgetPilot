using BudgetPilot.Domain.Enums;

namespace BudgetPilot.Domain.Entities;

/// <summary>
/// Tatsächliche Buchung für den späteren Plan/Ist-Vergleich. Im MVP nur als Tabelle
/// vorbereitet — keine UI erforderlich.
/// </summary>
public class ActualTransaction
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public BudgetItemType Type { get; set; }
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public Guid? BudgetItemId { get; set; }
    public BudgetItem? BudgetItem { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}
