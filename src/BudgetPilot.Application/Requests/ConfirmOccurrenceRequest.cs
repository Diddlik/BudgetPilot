namespace BudgetPilot.Application.Requests;

public sealed record ConfirmOccurrenceRequest(
    decimal? Amount = null,
    DateOnly? Date = null,
    string? Description = null);
