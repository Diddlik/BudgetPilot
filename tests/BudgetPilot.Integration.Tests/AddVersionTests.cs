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
}
