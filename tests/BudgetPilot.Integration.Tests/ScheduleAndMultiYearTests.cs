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
/// Verifiziert die Mehrjahres-Funktionen über die echte EF/SQLite-Strecke:
/// Zahlungsplan einer Position über Jahre (GetPaymentScheduleAsync) und
/// Jahressummen über einen Bereich (GetMultiYearSummaryAsync).
/// </summary>
public sealed class ScheduleAndMultiYearTests : IAsyncLifetime
{
    private SqliteConnection _conn = null!;
    private BudgetPilotDbContext _db = null!;
    private BudgetItemService _items = null!;
    private CategoryService _categories = null!;
    private BudgetProjectionService _projection = null!;

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
        _projection = new BudgetProjectionService(itemRepo);
    }

    public Task DisposeAsync() { _db.Dispose(); _conn.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task PaymentSchedule_yearly_from_2020_lists_one_due_per_year()
    {
        var catId = (await _categories.GetAllAsync()).First().Id;
        var created = await _items.CreateAsync(new CreateBudgetItemRequest(
            "Domain-Gebühr", null, BudgetItemType.Expense, catId,
            Amount: 12.00m, Frequency: BudgetFrequency.Yearly,
            ValidFrom: new DateOnly(2020, 6, 1), PaymentDay: null, PaymentMonth: null, Note: null));

        var schedule = await _projection.GetPaymentScheduleAsync(created.Id, 2020, 2026);

        schedule.Should().HaveCount(7); // 2020..2026
        schedule.Should().OnlyContain(e => e.Month == 6 && e.Amount == 12.00m);
        schedule.Select(e => e.Year).Should().BeInAscendingOrder()
            .And.ContainInOrder(2020, 2021, 2022, 2023, 2024, 2025, 2026);
    }

    [Fact]
    public async Task PaymentSchedule_returns_empty_for_unknown_item_or_inverted_range()
    {
        (await _projection.GetPaymentScheduleAsync(Guid.NewGuid(), 2026, 2030)).Should().BeEmpty();

        var catId = (await _categories.GetAllAsync()).First().Id;
        var created = await _items.CreateAsync(new CreateBudgetItemRequest(
            "X", null, BudgetItemType.Expense, catId, 10m, BudgetFrequency.Yearly,
            new DateOnly(2026, 1, 1), null, null, null));
        (await _projection.GetPaymentScheduleAsync(created.Id, 2030, 2026)).Should().BeEmpty();
    }

    [Fact]
    public async Task MultiYearSummary_covers_range_with_seed_income()
    {
        // Seed: Gehalt 3500 monatlich ab 2026-01 → 42.000 €/Jahr Einnahmen ab 2026.
        var years = await _projection.GetMultiYearSummaryAsync(2026, 2028, BudgetViewMode.Budget);

        years.Should().HaveCount(3);
        years.Select(y => y.Year).Should().ContainInOrder(2026, 2027, 2028);
        years.Should().OnlyContain(y => y.TotalIncome == 42000.00m);
        years.Should().OnlyContain(y => y.Balance == y.TotalIncome - y.TotalExpense);
    }
}
