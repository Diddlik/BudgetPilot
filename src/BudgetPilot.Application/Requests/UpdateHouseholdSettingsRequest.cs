namespace BudgetPilot.Application.Requests;

public sealed record UpdateHouseholdSettingsRequest(
    string TimeZoneId,
    DateOnly? OccurrenceTrackingStartDate = null);
