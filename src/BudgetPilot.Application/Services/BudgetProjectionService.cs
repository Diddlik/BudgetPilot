using BudgetPilot.Application.Abstractions;
using BudgetPilot.Application.Dtos;
using BudgetPilot.Domain.Enums;

namespace BudgetPilot.Application.Services;

/// <summary>
/// STUB (Wave 0). Implementierung der Projektionsregeln (requirements.md §5) folgt in Track A.
/// Verträge eingefroren — nur Methoden-Bodies füllen.
/// </summary>
public sealed class BudgetProjectionService : IBudgetProjectionService
{
    private readonly IBudgetItemRepository _items;

    public BudgetProjectionService(IBudgetItemRepository items)
    {
        _items = items;
    }

    public Task<MonthlyBudgetProjectionDto> GetMonthlyProjectionAsync(
        int year, int month, BudgetViewMode viewMode, CancellationToken ct = default)
        => throw new NotImplementedException("Track A: Monatsprojektion gemäß requirements.md §5.");

    public Task<YearlyBudgetProjectionDto> GetYearlyProjectionAsync(
        int year, BudgetViewMode viewMode, CancellationToken ct = default)
        => throw new NotImplementedException("Track A: Jahresprojektion gemäß requirements.md §5.");
}
