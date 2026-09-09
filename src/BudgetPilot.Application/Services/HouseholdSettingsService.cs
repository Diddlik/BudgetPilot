using BudgetPilot.Application.Abstractions;
using BudgetPilot.Application.Dtos;
using BudgetPilot.Domain.Entities;
using BudgetPilot.Domain.Enums;
using BudgetPilot.Domain.Exceptions;

namespace BudgetPilot.Application.Services;

public interface IHouseholdSettingsService
{
    Task<HouseholdSettingsDto> GetAsync(CancellationToken ct = default);
    Task<DateOnly> GetTodayAsync(CancellationToken ct = default);
    Task<HouseholdSettingsDto> UpdateTimeZoneAsync(string timeZoneId, CancellationToken ct = default);
}

public sealed class HouseholdSettingsService : IHouseholdSettingsService
{
    private readonly IHouseholdSettingsStore store;
    private readonly IAuditLog audit;
    private readonly TimeProvider timeProvider;

    public HouseholdSettingsService(IHouseholdSettingsStore store, IAuditLog audit, TimeProvider timeProvider)
    {
        this.store = store;
        this.audit = audit;
        this.timeProvider = timeProvider;
    }

    public async Task<HouseholdSettingsDto> GetAsync(CancellationToken ct = default)
    {
        var id = await GetStoredTimeZoneIdAsync(ct);
        return CreateDto(id, ResolveTimeZone(id));
    }

    public async Task<DateOnly> GetTodayAsync(CancellationToken ct = default)
    {
        var id = await GetStoredTimeZoneIdAsync(ct);
        return TodayIn(ResolveTimeZone(id));
    }

    public async Task<HouseholdSettingsDto> UpdateTimeZoneAsync(string timeZoneId, CancellationToken ct = default)
    {
        var normalized = timeZoneId?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new DomainException("Bitte eine Zeitzone auswählen.");
        }

        var zone = ResolveTimeZone(normalized);
        var portableId = ToIanaId(zone);
        var previous = await GetStoredTimeZoneIdAsync(ct);
        if (!string.Equals(previous, portableId, StringComparison.Ordinal))
        {
            await store.SetTimeZoneIdAsync(portableId, ct);
            await audit.RecordAsync(
                AuditAction.Updated,
                "HouseholdSettings",
                Guid.Empty,
                "Haushaltszeit",
                $"Zeitzone: {previous} → {portableId}",
                ct);
        }

        return CreateDto(portableId, zone);
    }

    private async Task<string> GetStoredTimeZoneIdAsync(CancellationToken ct) =>
        await store.GetTimeZoneIdAsync(ct) ?? HouseholdSettings.DefaultTimeZoneId;

    private HouseholdSettingsDto CreateDto(string id, TimeZoneInfo zone) => new(
        id,
        zone.DisplayName,
        TodayIn(zone),
        GetAvailableTimeZones());

    private DateOnly TodayIn(TimeZoneInfo zone)
    {
        var local = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), zone);
        return DateOnly.FromDateTime(local.DateTime);
    }

    private static TimeZoneInfo ResolveTimeZone(string id)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (TimeZoneNotFoundException) when (TimeZoneInfo.TryConvertIanaIdToWindowsId(id, out var windowsId))
        {
            return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new DomainException("Die ausgewählte Zeitzone wird auf diesem Server nicht unterstützt.");
        }
        catch (InvalidTimeZoneException)
        {
            throw new DomainException("Die ausgewählte Zeitzone ist ungültig.");
        }
    }

    private static IReadOnlyList<TimeZoneOptionDto> GetAvailableTimeZones() =>
        TimeZoneInfo.GetSystemTimeZones()
            .Select(zone => new TimeZoneOptionDto(ToIanaId(zone), zone.DisplayName))
            .DistinctBy(option => option.Id)
            .OrderBy(option => option.DisplayName, StringComparer.CurrentCulture)
            .ToList();

    private static string ToIanaId(TimeZoneInfo zone) =>
        TimeZoneInfo.TryConvertWindowsIdToIanaId(zone.Id, out var ianaId) ? ianaId : zone.Id;
}
