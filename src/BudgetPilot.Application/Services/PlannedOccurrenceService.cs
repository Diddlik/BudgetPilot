using BudgetPilot.Application.Abstractions;
using BudgetPilot.Application.Dtos;
using BudgetPilot.Domain.Enums;
using BudgetPilot.Domain.Rules;

namespace BudgetPilot.Application.Services;

public interface IPlannedOccurrenceService
{
    Task<IReadOnlyList<PlannedOccurrenceDto>> GetOpenAsync(
        DateOnly? from,
        DateOnly to,
        CancellationToken ct = default);

    Task<IReadOnlyList<PlannedOccurrenceDto>> GetAssumedActualsAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default);
}

public sealed class PlannedOccurrenceService : IPlannedOccurrenceService
{
    private readonly IBudgetItemRepository items;
    private readonly IResolvedOccurrenceStore resolved;
    private readonly IHouseholdSettingsService household;

    public PlannedOccurrenceService(
        IBudgetItemRepository items,
        IResolvedOccurrenceStore resolved,
        IHouseholdSettingsService household)
    {
        this.items = items;
        this.resolved = resolved;
        this.household = household;
    }

    public async Task<IReadOnlyList<PlannedOccurrenceDto>> GetOpenAsync(
        DateOnly? from,
        DateOnly to,
        CancellationToken ct = default)
    {
        var trackingStart = await household.GetOccurrenceTrackingStartDateAsync(ct);
        return await GetUnresolvedAsync(from ?? trackingStart, to, trackingStart, ct);
    }

    public Task<IReadOnlyList<PlannedOccurrenceDto>> GetAssumedActualsAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default) => GetUnresolvedAsync(from, to, null, ct);

    private async Task<IReadOnlyList<PlannedOccurrenceDto>> GetUnresolvedAsync(
        DateOnly from,
        DateOnly to,
        DateOnly? lowerBound,
        CancellationToken ct)
    {
        var allItems = await items.GetAllWithVersionsAsync(ct);
        var earliestVersion = allItems
            .SelectMany(item => item.Versions)
            .Select(version => (DateOnly?)version.ValidFrom)
            .Min() ?? to;
        var effectiveFrom = from;
        if (lowerBound is { } start && effectiveFrom < start)
        {
            effectiveFrom = start;
        }
        if (effectiveFrom < earliestVersion)
        {
            effectiveFrom = earliestVersion;
        }
        if (to < effectiveFrom)
        {
            return [];
        }

        var resolvedKeys = await resolved.GetResolvedKeysAsync(effectiveFrom, to, ct);
        var result = new List<PlannedOccurrenceDto>();

        for (var month = new DateOnly(effectiveFrom.Year, effectiveFrom.Month, 1);
             month <= to;
             month = month.AddMonths(1))
        {
            foreach (var item in allItems)
            {
                var version = ProjectionRules.SelectValidVersion(item, month.Year, month.Month);
                if (version is null)
                {
                    continue;
                }

                var amount = ProjectionRules.ProjectedMonthlyAmount(
                    version, month.Year, month.Month, BudgetViewMode.Cashflow, out var isDue);
                if (!isDue)
                {
                    continue;
                }

                var dueDate = new DateOnly(
                    month.Year,
                    month.Month,
                    Math.Min(version.PaymentDay ?? version.ValidFrom.Day, DateTime.DaysInMonth(month.Year, month.Month)));
                if (dueDate < effectiveFrom || dueDate > to || dueDate < version.ValidFrom || dueDate > version.ValidTo)
                {
                    continue;
                }
                if (!item.IsActive && (item.DeactivatedOn is null || dueDate > item.DeactivatedOn))
                {
                    continue;
                }

                var key = $"{item.Id:N}:{dueDate:yyyy-MM-dd}";
                if (!resolvedKeys.Contains(key))
                {
                    result.Add(new PlannedOccurrenceDto(
                        key, item.Id, item.Name, item.CategoryId, item.Category?.Name ?? string.Empty,
                        item.Type, dueDate, amount));
                }
            }
        }

        return result.OrderBy(entry => entry.DueDate).ThenBy(entry => entry.BudgetItemName).ToList();
    }
}
