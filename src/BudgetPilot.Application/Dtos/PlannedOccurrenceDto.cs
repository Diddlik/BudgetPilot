using BudgetPilot.Domain.Enums;

namespace BudgetPilot.Application.Dtos;

public sealed record PlannedOccurrenceDto(
    string OccurrenceKey,
    Guid BudgetItemId,
    string BudgetItemName,
    Guid CategoryId,
    string CategoryName,
    BudgetItemType Type,
    DateOnly DueDate,
    decimal PlannedAmount);
