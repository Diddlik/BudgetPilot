using BudgetPilot.Domain.Entities;
using BudgetPilot.Domain.Enums;
using BudgetPilot.Domain.Exceptions;
using BudgetPilot.Domain.Rules;
using FluentAssertions;

namespace BudgetPilot.Domain.Tests;

public sealed class BudgetValidationTests
{
    [Fact]
    public void ValidVersionValues_DoNotThrow()
    {
        var act = () => BudgetValidation.ValidateVersionValues(
            10m, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), 15, 3);
        act.Should().NotThrow();
    }

    [Fact]
    public void NegativeAmount_Throws()
    {
        var act = () => BudgetValidation.ValidateVersionValues(-0.01m, new DateOnly(2026, 1, 1), null, null, null);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ValidToBeforeValidFrom_Throws()
    {
        var act = () => BudgetValidation.ValidateVersionValues(
            10m, new DateOnly(2026, 3, 1), new DateOnly(2026, 2, 28), null, null);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ValidToEqualsValidFrom_DoesNotThrow()
    {
        var act = () => BudgetValidation.ValidateVersionValues(
            10m, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 1), null, null);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void PaymentDayOutOfRange_Throws(int day)
    {
        var act = () => BudgetValidation.ValidateVersionValues(10m, new DateOnly(2026, 1, 1), null, day, null);
        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void PaymentMonthOutOfRange_Throws(int month)
    {
        var act = () => BudgetValidation.ValidateVersionValues(10m, new DateOnly(2026, 1, 1), null, null, month);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void EmptyName_Throws()
    {
        var act = () => BudgetValidation.ValidateItemMetadata("   ");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void EnsureNoOverlap_OverlappingClosedInterval_Throws()
    {
        var existing = new[]
        {
            Version(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31))
        };
        var act = () => BudgetValidation.EnsureNoOverlap(existing, new DateOnly(2026, 2, 1), null);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void EnsureNoOverlap_AdjacentIntervals_DoNotThrow()
    {
        var existing = new[]
        {
            Version(new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 28))
        };
        var act = () => BudgetValidation.EnsureNoOverlap(existing, new DateOnly(2026, 3, 1), null);
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureNoOverlap_IgnoresSelf()
    {
        var self = Version(new DateOnly(2026, 1, 1), null);
        var act = () => BudgetValidation.EnsureNoOverlap(new[] { self }, new DateOnly(2026, 1, 1), null, ignoreVersionId: self.Id);
        act.Should().NotThrow();
    }

    private static BudgetItemVersion Version(DateOnly from, DateOnly? to) => new()
    {
        Id = Guid.NewGuid(),
        Amount = 1m,
        Frequency = BudgetFrequency.Monthly,
        ValidFrom = from,
        ValidTo = to
    };
}
