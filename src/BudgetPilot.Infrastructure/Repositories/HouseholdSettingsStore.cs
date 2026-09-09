using BudgetPilot.Application.Abstractions;
using BudgetPilot.Domain.Entities;
using BudgetPilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BudgetPilot.Infrastructure.Repositories;

public sealed class HouseholdSettingsStore : IHouseholdSettingsStore
{
    private readonly BudgetPilotDbContext db;

    public HouseholdSettingsStore(BudgetPilotDbContext db) => this.db = db;

    public Task<string?> GetTimeZoneIdAsync(CancellationToken ct = default) =>
        db.HouseholdSettings
            .AsNoTracking()
            .Where(settings => settings.Id == HouseholdSettings.SingletonId)
            .Select(settings => settings.TimeZoneId)
            .SingleOrDefaultAsync(ct);

    public Task<DateOnly?> GetOccurrenceTrackingStartDateAsync(CancellationToken ct = default) =>
        db.HouseholdSettings
            .AsNoTracking()
            .Where(settings => settings.Id == HouseholdSettings.SingletonId)
            .Select(settings => settings.OccurrenceTrackingStartDate)
            .SingleOrDefaultAsync(ct);

    public async Task SetAsync(
        string timeZoneId,
        DateOnly occurrenceTrackingStartDate,
        CancellationToken ct = default)
    {
        var settings = await db.HouseholdSettings
            .SingleOrDefaultAsync(value => value.Id == HouseholdSettings.SingletonId, ct);

        if (settings is null)
        {
            db.HouseholdSettings.Add(new HouseholdSettings
            {
                TimeZoneId = timeZoneId,
                OccurrenceTrackingStartDate = occurrenceTrackingStartDate
            });
        }
        else
        {
            settings.TimeZoneId = timeZoneId;
            settings.OccurrenceTrackingStartDate = occurrenceTrackingStartDate;
        }

        await db.SaveChangesAsync(ct);
    }
}
