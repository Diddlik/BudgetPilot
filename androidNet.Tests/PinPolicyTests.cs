using BudgetPilot.Mobile.Services;
using FluentAssertions;

namespace BudgetPilot.Mobile.Tests;

public sealed class PinPolicyTests
{
    [Theory]
    [InlineData("1234")]
    [InlineData("12345678")]
    public void Validate_AcceptsFourToEightDigits(string pin)
    {
        var act = () => PinPolicy.Validate(pin);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("123")]
    [InlineData("123456789")]
    [InlineData("12a4")]
    [InlineData("")]
    public void Validate_RejectsInvalidPins(string pin)
    {
        var act = () => PinPolicy.Validate(pin);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*4 bis 8 Ziffern*");
    }
}
