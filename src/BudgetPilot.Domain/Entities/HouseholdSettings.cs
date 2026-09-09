namespace BudgetPilot.Domain.Entities;

/// <summary>Globale Einstellungen des gemeinsam geführten Haushalts.</summary>
public sealed class HouseholdSettings
{
    public const int SingletonId = 1;
    public const string DefaultTimeZoneId = "Europe/Berlin";

    public int Id { get; set; } = SingletonId;
    public string TimeZoneId { get; set; } = DefaultTimeZoneId;
}
