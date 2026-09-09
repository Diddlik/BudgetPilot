using BudgetPilot.Application.Dtos;
using BudgetPilot.Application.Requests;
using BudgetPilot.Application.Services;

namespace BudgetPilot.Application.Tests.Fakes;

internal sealed class FakeHouseholdSettingsService(DateOnly trackingStart) : IHouseholdSettingsService
{
    public Task<DateOnly> GetOccurrenceTrackingStartDateAsync(CancellationToken ct = default) =>
        Task.FromResult(trackingStart);

    public Task<DateOnly> GetTodayAsync(CancellationToken ct = default) => Task.FromResult(trackingStart);
    public Task<HouseholdSettingsDto> GetAsync(CancellationToken ct = default) => throw new NotSupportedException();
    public Task<HouseholdSettingsDto> UpdateAsync(UpdateHouseholdSettingsRequest request, CancellationToken ct = default) =>
        throw new NotSupportedException();
}
