using BudgetPilot.Application.Dtos;
using BudgetPilot.Domain.Enums;

namespace BudgetPilot.Application.Services;

/// <summary>
/// Erzeugt deterministische Monats- und Jahresprojektionen in Budget- bzw. Cashflow-Sicht
/// (Berechnungsregeln siehe requirements.md §5). Implementierung: Track A.
/// </summary>
public interface IBudgetProjectionService
{
    Task<MonthlyBudgetProjectionDto> GetMonthlyProjectionAsync(
        int year, int month, BudgetViewMode viewMode, CancellationToken ct = default);

    Task<YearlyBudgetProjectionDto> GetYearlyProjectionAsync(
        int year, BudgetViewMode viewMode, CancellationToken ct = default);
}
