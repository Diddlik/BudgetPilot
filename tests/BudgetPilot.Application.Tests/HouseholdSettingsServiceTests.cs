using BudgetPilot.Application.Abstractions;
using BudgetPilot.Application.Services;
using BudgetPilot.Application.Tests.Fakes;
using BudgetPilot.Domain.Exceptions;
using FluentAssertions;

namespace BudgetPilot.Application.Tests;

public sealed class HouseholdSettingsServiceTests
{
    [Fact]
    public async Task GetTodayAsync_UsesHouseholdTimeZoneAcrossUtcMidnight()
    {
        var service = Build("Europe/Berlin", new DateTimeOffset(2026, 1, 1, 23, 30, 0, TimeSpan.Zero));

        var today = await service.GetTodayAsync();

        today.Should().Be(new DateOnly(2026, 1, 2));
    }

    [Fact]
    public async Task GetAsync_DefaultsMissingTrackingStartToCurrentMonth()
    {
        var service = Build("Europe/Berlin", new DateTimeOffset(2026, 9, 9, 12, 0, 0, TimeSpan.Zero));

        var result = await service.GetAsync();

        result.OccurrenceTrackingStartDate.Should().Be(new DateOnly(2026, 9, 1));
    }

    [Fact]
    public async Task UpdateAsync_PersistsTimeZoneAndTrackingStart()
    {
        var store = new MemoryStore();
        var service = Build(store, new DateTimeOffset(2026, 1, 1, 2, 30, 0, TimeSpan.Zero));

        var trackingStart = new DateOnly(2025, 12, 1);
        var result = await service.UpdateAsync(new("America/New_York", trackingStart));

        store.TimeZoneId.Should().Be("America/New_York");
        store.OccurrenceTrackingStartDate.Should().Be(trackingStart);
        result.Today.Should().Be(new DateOnly(2025, 12, 31));
        result.OccurrenceTrackingStartDate.Should().Be(trackingStart);
    }

    [Fact]
    public async Task UpdateAsync_RejectsUnknownTimeZone()
    {
        var service = Build((string?)null, DateTimeOffset.UtcNow);

        var act = () => service.UpdateAsync(new("Mars/Olympus_Mons", new DateOnly(2026, 1, 1)));

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task UpdateAsync_RejectsTrackingStartInFuture()
    {
        var service = Build("Europe/Berlin", new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero));

        var act = () => service.UpdateAsync(new("Europe/Berlin", new DateOnly(2026, 2, 1)));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*Zukunft*");
    }

    [Fact]
    public async Task UpdateAsync_OmittedTrackingStartPreservesExistingValue()
    {
        var store = new MemoryStore
        {
            TimeZoneId = "Europe/Berlin",
            OccurrenceTrackingStartDate = new DateOnly(2026, 8, 1)
        };
        var service = Build(store, new DateTimeOffset(2026, 9, 9, 12, 0, 0, TimeSpan.Zero));

        var result = await service.UpdateAsync(new("Europe/Berlin"));

        result.OccurrenceTrackingStartDate.Should().Be(new DateOnly(2026, 8, 1));
    }

    private static HouseholdSettingsService Build(string? timeZoneId, DateTimeOffset utcNow) =>
        Build(new MemoryStore { TimeZoneId = timeZoneId }, utcNow);

    private static HouseholdSettingsService Build(MemoryStore store, DateTimeOffset utcNow) =>
        new(store, new NoopAuditLog(), new FixedTimeProvider(utcNow));

    private sealed class MemoryStore : IHouseholdSettingsStore
    {
        public string? TimeZoneId { get; set; }
        public DateOnly? OccurrenceTrackingStartDate { get; set; }

        public Task<string?> GetTimeZoneIdAsync(CancellationToken ct = default) =>
            Task.FromResult(TimeZoneId);

        public Task<DateOnly?> GetOccurrenceTrackingStartDateAsync(CancellationToken ct = default) =>
            Task.FromResult(OccurrenceTrackingStartDate);

        public Task SetAsync(string timeZoneId, DateOnly occurrenceTrackingStartDate, CancellationToken ct = default)
        {
            TimeZoneId = timeZoneId;
            OccurrenceTrackingStartDate = occurrenceTrackingStartDate;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
