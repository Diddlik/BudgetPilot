using BudgetPilot.Application.Abstractions;
using BudgetPilot.Application.Dtos;
using BudgetPilot.Application.Services;
using BudgetPilot.Domain.Entities;
using BudgetPilot.Domain.Enums;
using BudgetPilot.Domain.Exceptions;
using FluentAssertions;

namespace BudgetPilot.Application.Tests;

public sealed class PlanActualComparisonServiceTests
{
    [Fact]
    public async Task MonthlyComparison_CombinesPlanAndActualByCategory()
    {
        var housingId = Guid.NewGuid();
        var foodId = Guid.NewGuid();
        var emptyId = Guid.NewGuid();
        var projection = new StubProjectionService(new MonthlyBudgetProjectionDto
        {
            Year = 2026,
            Month = 3,
            TotalIncome = 3_000m,
            TotalExpense = 1_300m,
            Balance = 1_700m,
            Categories =
            [
                new() { CategoryId = housingId, CategoryName = "Wohnen", TotalAmount = 1_200m },
                new() { CategoryId = foodId, CategoryName = "Lebensmittel", TotalAmount = 100m },
                new() { CategoryId = emptyId, CategoryName = "Leer", TotalAmount = 0m },
            ],
        });
        var actuals = new StubActualTransactionStore(
        [
            Transaction(new DateOnly(2026, 3, 2), 3_100m, BudgetItemType.Income, Guid.NewGuid(), "Einkommen"),
            Transaction(new DateOnly(2026, 3, 3), 1_250m, BudgetItemType.Expense, housingId, "Wohnen"),
            Transaction(new DateOnly(2026, 3, 4), 80m, BudgetItemType.Expense, foodId, "Lebensmittel"),
        ]);

        var result = await new PlanActualComparisonService(projection, actuals)
            .GetMonthlyAsync(2026, 3);

        result.PlannedIncome.Should().Be(3_000m);
        result.ActualIncome.Should().Be(3_100m);
        result.PlannedExpense.Should().Be(1_300m);
        result.ActualExpense.Should().Be(1_330m);
        result.ExpenseVariance.Should().Be(30m);
        result.ActualBalance.Should().Be(1_770m);
        result.Categories.Single(x => x.CategoryName == "Wohnen").Should().BeEquivalentTo(
            new CategoryPlanActualDto(housingId, "Wohnen", 1_200m, 1_250m, 50m));
        result.Categories.Should().NotContain(x => x.CategoryName == "Leer");
    }

    [Fact]
    public async Task YearlyComparison_AggregatesMonthlyCashflowPlansAndActuals()
    {
        var categoryId = Guid.NewGuid();
        var months = Enumerable.Range(1, 12).Select(month => new MonthlyBudgetProjectionDto
        {
            Year = 2026,
            Month = month,
            TotalIncome = 3_000m,
            TotalExpense = 1_000m,
            Balance = 2_000m,
            Categories = [new() { CategoryId = categoryId, CategoryName = "Wohnen", TotalAmount = 1_000m }],
        }).ToList();
        var projection = new StubProjectionService(months[0], new YearlyBudgetProjectionDto
        {
            Year = 2026,
            Months = months,
            TotalIncome = 36_000m,
            TotalExpense = 12_000m,
            Balance = 24_000m,
        });
        var actuals = new StubActualTransactionStore(
        [Transaction(new DateOnly(2026, 6, 1), 11_500m, BudgetItemType.Expense, categoryId, "Wohnen")]);

        var result = await new PlanActualComparisonService(projection, actuals).GetYearlyAsync(2026);

        result.Month.Should().BeNull();
        result.PlannedExpense.Should().Be(12_000m);
        result.ActualExpense.Should().Be(11_500m);
        result.Categories.Single().ExpenseVariance.Should().Be(-500m);
    }

    [Fact]
    public async Task MonthlyComparison_RejectsInvalidMonth()
    {
        var service = new PlanActualComparisonService(
            new StubProjectionService(new MonthlyBudgetProjectionDto()),
            new StubActualTransactionStore([]));

        var action = () => service.GetMonthlyAsync(2026, 13);

        await action.Should().ThrowAsync<DomainException>()
            .WithMessage("Der Monat muss zwischen 1 und 12 liegen.");
    }

    private static ActualTransaction Transaction(
        DateOnly date, decimal amount, BudgetItemType type, Guid categoryId, string categoryName) => new()
    {
        Id = Guid.NewGuid(),
        Date = date,
        Amount = amount,
        Type = type,
        CategoryId = categoryId,
        Category = new Category { Id = categoryId, Name = categoryName },
    };

    private sealed class StubActualTransactionStore(IReadOnlyList<ActualTransaction> transactions)
        : IActualTransactionStore
    {
        public Task<IReadOnlyList<ActualTransaction>> GetByDateRangeAsync(
            DateOnly from, DateOnly to, CancellationToken ct = default) => Task.FromResult(transactions);

        public Task<ActualTransactionWriteResult> AddAsync(
            ActualTransaction transaction, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class StubProjectionService(
        MonthlyBudgetProjectionDto month,
        YearlyBudgetProjectionDto? year = null) : IBudgetProjectionService
    {
        public Task<MonthlyBudgetProjectionDto> GetMonthlyProjectionAsync(
            int year, int monthNumber, BudgetViewMode viewMode, CancellationToken ct = default) => Task.FromResult(month);

        public Task<YearlyBudgetProjectionDto> GetYearlyProjectionAsync(
            int yearNumber, BudgetViewMode viewMode, CancellationToken ct = default) =>
            Task.FromResult(year ?? throw new NotSupportedException());

        public Task<IReadOnlyList<PaymentScheduleEntry>> GetPaymentScheduleAsync(
            Guid budgetItemId, int fromYear, int toYear, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<YearSummaryDto>> GetMultiYearSummaryAsync(
            int fromYear, int toYear, BudgetViewMode viewMode, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
