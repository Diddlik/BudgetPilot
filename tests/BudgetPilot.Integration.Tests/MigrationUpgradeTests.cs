using BudgetPilot.Infrastructure.Data;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BudgetPilot.Integration.Tests;

public sealed class MigrationUpgradeTests
{
    [Fact]
    public async Task ExistingDatabase_MigratesToCurrentSchemaAndBackfillsTrackingDates()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BudgetPilotDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new BudgetPilotDbContext(options);
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260619091439_AddAuditEntries");

        var categoryId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO Categories (Id, Name, IsActive, CreatedAt, UpdatedAt)
            VALUES ({categoryId}, {"Archiv"}, {true}, {new DateTime(2026, 1, 1)}, {(DateTime?)null});
            """);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO BudgetItems
                (Id, Name, Description, Owner, Type, CategoryId, IsActive, CreatedAt, UpdatedAt)
            VALUES
                ({itemId}, {"Alter Vertrag"}, {(string?)null}, {(string?)null}, {2}, {categoryId}, {false},
                 {new DateTime(2025, 1, 1)}, {new DateTime(2026, 9, 8, 18, 30, 0)});
            """);

        await migrator.MigrateAsync();

        db.ChangeTracker.Clear();
        var item = await db.BudgetItems.SingleAsync(x => x.Id == itemId);
        item.DeactivatedOn.Should().Be(new DateOnly(2026, 9, 8));
        var settings = await db.HouseholdSettings.SingleAsync();
        settings.OccurrenceTrackingStartDate.Should().NotBeNull();
        settings.OccurrenceTrackingStartDate!.Value.Day.Should().Be(1);
        (await db.Database.GetPendingMigrationsAsync()).Should().BeEmpty();
    }
}
