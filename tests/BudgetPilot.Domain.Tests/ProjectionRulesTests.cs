using BudgetPilot.Domain.Entities;
using BudgetPilot.Domain.Enums;
using BudgetPilot.Domain.Exceptions;
using BudgetPilot.Domain.Rules;
using FluentAssertions;

namespace BudgetPilot.Domain.Tests;

public sealed class ProjectionRulesTests
{
    [Fact]
    public void IsValidInMonth_RespectsClosedAndOpenIntervals()
    {
        var v = Version(BudgetFrequency.Monthly, new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 28));
        ProjectionRules.IsValidInMonth(v, 2026, 1).Should().BeTrue();
        ProjectionRules.IsValidInMonth(v, 2026, 2).Should().BeTrue();
        ProjectionRules.IsValidInMonth(v, 2026, 3).Should().BeFalse();
        ProjectionRules.IsValidInMonth(v, 2025, 12).Should().BeFalse();
    }

    [Fact]
    public void MonthsSince_ComputesGapAcrossYears()
    {
        ProjectionRules.MonthsSince(new DateOnly(2026, 1, 1), 2026, 1).Should().Be(0);
        ProjectionRules.MonthsSince(new DateOnly(2026, 1, 1), 2026, 4).Should().Be(3);
        ProjectionRules.MonthsSince(new DateOnly(2026, 1, 1), 2027, 1).Should().Be(12);
    }

    [Fact]
    public void SelectValidVersion_ReturnsSingleMatch_OrNull()
    {
        var item = Item(
            Version(BudgetFrequency.Monthly, new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 28)),
            Version(BudgetFrequency.Monthly, new DateOnly(2026, 3, 1), null));

        ProjectionRules.SelectValidVersion(item, 2026, 2)!.ValidFrom.Should().Be(new DateOnly(2026, 1, 1));
        ProjectionRules.SelectValidVersion(item, 2026, 4)!.ValidFrom.Should().Be(new DateOnly(2026, 3, 1));
        ProjectionRules.SelectValidVersion(item, 2025, 12).Should().BeNull();
    }

    [Fact]
    public void SelectValidVersion_TwoValid_ThrowsDomainException()
    {
        var item = Item(
            Version(BudgetFrequency.Monthly, new DateOnly(2026, 1, 1), null),
            Version(BudgetFrequency.Monthly, new DateOnly(2026, 1, 1), null));

        var act = () => ProjectionRules.SelectValidVersion(item, 2026, 3);
        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(BudgetViewMode.Budget, 1, 100)]   // 300/3
    [InlineData(BudgetViewMode.Cashflow, 1, 300)] // MonthsSince%3==0
    [InlineData(BudgetViewMode.Cashflow, 2, 0)]
    [InlineData(BudgetViewMode.Cashflow, 4, 300)]
    public void Quarterly_ProjectedAmount(BudgetViewMode mode, int month, decimal expected)
    {
        var v = Version(BudgetFrequency.Quarterly, new DateOnly(2026, 1, 1), null, amount: 300m);
        ProjectionRules.ProjectedMonthlyAmount(v, 2026, month, mode, out _).Should().Be(expected);
    }

    [Fact]
    public void Yearly_Budget_IsOneTwelfth_Cashflow_OnlyPaymentMonth()
    {
        var v = Version(BudgetFrequency.Yearly, new DateOnly(2026, 1, 1), null, amount: 720m, paymentMonth: 3);

        ProjectionRules.ProjectedMonthlyAmount(v, 2026, 5, BudgetViewMode.Budget, out _).Should().Be(60m);
        ProjectionRules.ProjectedMonthlyAmount(v, 2026, 3, BudgetViewMode.Cashflow, out var dueMar).Should().Be(720m);
        dueMar.Should().BeTrue();
        ProjectionRules.ProjectedMonthlyAmount(v, 2026, 4, BudgetViewMode.Cashflow, out var dueApr).Should().Be(0m);
        dueApr.Should().BeFalse();
    }

    [Fact]
    public void Yearly_Cashflow_FallsBackToValidFromMonthWhenNoPaymentMonth()
    {
        var v = Version(BudgetFrequency.Yearly, new DateOnly(2026, 6, 1), null, amount: 1200m);
        ProjectionRules.ProjectedMonthlyAmount(v, 2026, 6, BudgetViewMode.Cashflow, out _).Should().Be(1200m);
        ProjectionRules.ProjectedMonthlyAmount(v, 2026, 7, BudgetViewMode.Cashflow, out _).Should().Be(0m);
    }

    [Fact]
    public void Once_OnlyInValidFromMonth_BothViews()
    {
        var v = Version(BudgetFrequency.Once, new DateOnly(2026, 5, 15), null, amount: 600m);
        ProjectionRules.ProjectedMonthlyAmount(v, 2026, 5, BudgetViewMode.Budget, out _).Should().Be(600m);
        ProjectionRules.ProjectedMonthlyAmount(v, 2026, 5, BudgetViewMode.Cashflow, out _).Should().Be(600m);
        ProjectionRules.ProjectedMonthlyAmount(v, 2026, 6, BudgetViewMode.Budget, out _).Should().Be(0m);
    }

    private static BudgetItemVersion Version(
        BudgetFrequency freq, DateOnly from, DateOnly? to, decimal amount = 10m, int? paymentMonth = null) => new()
    {
        Id = Guid.NewGuid(),
        Amount = amount,
        Frequency = freq,
        ValidFrom = from,
        ValidTo = to,
        PaymentMonth = paymentMonth
    };

    private static BudgetItem Item(params BudgetItemVersion[] versions) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test",
        Versions = versions.ToList()
    };
}
