using BudgetPilot.Mobile.Services;
using FluentAssertions;

namespace BudgetPilot.Mobile.Tests;

public sealed class InstanceAddressTests
{
    [Theory]
    [InlineData("https://budget.example.de", "https://budget.example.de")]
    [InlineData("http://budget.lan:8080", "http://budget.lan:8080")]
    [InlineData("budget.example.de", "https://budget.example.de")]
    [InlineData(" https://budget.example.de/path ", "https://budget.example.de")]
    public void Normalize_ReturnsAuthorityForSupportedAddress(string input, string expected)
    {
        InstanceAddress.Normalize(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("https://")]
    [InlineData("ftp://budget.example.de")]
    public void Normalize_RejectsInvalidOrUnsupportedAddress(string input)
    {
        var action = () => InstanceAddress.Normalize(input);

        action.Should().Throw<InvalidOperationException>();
    }
}
