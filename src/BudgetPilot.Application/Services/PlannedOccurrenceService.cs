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
}

public sealed class PlannedOccurrenceService : IPlannedOccurrenceService
{
    private readonly IBudgetItemRepository items;
    private readonly IResolvedOccurrenceStore resolved;

    public PlannedOccurrenceService(IBudgetItemRepository items, IResolvedOccurrenceStore resolved)
    {
        this.items = items;
        this.resolved = resolved;
    }

    public async Task<IReadOnlyList<PlannedOccurrenceDto>> GetOpenAsync(
        DateOnly? from,
        DateOnly to,
        CancellationToken ct = default)
    {
        var allItems = await items.GetAllWithVersionsAsync(ct);
        var effectiveFrom = from ?? allItems
            .SelectMany(item => item.Versions)
            .Select(version => (DateOnly?)version.ValidFrom)
            .Min() ?? to;
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
