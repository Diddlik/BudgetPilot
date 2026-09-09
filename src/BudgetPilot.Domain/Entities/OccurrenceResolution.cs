using BudgetPilot.Domain.Enums;

namespace BudgetPilot.Domain.Entities;

public class OccurrenceResolution
{
    public Guid Id { get; set; }
    public string OccurrenceKey { get; set; } = string.Empty;
    public Guid BudgetItemId { get; set; }
    public string BudgetItemName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public DateOnly PlannedDueDate { get; set; }
    public decimal PlannedAmount { get; set; }
    public OccurrenceResolutionKind Kind { get; set; }
    public string? Note { get; set; }
    public Guid? ActualTransactionId { get; set; }
    public ActualTransaction? ActualTransaction { get; set; }
    public DateTime CreatedAt { get; set; }
}
