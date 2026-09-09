using BudgetPilot.Application.Services;
using BudgetPilot.Application.Abstractions;
using BudgetPilot.Application.Tests.Fakes;
using BudgetPilot.Domain.Enums;
using FluentAssertions;

namespace BudgetPilot.Application.Tests;

public sealed class PlannedOccurrenceServiceTests
{
    [Fact]
    public async Task GetOpenAsync_DefaultRangeStartsAtHouseholdTrackingDate()
    {
        var store = new InMemoryStore();
        var category = SeedData.Category("Wohnen");
        store.Categories.Add(category);
        store.Items.Add(SeedData.Item("Darlehen", BudgetItemType.Expense, category,
            SeedData.Version(2018m, BudgetFrequency.Monthly, new DateOnly(2018, 1, 5), paymentDay: 5)));
        var service = new PlannedOccurrenceService(
            new FakeBudgetItemRepository(store),
            new EmptyResolvedOccurrenceStore(),
            new FakeHouseholdSettingsService(new DateOnly(2026, 9, 1)));

        var result = await service.GetOpenAsync(null, new DateOnly(2026, 10, 31));

        result.Select(x => x.DueDate).Should().Equal(
            new DateOnly(2026, 9, 5),
            new DateOnly(2026, 10, 5));
    }

    [Fact]
    public async Task GetAssumedActualsAsync_IgnoresTrackingStartButExcludesResolvedOccurrences()
    {
        var store = new InMemoryStore();
        var category = SeedData.Category("Wohnen");
        var item = SeedData.Item("Miete", BudgetItemType.Expense, category,
            SeedData.Version(1200m, BudgetFrequency.Monthly, new DateOnly(2026, 1, 5), paymentDay: 5));
        store.Categories.Add(category);
        store.Items.Add(item);
        var service = new PlannedOccurrenceService(
            new FakeBudgetItemRepository(store),
            new EmptyResolvedOccurrenceStore($"{item.Id:N}:2026-02-05"),
            new FakeHouseholdSettingsService(new DateOnly(2026, 9, 1)));

        var result = await service.GetAssumedActualsAsync(
            new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31));

        result.Select(x => x.DueDate).Should().Equal(
            new DateOnly(2026, 1, 5),
            new DateOnly(2026, 3, 5));
    }

    [Fact]
    public async Task GetOpenAsync_ReconstructsEveryMissedMonth()
    {
        var store = new InMemoryStore();
        var category = SeedData.Category("Wohnen");
        store.Categories.Add(category);
        store.Items.Add(SeedData.Item("Miete", BudgetItemType.Expense, category,
            SeedData.Version(1200m, BudgetFrequency.Monthly, new DateOnly(2026, 1, 5), paymentDay: 5)));
        var service = new PlannedOccurrenceService(
            new FakeBudgetItemRepository(store),
            new EmptyResolvedOccurrenceStore(),
            new FakeHouseholdSettingsService(new DateOnly(2026, 1, 1)));

        var result = await service.GetOpenAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31));

        result.Select(x => (x.DueDate, x.PlannedAmount)).Should().Equal(
            (new DateOnly(2026, 1, 5), 1200m),
            (new DateOnly(2026, 2, 5), 1200m),
            (new DateOnly(2026, 3, 5), 1200m));
        result.Select(x => x.OccurrenceKey).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task GetOpenAsync_ExcludesAlreadyResolvedOccurrence()
    {
        var store = new InMemoryStore();
        var category = SeedData.Category("Wohnen");
        var item = SeedData.Item("Miete", BudgetItemType.Expense, category,
            SeedData.Version(1200m, BudgetFrequency.Monthly, new DateOnly(2026, 1, 5), paymentDay: 5));
        store.Categories.Add(category);
        store.Items.Add(item);
        var resolvedKey = $"{item.Id:N}:2026-02-05";
        var service = new PlannedOccurrenceService(
            new FakeBudgetItemRepository(store),
            new EmptyResolvedOccurrenceStore(resolvedKey),
            new FakeHouseholdSettingsService(new DateOnly(2026, 1, 1)));

        var result = await service.GetOpenAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31));

        result.Select(x => x.DueDate).Should().Equal(
            new DateOnly(2026, 1, 5),
            new DateOnly(2026, 3, 5));
    }

    [Fact]
    public async Task GetOpenAsync_InactiveItemKeepsOnlyOccurrencesThroughDeactivationDate()
    {
        var store = new InMemoryStore();
        var category = SeedData.Category("Wohnen");
        var item = SeedData.Item("Miete", BudgetItemType.Expense, category,
            SeedData.Version(1200m, BudgetFrequency.Monthly, new DateOnly(2026, 1, 5), paymentDay: 5));
        item.IsActive = false;
        item.DeactivatedOn = new DateOnly(2026, 2, 10);
        store.Categories.Add(category);
        store.Items.Add(item);
        var service = new PlannedOccurrenceService(
            new FakeBudgetItemRepository(store),
            new EmptyResolvedOccurrenceStore(),
            new FakeHouseholdSettingsService(new DateOnly(2026, 1, 1)));

        var result = await service.GetOpenAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 30));

        result.Select(x => x.DueDate).Should().Equal(
            new DateOnly(2026, 1, 5),
            new DateOnly(2026, 2, 5));
    }
}

file sealed class EmptyResolvedOccurrenceStore(params string[] keys) : IResolvedOccurrenceStore
{
    public Task<IReadOnlySet<string>> GetResolvedKeysAsync(DateOnly from, DateOnly to, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlySet<string>>(keys.ToHashSet());

    public Task<BudgetPilot.Domain.Entities.OccurrenceResolution?> GetAsync(string occurrenceKey, CancellationToken ct = default) =>
        Task.FromResult<BudgetPilot.Domain.Entities.OccurrenceResolution?>(null);

    public Task<BudgetPilot.Domain.Entities.OccurrenceResolution> AddAsync(
        BudgetPilot.Domain.Entities.OccurrenceResolution resolution,
        BudgetPilot.Domain.Entities.ActualTransaction? transaction,
        CancellationToken ct = default) => Task.FromResult(resolution);

    public Task<IReadOnlyList<BudgetPilot.Domain.Entities.OccurrenceResolution>> GetRecentAsync(
        int max,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<BudgetPilot.Domain.Entities.OccurrenceResolution>>([]);

    public Task<BudgetPilot.Domain.Entities.OccurrenceResolution?> UpdateConfirmedAsync(
        string occurrenceKey,
        decimal amount,
        DateOnly date,
        string? description,
        CancellationToken ct = default) =>
        Task.FromResult<BudgetPilot.Domain.Entities.OccurrenceResolution?>(null);

    public Task<bool> RemoveAsync(string occurrenceKey, CancellationToken ct = default) => Task.FromResult(false);
}
