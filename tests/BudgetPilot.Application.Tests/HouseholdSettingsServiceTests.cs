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
    public async Task UpdateTimeZoneAsync_PersistsPortableIanaIdAndChangesToday()
    {
        var store = new MemoryStore();
        var service = Build(store, new DateTimeOffset(2026, 1, 1, 2, 30, 0, TimeSpan.Zero));

        var result = await service.UpdateTimeZoneAsync("America/New_York");

        store.TimeZoneId.Should().Be("America/New_York");
        result.Today.Should().Be(new DateOnly(2025, 12, 31));
    }

    [Fact]
    public async Task UpdateTimeZoneAsync_RejectsUnknownTimeZone()
    {
        var service = Build((string?)null, DateTimeOffset.UtcNow);

        var act = () => service.UpdateTimeZoneAsync("Mars/Olympus_Mons");

        await act.Should().ThrowAsync<DomainException>();
    }

    private static HouseholdSettingsService Build(string? timeZoneId, DateTimeOffset utcNow) =>
        Build(new MemoryStore { TimeZoneId = timeZoneId }, utcNow);

    private static HouseholdSettingsService Build(MemoryStore store, DateTimeOffset utcNow) =>
        new(store, new NoopAuditLog(), new FixedTimeProvider(utcNow));

    private sealed class MemoryStore : IHouseholdSettingsStore
    {
        public string? TimeZoneId { get; set; }

        public Task<string?> GetTimeZoneIdAsync(CancellationToken ct = default) =>
            Task.FromResult(TimeZoneId);

        public Task SetTimeZoneIdAsync(string timeZoneId, CancellationToken ct = default)
        {
            TimeZoneId = timeZoneId;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
