using BudgetPilot.Application.Abstractions;
using BudgetPilot.Application.Dtos;
using BudgetPilot.Domain.Entities;
using BudgetPilot.Domain.Enums;
using BudgetPilot.Domain.Rules;

namespace BudgetPilot.Application.Services;

/// <summary>
/// Erzeugt deterministische Monats- und Jahresprojektionen (requirements.md §5).
/// Lädt Items inkl. Versionen EINMAL und berechnet die 12 Monate einer Jahresübersicht
/// auf derselben in-memory Datenbasis (kein N+1, §5.3).
/// </summary>
public sealed class BudgetProjectionService : IBudgetProjectionService
{
    private readonly IBudgetItemRepository _items;

    public BudgetProjectionService(IBudgetItemRepository items)
    {
        _items = items;
    }

    public async Task<MonthlyBudgetProjectionDto> GetMonthlyProjectionAsync(
        int year, int month, BudgetViewMode viewMode, CancellationToken ct = default)
    {
        var items = await _items.GetAllWithVersionsAsync(ct).ConfigureAwait(false);
        return BuildMonth(items, year, month, viewMode);
    }

    public async Task<YearlyBudgetProjectionDto> GetYearlyProjectionAsync(
        int year, BudgetViewMode viewMode, CancellationToken ct = default)
    {
        // Eine einzige Repo-Abfrage; 12 Monatsprojektionen auf derselben Datenbasis.
        var items = await _items.GetAllWithVersionsAsync(ct).ConfigureAwait(false);

        var result = new YearlyBudgetProjectionDto { Year = year, ViewMode = viewMode };
        for (var m = 1; m <= 12; m++)
        {
            var monthDto = BuildMonth(items, year, m, viewMode);
            result.Months.Add(monthDto);
            result.TotalIncome += monthDto.TotalIncome;
            result.TotalExpense += monthDto.TotalExpense;
        }

        result.Balance = result.TotalIncome - result.TotalExpense;
        return result;
    }

    public async Task<IReadOnlyList<PaymentScheduleEntry>> GetPaymentScheduleAsync(
        Guid budgetItemId, int fromYear, int toYear, CancellationToken ct = default)
    {
        var entries = new List<PaymentScheduleEntry>();
        if (toYear < fromYear)
            return entries;

        var item = await _items.GetByIdWithVersionsAsync(budgetItemId, ct).ConfigureAwait(false);
        if (item is null)
            return entries;

        // Zahlungsreihe = tatsächliche Fälligkeiten → immer Cashflow-Sicht, unabhängig von IsActive.
        for (var year = fromYear; year <= toYear; year++)
        {
            for (var month = 1; month <= 12; month++)
            {
                var version = ProjectionRules.SelectValidVersion(item, year, month);
                if (version is null)
                    continue;

                var amount = ProjectionRules.ProjectedMonthlyAmount(
                    version, year, month, BudgetViewMode.Cashflow, out var isDue);
                if (!isDue || amount <= 0m)
                    continue;

                entries.Add(new PaymentScheduleEntry
                {
                    Year = year,
                    Month = month,
                    Amount = amount,
                    Frequency = version.Frequency
                });
            }
        }

        return entries;
    }

    public async Task<IReadOnlyList<YearSummaryDto>> GetMultiYearSummaryAsync(
        int fromYear, int toYear, BudgetViewMode viewMode, CancellationToken ct = default)
    {
        var summaries = new List<YearSummaryDto>();
        if (toYear < fromYear)
            return summaries;

        // Eine einzige Repo-Abfrage; alle Jahre auf derselben Datenbasis berechnen.
        var items = await _items.GetAllWithVersionsAsync(ct).ConfigureAwait(false);

        for (var year = fromYear; year <= toYear; year++)
        {
            var summary = new YearSummaryDto { Year = year };
            for (var m = 1; m <= 12; m++)
            {
                var month = BuildMonth(items, year, m, viewMode);
                summary.TotalIncome += month.TotalIncome;
                summary.TotalExpense += month.TotalExpense;
            }
            summary.Balance = summary.TotalIncome - summary.TotalExpense;
            summaries.Add(summary);
        }

        return summaries;
    }

    private static MonthlyBudgetProjectionDto BuildMonth(
        IReadOnlyList<BudgetItem> items, int year, int month, BudgetViewMode viewMode)
    {
        var dto = new MonthlyBudgetProjectionDto { Year = year, Month = month, ViewMode = viewMode };

        foreach (var item in items)
        {
            // Nur aktive Items fließen in die Projektion ein (§4.4 / Test §8.9).
            if (!item.IsActive)
                continue;

            // §4.2 + §4.1.2: genau eine gültige Version (oder keine) je Monat.
            var version = ProjectionRules.SelectValidVersion(item, year, month);
            if (version is null)
                continue;

            var amount = ProjectionRules.ProjectedMonthlyAmount(version, year, month, viewMode, out var isDue);

            var line = new BudgetProjectionLine
            {
                BudgetItemId = item.Id,
                BudgetItemName = item.Name,
                CategoryId = item.CategoryId,
                CategoryName = item.Category?.Name ?? string.Empty,
                Type = item.Type,
                Frequency = version.Frequency,
                Amount = version.Amount,
                ProjectedMonthlyAmount = amount,
                IsDue = isDue,
                Note = version.Note
            };
            dto.Lines.Add(line);

            if (item.Type == BudgetItemType.Income)
                dto.TotalIncome += amount;
            else
                dto.TotalExpense += amount;
        }

        dto.Balance = dto.TotalIncome - dto.TotalExpense;

        // §5.2: Ausgaben je Kategorie in der gewählten Sicht.
        dto.Categories = dto.Lines
            .Where(l => l.Type == BudgetItemType.Expense)
            .GroupBy(l => (l.CategoryId, l.CategoryName))
            .Select(g => new CategoryProjectionSummary
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.CategoryName,
                TotalAmount = g.Sum(l => l.ProjectedMonthlyAmount)
            })
            .OrderBy(c => c.CategoryName)
            .ToList();

        return dto;
    }
}
