using BudgetPilot.Application.Abstractions;
using BudgetPilot.Application.Requests;
using BudgetPilot.Application.Services;
using BudgetPilot.Application.Tests.Fakes;
using BudgetPilot.Domain.Entities;
using BudgetPilot.Domain.Enums;
using FluentAssertions;

namespace BudgetPilot.Application.Tests;

public sealed class QuickExpenseServiceTests
{
    [Fact]
    public async Task CreateAsync_UsesHouseholdTodayAndIsIdempotent()
    {
        var data = new InMemoryStore();
        var category = SeedData.Category("Lebensmittel");
        data.Categories.Add(category);
        var transactions = new MemoryActualTransactionStore();
        var audit = new RecordingAuditLog();
        var service = new QuickExpenseService(
            transactions,
            new FakeCategoryRepository(data),
            new FakeBudgetItemRepository(data),
            new FixedHouseholdSettingsService(new DateOnly(2026, 9, 8)),
            audit);
        var request = new CreateQuickExpenseRequest(Guid.NewGuid(), 12.45m, category.Id);

        var first = await service.CreateAsync(request);
        var repeated = await service.CreateAsync(request);

        transactions.Items.Should().ContainSingle();
        first.Should().BeEquivalentTo(repeated);
        first.Date.Should().Be(new DateOnly(2026, 9, 8));
        first.Type.Should().Be(BudgetItemType.Expense);
        audit.Actions.Should().Equal(AuditAction.Created);
    }

    private sealed class MemoryActualTransactionStore : IActualTransactionStore
    {
        public List<ActualTransaction> Items { get; } = [];

        public Task<IReadOnlyList<ActualTransaction>> GetByDateRangeAsync(
            DateOnly from, DateOnly to, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ActualTransaction>>(Items.Where(x => x.Date >= from && x.Date <= to).ToList());

        public Task<ActualTransactionWriteResult> AddAsync(
            ActualTransaction transaction, CancellationToken ct = default)
        {
            var existing = Items.SingleOrDefault(x => x.Id == transaction.Id);
            if (existing is not null)
            {
                return Task.FromResult(new ActualTransactionWriteResult(existing, false));
            }

            Items.Add(transaction);
            return Task.FromResult(new ActualTransactionWriteResult(transaction, true));
        }
    }

    private sealed class FixedHouseholdSettingsService(DateOnly today) : IHouseholdSettingsService
    {
        public Task<DateOnly> GetTodayAsync(CancellationToken ct = default) => Task.FromResult(today);
        public Task<DateOnly> GetOccurrenceTrackingStartDateAsync(CancellationToken ct = default) => Task.FromResult(today);
        public Task<BudgetPilot.Application.Dtos.HouseholdSettingsDto> GetAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<BudgetPilot.Application.Dtos.HouseholdSettingsDto> UpdateAsync(BudgetPilot.Application.Requests.UpdateHouseholdSettingsRequest request, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
