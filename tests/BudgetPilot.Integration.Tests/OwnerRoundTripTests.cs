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

/// <summary>Verifiziert, dass das optionale Inhaber-Feld (Owner) über EF/SQLite korrekt persistiert wird.</summary>
public sealed class OwnerRoundTripTests : IAsyncLifetime
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
        _items = new BudgetItemService(itemRepo, catRepo, uow);
        _categories = new CategoryService(catRepo, itemRepo, uow);
    }

    public Task DisposeAsync() { _db.Dispose(); _conn.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task Owner_is_persisted_on_create_and_updatable()
    {
        var catId = (await _categories.GetAllAsync()).First().Id;

        var created = await _items.CreateAsync(new CreateBudgetItemRequest(
            "Gehalt Frau", null, BudgetItemType.Income, catId,
            2800m, BudgetFrequency.Monthly, new DateOnly(2026, 1, 1),
            null, null, null, Owner: "Frau"));

        created.Owner.Should().Be("Frau");

        var reloaded = await _items.GetByIdAsync(created.Id);
        reloaded!.Owner.Should().Be("Frau");

        await _items.UpdateMetadataAsync(created.Id, new UpdateBudgetItemMetadataRequest(
            "Gehalt Frau", null, BudgetItemType.Income, catId, Owner: "Gemeinsam"));

        (await _items.GetByIdAsync(created.Id))!.Owner.Should().Be("Gemeinsam");
    }
}
