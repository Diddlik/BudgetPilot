namespace BudgetPilot.Application.Dtos;

public sealed record CategoryPlanActualDto(
    Guid CategoryId,
    string CategoryName,
    decimal PlannedExpense,
    decimal ActualExpense,
    decimal ExpenseVariance);

public sealed record PlanActualComparisonDto(
    int Year,
    int? Month,
    decimal PlannedIncome,
    decimal ActualIncome,
    decimal IncomeVariance,
    decimal PlannedExpense,
    decimal ActualExpense,
    decimal ExpenseVariance,
    decimal PlannedBalance,
    decimal ActualBalance,
    decimal BalanceVariance,
    IReadOnlyList<CategoryPlanActualDto> Categories);
