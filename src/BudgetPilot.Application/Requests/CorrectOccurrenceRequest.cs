namespace BudgetPilot.Application.Requests;

public sealed record CorrectOccurrenceRequest(
    decimal Amount,
    DateOnly Date,
    string? Description = null);
