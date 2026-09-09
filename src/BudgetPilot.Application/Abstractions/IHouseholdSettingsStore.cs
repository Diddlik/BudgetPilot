namespace BudgetPilot.Application.Abstractions;

public interface IHouseholdSettingsStore
{
    Task<string?> GetTimeZoneIdAsync(CancellationToken ct = default);
    Task<DateOnly?> GetOccurrenceTrackingStartDateAsync(CancellationToken ct = default);
    Task SetAsync(string timeZoneId, DateOnly occurrenceTrackingStartDate, CancellationToken ct = default);
}
