using BudgetPilot.Domain.Enums;

namespace BudgetPilot.Application.Dtos;

public sealed record ActualTransactionDto(
    Guid Id,
    DateOnly Date,
    decimal Amount,
    BudgetItemType Type,
    Guid CategoryId,
    string CategoryName,
    Guid? BudgetItemId,
    string? Description);
