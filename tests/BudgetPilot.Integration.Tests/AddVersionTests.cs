using BudgetPilot.Application.Requests;
using BudgetPilot.Application.Services;
using BudgetPilot.Domain.Enums;
using BudgetPilot.Infrastructure;
using BudgetPilot.Infrastructure.Data;
using BudgetPilot.Infrastructure.Repositories;
using BudgetPilot.Infrastructure.Seeding;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BudgetPilot.Integration.Tests;

/// <summary>
/// Verifiziert das Anlegen neuer Versionen über die echte EF/SQLite-Strecke.
/// Deckt die Lücke ab, die zur „expected 1 row, affected 0"-Concurrency-Exception führte:
/// eine über die Navigations-Collection angehängte Version (mit vorab gesetzter Guid-PK)
/// muss als INSERT, nicht als UPDATE persistiert werden – vorwärts wie rückwirkend.
/// </summary>
public sealed class AddVersionTests : IAsyncLifetime
{
    private SqliteConnection _conn = null!;
    private BudgetPilotDbContext _db = null!;
    private BudgetItemService _items = null!;
    private CategoryService _categories = null!;

    public async Task InitializeAsync()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        var options = new DbContextOptionsBuilder<BudgetPilotDbContext>().UseSqlite(_conn).Options;
        _db = new BudgetPilotDbContext(options);
        await new DatabaseSeeder(_db, NullLogger<DatabaseSeeder>.Instance).SeedAsync();

        var itemRepo = new BudgetItemRepository(_db);
        var catRepo = new CategoryRepository(_db);
        var uow = new UnitOfWork(_db);
        var audit = new NoopAuditLog();
        _items = new BudgetItemService(itemRepo, catRepo, uow, audit);
        _categories = new CategoryService(catRepo, itemRepo, uow, audit);
    }

    public Task DisposeAsync() { _db.Dispose(); _conn.Dispose(); return Task.CompletedTask; }

    private async Task<Guid> CreateItemAsync(DateOnly validFrom, decimal amount = 10m)
    {
        var catId = (await _categories.GetAllAsync()).First().Id;
        var created = await _items.CreateAsync(new CreateBudgetItemRequest(
            "Streaming", null, BudgetItemType.Expense, catId,
            amount, BudgetFrequency.Monthly, validFrom, null, null, null));
        return created.Id;
    }

    [Fact]
    public async Task AddVersion_forward_closes_previous_and_opens_new()
    {
        var id = await CreateItemAsync(new DateOnly(2026, 1, 1), amount: 10m);

        await _items.AddVersionAsync(id, new CreateBudgetItemVersionRequest(
            12m, BudgetFrequency.Monthly, new DateOnly(2026, 6, 1), null, null, null));

        var item = (await _items.GetByIdAsync(id))!;
        item.Versions.Should().HaveCount(2);

        var current = item.Versions.Single(v => v.IsCurrent);
        current.Amount.Should().Be(12m);
        current.ValidFrom.Should().Be(new DateOnly(2026, 6, 1));
        current.ValidTo.Should().BeNull();

        var previous = item.Versions.Single(v => !v.IsCurrent);
        previous.Amount.Should().Be(10m);
        previous.ValidTo.Should().Be(new DateOnly(2026, 5, 31));
    }

    [Fact]
    public async Task AddVersion_retroactive_inserts_closed_version_before_existing()
    {
        var id = await CreateItemAsync(new DateOnly(2026, 1, 1), amount: 10m);

        // Rückwirkende Version VOR der bestehenden offenen Version (früher schon ein Fehler).
        await _items.AddVersionAsync(id, new CreateBudgetItemVersionRequest(
            8m, BudgetFrequency.Monthly, new DateOnly(2025, 1, 1), null, null, null));

        var item = (await _items.GetByIdAsync(id))!;
        item.Versions.Should().HaveCount(2);

        // Die ursprüngliche Version bleibt offen/aktuell.
        var current = item.Versions.Single(v => v.IsCurrent);
        current.Amount.Should().Be(10m);
        current.ValidFrom.Should().Be(new DateOnly(2026, 1, 1));
        current.ValidTo.Should().BeNull();

        // Die rückwirkende Version endet am Tag vor der bestehenden.
        var retro = item.Versions.Single(v => !v.IsCurrent);
        retro.Amount.Should().Be(8m);
        retro.ValidFrom.Should().Be(new DateOnly(2025, 1, 1));
        retro.ValidTo.Should().Be(new DateOnly(2025, 12, 31));
    }

    [Fact]
    public async Task AddVersion_retroactive_aligns_to_month_so_projection_stays_valid()
    {
        // Bestehende Version startet MITTEN im Monat (20.06.) – wie beim Anlegen "heute".
        var id = await CreateItemAsync(new DateOnly(2026, 6, 20), amount: 11.99m);

        // Rückwirkende Version davor.
        await _items.AddVersionAsync(id, new CreateBudgetItemVersionRequest(
            7.99m, BudgetFrequency.Monthly, new DateOnly(2024, 3, 15), null, null, null));

        var item = (await _items.GetByIdAsync(id))!;
        var retro = item.Versions.Single(v => !v.IsCurrent);
        // Auf Monatsgrenze geschlossen (31.05.), NICHT 19.06. – sonst läge Juni in beiden Versionen.
        retro.ValidTo.Should().Be(new DateOnly(2026, 5, 31));

        // Die Projektion über Juni 2026 darf nicht wegen zweier gültiger Versionen werfen.
        var projection = new BudgetProjectionService(new BudgetItemRepository(_db));
        var act = async () => await projection.GetMonthlyProjectionAsync(2026, 6, BudgetViewMode.Cashflow);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdateVersion_corrects_a_historical_version_in_place()
    {
        var id = await CreateItemAsync(new DateOnly(2026, 1, 1), amount: 10m);
        // Zweite (aktuelle) Version ab Juni -> die erste wird historisch (bis 31.05.2026).
        await _items.AddVersionAsync(id, new CreateBudgetItemVersionRequest(
            12m, BudgetFrequency.Monthly, new DateOnly(2026, 6, 1), null, null, null));

        var historical = (await _items.GetByIdAsync(id))!.Versions.Single(v => !v.IsCurrent);

        // Falsch eingetragenen historischen Betrag korrigieren (10 -> 9,50).
        await _items.UpdateVersionAsync(id, historical.Id, new UpdateVersionRequest(
            9.50m, BudgetFrequency.Monthly, historical.ValidFrom, null, null, null));

        var reloaded = (await _items.GetByIdAsync(id))!;
        reloaded.Versions.Should().HaveCount(2);

        var fixedVersion = reloaded.Versions.Single(v => v.Id == historical.Id);
        fixedVersion.Amount.Should().Be(9.50m);
        fixedVersion.ValidFrom.Should().Be(new DateOnly(2026, 1, 1));
        fixedVersion.ValidTo.Should().Be(new DateOnly(2026, 5, 31)); // Grenze unverändert

        // Die aktuelle Version bleibt unberührt.
        reloaded.Versions.Single(v => v.IsCurrent).Amount.Should().Be(12m);
    }

    [Fact]
    public async Task AddVersion_before_two_existing_bounds_at_next_version_not_open_one()
    {
        // Reproduziert den Nutzerfall „Gehalt":
        //  V1: 01.01.2026 - offen,  V2: 21.01.2025 - 31.12.2025,  dann eine 3. Version 2023 einfügen.
        var id = await CreateItemAsync(new DateOnly(2026, 1, 1), amount: 3500m);
        await _items.AddVersionAsync(id, new CreateBudgetItemVersionRequest(
            3400m, BudgetFrequency.Monthly, new DateOnly(2025, 1, 21), null, null, null));

        // Dritte Version VOR beiden – früher: „überschneidet sich" (fälschlich bis 31.12.2025).
        await _items.AddVersionAsync(id, new CreateBudgetItemVersionRequest(
            3200m, BudgetFrequency.Monthly, new DateOnly(2023, 3, 21), null, null, null));

        var item = (await _items.GetByIdAsync(id))!;
        item.Versions.Should().HaveCount(3);

        var v2023 = item.Versions.Single(v => v.ValidFrom == new DateOnly(2023, 3, 21));
        // Endet am Monat VOR dem nächsten Eintrag (21.01.2025) -> 31.12.2024, nicht 31.12.2025.
        v2023.ValidTo.Should().Be(new DateOnly(2024, 12, 31));

        // Bestehende Einträge unverändert.
        item.Versions.Single(v => v.ValidFrom == new DateOnly(2025, 1, 21)).ValidTo
            .Should().Be(new DateOnly(2025, 12, 31));
        item.Versions.Single(v => v.ValidFrom == new DateOnly(2026, 1, 1)).ValidTo
            .Should().BeNull();
    }
}
