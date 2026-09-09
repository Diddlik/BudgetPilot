using BudgetPilot.Application.Requests;
using BudgetPilot.Application.Dtos;
using BudgetPilot.Application.Services;
using BudgetPilot.Domain.Enums;
using FluentAssertions;

namespace BudgetPilot.Application.Tests;

public sealed class PaymentSchedulePreviewTests
{
    [Theory]
    [InlineData(BudgetFrequency.Monthly, "2026-01-31", "2026-02-28", "2026-03-31")]
    [InlineData(BudgetFrequency.Quarterly, "2026-01-31", "2026-04-30", "2026-07-31")]
    [InlineData(BudgetFrequency.Yearly, "2026-01-31", "2027-01-31", "2028-01-31")]
    public void Build_ReturnsNextThreeDueDates(
        BudgetFrequency frequency,
        string first,
        string second,
        string third)
    {
        var request = new CreateBudgetItemVersionRequest(
            42m, frequency, new DateOnly(2026, 1, 31), null, null, null);

        var result = PaymentSchedulePreview.Build(request);

        result.Select(entry => entry.DueDate).Should().Equal(
            DateOnly.Parse(first), DateOnly.Parse(second), DateOnly.Parse(third));
    }

    [Fact]
    public void Build_Once_ReturnsOnlyTheEnteredDate()
    {
        var request = new CreateBudgetItemVersionRequest(
            99m, BudgetFrequency.Once, new DateOnly(2026, 5, 15), null, null, null);

        var result = PaymentSchedulePreview.Build(request);

        result.Should().ContainSingle()
            .Which.Should().Be(new PaymentSchedulePreviewEntry(new DateOnly(2026, 5, 15), 99m));
    }

    [Fact]
    public void Build_YearlyPaymentMonthBeforeStart_MovesToNextYear()
    {
        var request = new CreateBudgetItemVersionRequest(
            120m, BudgetFrequency.Yearly, new DateOnly(2026, 9, 10), 5, 3, null);

        var result = PaymentSchedulePreview.Build(request);

        result[0].DueDate.Should().Be(new DateOnly(2027, 3, 5));
    }
}
