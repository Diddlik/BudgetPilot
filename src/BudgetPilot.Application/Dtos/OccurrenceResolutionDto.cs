using BudgetPilot.Domain.Enums;

namespace BudgetPilot.Application.Dtos;

public sealed record OccurrenceResolutionDto(
    string OccurrenceKey,
    OccurrenceResolutionKind Kind,
    Guid BudgetItemId,
    string BudgetItemName,
    string CategoryName,
    DateOnly PlannedDueDate,
    decimal PlannedAmount,
    Guid? ActualTransactionId,
    DateOnly? ActualDate,
    decimal? ActualAmount,
    string? Note);
