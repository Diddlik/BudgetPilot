namespace BudgetPilot.Application.Abstractions;

public interface IHouseholdSettingsStore
{
    Task<string?> GetTimeZoneIdAsync(CancellationToken ct = default);
    Task SetTimeZoneIdAsync(string timeZoneId, CancellationToken ct = default);
}
