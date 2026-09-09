using BudgetPilot.Domain.Entities;
using BudgetPilot.Infrastructure.Data;
using BudgetPilot.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BudgetPilot.Integration.Tests;

public sealed class HouseholdSettingsPersistenceTests
{
    [Fact]
    public async Task Settings_RoundTripThroughSqlite()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BudgetPilotDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var writeDb = new BudgetPilotDbContext(options))
        {
            await writeDb.Database.EnsureCreatedAsync();
            var store = new HouseholdSettingsStore(writeDb);

            (await store.GetTimeZoneIdAsync()).Should().Be(HouseholdSettings.DefaultTimeZoneId);
            await store.SetTimeZoneIdAsync("America/New_York");
        }

        await using var readDb = new BudgetPilotDbContext(options);
        var reloaded = new HouseholdSettingsStore(readDb);
        (await reloaded.GetTimeZoneIdAsync()).Should().Be("America/New_York");
    }
}
