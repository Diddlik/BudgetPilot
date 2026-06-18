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

    /// <summary>
    /// Cashflow-Zahlungsreihe einer einzelnen Position über mehrere Jahre
    /// (alle fälligen Zahlungen im Bereich [fromYear, toYear], aufsteigend).
    /// </summary>
    Task<IReadOnlyList<PaymentScheduleEntry>> GetPaymentScheduleAsync(
        Guid budgetItemId, int fromYear, int toYear, CancellationToken ct = default);

    /// <summary>Jahressummen (Einnahmen/Ausgaben/Saldo) je Jahr im Bereich [fromYear, toYear].</summary>
    Task<IReadOnlyList<YearSummaryDto>> GetMultiYearSummaryAsync(
        int fromYear, int toYear, BudgetViewMode viewMode, CancellationToken ct = default);
}
