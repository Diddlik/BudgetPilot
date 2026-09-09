using BudgetPilot.Application.Abstractions;
using BudgetPilot.Application.Requests;
using BudgetPilot.Application.Services;
using BudgetPilot.Application.Tests.Fakes;
using BudgetPilot.Domain.Entities;
using BudgetPilot.Domain.Enums;
using FluentAssertions;

namespace BudgetPilot.Application.Tests;

public sealed class OccurrenceActionServiceTests
{
    [Fact]
    public async Task ConfirmAsync_UsesPlannedValues_AndIsIdempotent()
    {
        var data = new InMemoryStore();
        var category = SeedData.Category("Wohnen");
        var item = SeedData.Item("Miete", BudgetItemType.Expense, category,
            SeedData.Version(1200m, BudgetFrequency.Monthly, new DateOnly(2026, 1, 5), paymentDay: 5));
        data.Categories.Add(category);
        data.Items.Add(item);
        var resolutions = new FakeOccurrenceResolutionStore();
        var planned = new PlannedOccurrenceService(new FakeBudgetItemRepository(data), resolutions);
        var service = new OccurrenceActionService(planned, resolutions, new NoopAuditLog());

        var first = await service.ConfirmAsync(item.Id, new DateOnly(2026, 2, 5), new ConfirmOccurrenceRequest());
        var repeated = await service.ConfirmAsync(item.Id, new DateOnly(2026, 2, 5), new ConfirmOccurrenceRequest());

        first.Kind.Should().Be(OccurrenceResolutionKind.Confirmed);
        first.ActualAmount.Should().Be(1200m);
        first.ActualDate.Should().Be(new DateOnly(2026, 2, 5));
        repeated.Should().Be(first);
        resolutions.Transactions.Should().ContainSingle();
    }

    [Fact]
    public async Task ConfirmAsync_PersistsAdjustedValues()
    {
        var (item, service, resolutions) = Setup();

        var result = await service.ConfirmAsync(
            item.Id,
            new DateOnly(2026, 2, 5),
            new ConfirmOccurrenceRequest(1195.50m, new DateOnly(2026, 2, 7), "Korrigierter Betrag"));

        result.ActualAmount.Should().Be(1195.50m);
        result.ActualDate.Should().Be(new DateOnly(2026, 2, 7));
        result.Note.Should().Be("Korrigierter Betrag");
        resolutions.Transactions.Should().ContainSingle(x => x.Amount == 1195.50m);
    }

    [Fact]
    public async Task SkipAsync_IsIdempotent_AndCannotThenBeConfirmed()
    {
        var (item, service, resolutions) = Setup();

        var skipped = await service.SkipAsync(
            item.Id,
            new DateOnly(2026, 2, 5),
            new SkipOccurrenceRequest("Vertrag beendet"));
        var repeated = await service.SkipAsync(
            item.Id,
            new DateOnly(2026, 2, 5),
            new SkipOccurrenceRequest());
        var confirm = () => service.ConfirmAsync(
            item.Id,
            new DateOnly(2026, 2, 5),
            new ConfirmOccurrenceRequest());

        skipped.Kind.Should().Be(OccurrenceResolutionKind.Skipped);
        skipped.Note.Should().Be("Vertrag beendet");
        repeated.Should().Be(skipped);
        resolutions.Transactions.Should().BeEmpty();
        await confirm.Should().ThrowAsync<BudgetPilot.Domain.Exceptions.DomainException>()
            .WithMessage("*entfallen*");
    }

    [Fact]
    public async Task CorrectThenReopen_ChangesOnlyActual_AndMakesOccurrenceOpenAgain()
    {
        var (item, service, resolutions) = Setup();
        var dueDate = new DateOnly(2026, 2, 5);
        await service.ConfirmAsync(item.Id, dueDate, new ConfirmOccurrenceRequest());

        var corrected = await service.CorrectAsync(
            item.Id,
            dueDate,
            new CorrectOccurrenceRequest(1188m, new DateOnly(2026, 2, 8), "Kontoauszug"));
        await service.ReopenAsync(item.Id, dueDate);

        corrected.ActualAmount.Should().Be(1188m);
        corrected.ActualDate.Should().Be(new DateOnly(2026, 2, 8));
        item.Versions.Single().Amount.Should().Be(1200m);
        resolutions.Transactions.Should().BeEmpty();
        resolutions.Resolutions.Should().BeEmpty();
    }

    [Fact]
    public async Task Actions_AreWrittenToAuditLog()
    {
        var audit = new RecordingAuditLog();
        var (item, service, _) = Setup(audit);
        var firstDue = new DateOnly(2026, 2, 5);
        var secondDue = new DateOnly(2026, 3, 5);

        await service.ConfirmAsync(item.Id, firstDue, new ConfirmOccurrenceRequest());
        await service.CorrectAsync(item.Id, firstDue, new CorrectOccurrenceRequest(1190m, firstDue, null));
        await service.ReopenAsync(item.Id, firstDue);
        await service.SkipAsync(item.Id, secondDue, new SkipOccurrenceRequest());

        audit.Actions.Should().Equal(
            AuditAction.Created,
            AuditAction.Updated,
            AuditAction.Reactivated,
            AuditAction.Deactivated);
    }

    private static (BudgetItem Item, OccurrenceActionService Service, FakeOccurrenceResolutionStore Resolutions) Setup(
        IAuditLog? audit = null)
    {
        var data = new InMemoryStore();
        var category = SeedData.Category("Wohnen");
        var item = SeedData.Item("Miete", BudgetItemType.Expense, category,
            SeedData.Version(1200m, BudgetFrequency.Monthly, new DateOnly(2026, 1, 5), paymentDay: 5));
        data.Categories.Add(category);
        data.Items.Add(item);
        var resolutions = new FakeOccurrenceResolutionStore();
        var planned = new PlannedOccurrenceService(new FakeBudgetItemRepository(data), resolutions);
        return (item, new OccurrenceActionService(planned, resolutions, audit ?? new NoopAuditLog()), resolutions);
    }
}

internal sealed class RecordingAuditLog : IAuditLog
{
    public List<AuditAction> Actions { get; } = [];

    public Task RecordAsync(
        AuditAction action,
        string entityType,
        Guid entityId,
        string entityName,
        string? details = null,
        CancellationToken ct = default)
    {
        Actions.Add(action);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<BudgetPilot.Application.Dtos.AuditEntryDto>> GetRecentAsync(
        int max = 200,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<BudgetPilot.Application.Dtos.AuditEntryDto>>([]);
}

internal sealed class FakeOccurrenceResolutionStore : IResolvedOccurrenceStore
{
    public List<OccurrenceResolution> Resolutions { get; } = [];
    public List<ActualTransaction> Transactions { get; } = [];

    public Task<IReadOnlySet<string>> GetResolvedKeysAsync(DateOnly from, DateOnly to, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlySet<string>>(Resolutions.Select(x => x.OccurrenceKey).ToHashSet());

    public Task<OccurrenceResolution?> GetAsync(string occurrenceKey, CancellationToken ct = default) =>
        Task.FromResult(Resolutions.SingleOrDefault(x => x.OccurrenceKey == occurrenceKey));

    public Task<OccurrenceResolution> AddAsync(OccurrenceResolution resolution, ActualTransaction? transaction, CancellationToken ct = default)
    {
        if (Resolutions.SingleOrDefault(x => x.OccurrenceKey == resolution.OccurrenceKey) is { } existing)
        {
            return Task.FromResult(existing);
        }
        if (transaction is not null)
        {
            Transactions.Add(transaction);
            resolution.ActualTransaction = transaction;
            resolution.ActualTransactionId = transaction.Id;
        }
        Resolutions.Add(resolution);
        return Task.FromResult(resolution);
    }

    public Task<IReadOnlyList<OccurrenceResolution>> GetRecentAsync(int max, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<OccurrenceResolution>>(Resolutions.OrderByDescending(x => x.CreatedAt).Take(max).ToList());

    public Task<OccurrenceResolution?> UpdateConfirmedAsync(
        string occurrenceKey,
        decimal amount,
        DateOnly date,
        string? description,
        CancellationToken ct = default)
    {
        var resolution = Resolutions.SingleOrDefault(x => x.OccurrenceKey == occurrenceKey);
        if (resolution?.Kind != OccurrenceResolutionKind.Confirmed || resolution.ActualTransaction is null)
        {
            return Task.FromResult<OccurrenceResolution?>(null);
        }
        resolution.ActualTransaction.Amount = amount;
        resolution.ActualTransaction.Date = date;
        resolution.ActualTransaction.Description = description;
        resolution.Note = description;
        return Task.FromResult<OccurrenceResolution?>(resolution);
    }

    public Task<bool> RemoveAsync(string occurrenceKey, CancellationToken ct = default)
    {
        var resolution = Resolutions.SingleOrDefault(x => x.OccurrenceKey == occurrenceKey);
        if (resolution is null)
        {
            return Task.FromResult(false);
        }
        Resolutions.Remove(resolution);
        if (resolution.ActualTransaction is not null)
        {
            Transactions.Remove(resolution.ActualTransaction);
        }
        return Task.FromResult(true);
    }
}
