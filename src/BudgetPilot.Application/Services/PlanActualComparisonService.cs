using BudgetPilot.Application.Abstractions;
using BudgetPilot.Application.Dtos;
using BudgetPilot.Domain.Enums;
using BudgetPilot.Domain.Exceptions;

namespace BudgetPilot.Application.Services;

public interface IPlanActualComparisonService
{
    Task<PlanActualComparisonDto> GetMonthlyAsync(
        int year,
        int month,
        CancellationToken ct = default);

    Task<PlanActualComparisonDto> GetYearlyAsync(
        int year,
        CancellationToken ct = default);
}

public sealed class PlanActualComparisonService(
    IBudgetProjectionService projections,
    IActualTransactionStore actualTransactions) : IPlanActualComparisonService
{
    public async Task<PlanActualComparisonDto> GetMonthlyAsync(
        int year,
        int month,
        CancellationToken ct = default)
    {
        if (month is < 1 or > 12)
        {
            throw new DomainException("Der Monat muss zwischen 1 und 12 liegen.");
        }

        var from = new DateOnly(year, month, 1);
        var to = from.AddMonths(1).AddDays(-1);
        var plan = await projections.GetMonthlyProjectionAsync(year, month, BudgetViewMode.Cashflow, ct);
        var actuals = await actualTransactions.GetByDateRangeAsync(from, to, ct);

        return Build(year, month, [plan], actuals);
    }

    public async Task<PlanActualComparisonDto> GetYearlyAsync(
        int year,
        CancellationToken ct = default)
    {
        var plan = await projections.GetYearlyProjectionAsync(year, BudgetViewMode.Cashflow, ct);
        var actuals = await actualTransactions.GetByDateRangeAsync(
            new DateOnly(year, 1, 1),
            new DateOnly(year, 12, 31),
            ct);

        return Build(year, null, plan.Months, actuals);
    }

    private static PlanActualComparisonDto Build(
        int year,
        int? month,
        IReadOnlyList<MonthlyBudgetProjectionDto> plans,
        IReadOnlyList<Domain.Entities.ActualTransaction> actuals)
    {
        var plannedIncome = plans.Sum(x => x.TotalIncome);
        var plannedExpense = plans.Sum(x => x.TotalExpense);
        var actualIncome = actuals.Where(x => x.Type == BudgetItemType.Income).Sum(x => x.Amount);
        var actualExpense = actuals.Where(x => x.Type == BudgetItemType.Expense).Sum(x => x.Amount);
        var plannedBalance = plannedIncome - plannedExpense;
        var actualBalance = actualIncome - actualExpense;

        var plannedCategories = plans.SelectMany(x => x.Categories)
            .GroupBy(x => new { x.CategoryId, x.CategoryName })
            .ToDictionary(x => x.Key.CategoryId, x => (x.Key.CategoryName, Amount: x.Sum(y => y.TotalAmount)));
        var actualCategories = actuals.Where(x => x.Type == BudgetItemType.Expense)
            .GroupBy(x => new { x.CategoryId, x.Category.Name })
            .ToDictionary(x => x.Key.CategoryId, x => (x.Key.Name, Amount: x.Sum(y => y.Amount)));

        var categories = plannedCategories.Keys.Union(actualCategories.Keys)
            .Select(id =>
            {
                plannedCategories.TryGetValue(id, out var planned);
                actualCategories.TryGetValue(id, out var actual);
                var name = planned.CategoryName ?? actual.Name ?? string.Empty;
                return new CategoryPlanActualDto(id, name, planned.Amount, actual.Amount, actual.Amount - planned.Amount);
            })
            .Where(x => x.PlannedExpense != 0m || x.ActualExpense != 0m)
            .OrderByDescending(x => x.ActualExpense)
            .ThenBy(x => x.CategoryName)
            .ToList();

        return new PlanActualComparisonDto(
            year,
            month,
            plannedIncome,
            actualIncome,
            actualIncome - plannedIncome,
            plannedExpense,
            actualExpense,
            actualExpense - plannedExpense,
            plannedBalance,
            actualBalance,
            actualBalance - plannedBalance,
            categories);
    }
}
