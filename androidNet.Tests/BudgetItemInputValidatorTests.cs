using BudgetPilot.Domain.Enums;
using BudgetPilot.Mobile.Services;
using FluentAssertions;

namespace BudgetPilot.Mobile.Tests;

public sealed class BudgetItemInputValidatorTests
{
    [Fact]
    public void ValidateMetadata_RequiresNameAndCategory()
    {
        var noName = () => BudgetItemInputValidator.ValidateMetadata(" ", Guid.NewGuid());
        var noCategory = () => BudgetItemInputValidator.ValidateMetadata("Miete", Guid.Empty);

        noName.Should().Throw<InvalidOperationException>().WithMessage("*Namen*");
        noCategory.Should().Throw<InvalidOperationException>().WithMessage("*Kategorie*");
    }

    [Fact]
    public void ValidateVersion_AcceptsBoundaryValues()
    {
        var model = new VersionFormModel
        {
            Amount = 0m,
            Frequency = BudgetFrequency.Yearly,
            PaymentDay = 31,
            PaymentMonth = 12
        };

        var act = () => BudgetItemInputValidator.ValidateVersion(model);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(-0.01, BudgetFrequency.Monthly, null, null)]
    [InlineData(10, BudgetFrequency.Monthly, 0, null)]
    [InlineData(10, BudgetFrequency.Monthly, 32, null)]
    [InlineData(10, BudgetFrequency.Yearly, null, 0)]
    [InlineData(10, BudgetFrequency.Yearly, null, 13)]
    public void ValidateVersion_RejectsInvalidFinancialInputs(
        double amount,
        BudgetFrequency frequency,
        int? day,
        int? month)
    {
        var model = new VersionFormModel
        {
            Amount = (decimal)amount,
            Frequency = frequency,
            PaymentDay = day,
            PaymentMonth = month
        };

        var act = () => BudgetItemInputValidator.ValidateVersion(model);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateVersion_IgnoresFieldsHiddenForFrequency()
    {
        var model = new VersionFormModel
        {
            Amount = 10m,
            Frequency = BudgetFrequency.Once,
            PaymentDay = 99,
            PaymentMonth = 99
        };

        var act = () => BudgetItemInputValidator.ValidateVersion(model);

        act.Should().NotThrow();
    }
}
