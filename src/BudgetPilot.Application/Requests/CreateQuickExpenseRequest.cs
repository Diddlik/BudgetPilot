namespace BudgetPilot.Application.Requests;

public sealed record CreateQuickExpenseRequest(
    Guid RequestId,
    decimal Amount,
    Guid CategoryId,
    Guid? BudgetItemId = null,
    DateOnly? Date = null,
    string? Description = null);
