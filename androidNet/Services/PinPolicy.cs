namespace BudgetPilot.Mobile.Services;

public static class PinPolicy
{
    public static void Validate(string pin)
    {
        if (pin.Length is < 4 or > 8 || pin.Any(c => !char.IsAsciiDigit(c)))
        {
            throw new InvalidOperationException("Die App-PIN muss aus 4 bis 8 Ziffern bestehen.");
        }
    }
}
