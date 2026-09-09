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

    public async Task SetTimeZoneIdAsync(string timeZoneId, CancellationToken ct = default)
    {
        var settings = await db.HouseholdSettings
            .SingleOrDefaultAsync(value => value.Id == HouseholdSettings.SingletonId, ct);

        if (settings is null)
        {
            db.HouseholdSettings.Add(new HouseholdSettings { TimeZoneId = timeZoneId });
        }
        else
        {
            settings.TimeZoneId = timeZoneId;
        }

        await db.SaveChangesAsync(ct);
    }
}
