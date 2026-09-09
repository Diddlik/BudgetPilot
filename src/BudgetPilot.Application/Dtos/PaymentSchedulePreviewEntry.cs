namespace BudgetPilot.Application.Dtos;

public sealed record PaymentSchedulePreviewEntry(DateOnly DueDate, decimal Amount);
