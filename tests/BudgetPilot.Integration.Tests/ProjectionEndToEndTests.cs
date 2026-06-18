using BudgetPilot.Application.Services;
using BudgetPilot.Domain.Enums;
using BudgetPilot.Infrastructure.Data;
using BudgetPilot.Infrastructure.Repositories;
using BudgetPilot.Infrastructure.Seeding;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BudgetPilot.Integration.Tests;

/// <summary>
/// End-to-End-Verifikation der echten Strecke: SQLite + EF-Core-Migration + DateOnly-Converter
/// + DatabaseSeeder (§12) + EF-Repositories + BudgetProjectionService. Ergänzt die Unit-Tests,
/// die nur In-Memory-Fakes nutzen — hier wird u. a. der ValidFrom/ValidTo-String-Converter real geprüft.
/// </summary>
public sealed class ProjectionEndToEndTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private BudgetPilotDbContext _db = null!;
    private IBudgetProjectionService _projection = null!;

    public async Task InitializeAsync()
    {
        // Eine offen gehaltene In-Memory-SQLite-Verbindung: gleiche DB über alle Contexts hinweg.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<BudgetPilotDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new BudgetPilotDbContext(options);

        // Migration + §12-Seed über den echten Seeder.
        var seeder = new DatabaseSeeder(_db, NullLogger<DatabaseSeeder>.Instance);
        await seeder.SeedAsync();

        _projection = new BudgetProjectionService(new BudgetItemRepository(_db));
    }

    public Task DisposeAsync()
    {
        _db.Dispose();
        _connection.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task March2026_Budget_matches_spec_section_12()
    {
        var march = await _projection.GetMonthlyProjectionAsync(2026, 3, BudgetViewMode.Budget);

        march.TotalIncome.Should().Be(3500.00m);
        Math.Round(march.TotalExpense, 2).Should().Be(1435.98m);
        Math.Round(march.Balance, 2).Should().Be(2064.02m);

        // Strom nutzt die ab März gültige Version (145 statt 120) — prüft den DateOnly-Converter über die Versionsgrenze.
        var strom = march.Lines.Single(l => l.BudgetItemName == "Strom");
        strom.ProjectedMonthlyAmount.Should().Be(145.00m);

        // Kfz jährlich → anteilig 60 in der Budget-Sicht.
        var kfz = march.Lines.Single(l => l.BudgetItemName == "Kfz-Versicherung");
        kfz.ProjectedMonthlyAmount.Should().Be(60.00m);
    }

    [Fact]
    public async Task February2026_Strom_uses_old_version_120()
    {
        var february = await _projection.GetMonthlyProjectionAsync(2026, 2, BudgetViewMode.Budget);

        var strom = february.Lines.Single(l => l.BudgetItemName == "Strom");
        strom.ProjectedMonthlyAmount.Should().Be(120.00m);
    }

    [Fact]
    public async Task Kfz_cashflow_due_only_in_payment_month()
    {
        var march = await _projection.GetMonthlyProjectionAsync(2026, 3, BudgetViewMode.Cashflow);
        var april = await _projection.GetMonthlyProjectionAsync(2026, 4, BudgetViewMode.Cashflow);

        march.Lines.Single(l => l.BudgetItemName == "Kfz-Versicherung")
            .ProjectedMonthlyAmount.Should().Be(720.00m);
        april.Lines.Single(l => l.BudgetItemName == "Kfz-Versicherung")
            .ProjectedMonthlyAmount.Should().Be(0.00m);
    }

    [Fact]
    public async Task Waschmaschine_once_only_in_may()
    {
        var may = await _projection.GetMonthlyProjectionAsync(2026, 5, BudgetViewMode.Budget);
        var june = await _projection.GetMonthlyProjectionAsync(2026, 6, BudgetViewMode.Budget);

        may.Lines.Single(l => l.BudgetItemName == "Waschmaschine")
            .ProjectedMonthlyAmount.Should().Be(600.00m);
        june.Lines.Single(l => l.BudgetItemName == "Waschmaschine")
            .ProjectedMonthlyAmount.Should().Be(0.00m);
    }

    [Fact]
    public async Task Seeder_is_idempotent_on_second_run()
    {
        // Zweiter Seed-Lauf darf keine Duplikate erzeugen.
        var seeder = new DatabaseSeeder(_db, NullLogger<DatabaseSeeder>.Instance);
        await seeder.SeedAsync();

        (await _db.Categories.CountAsync()).Should().Be(6);
        (await _db.BudgetItems.CountAsync()).Should().Be(8);
        (await _db.BudgetItemVersions.CountAsync()).Should().Be(9); // Strom hat 2 Versionen
    }
}
