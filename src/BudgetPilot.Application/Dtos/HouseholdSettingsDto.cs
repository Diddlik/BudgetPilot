namespace BudgetPilot.Application.Dtos;

public sealed record HouseholdSettingsDto(
    string TimeZoneId,
    string TimeZoneDisplayName,
    DateOnly Today,
    DateOnly OccurrenceTrackingStartDate,
    IReadOnlyList<TimeZoneOptionDto> AvailableTimeZones);

public sealed record TimeZoneOptionDto(string Id, string DisplayName);
