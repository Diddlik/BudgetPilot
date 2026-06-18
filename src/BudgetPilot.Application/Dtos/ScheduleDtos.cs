using BudgetPilot.Domain.Enums;

namespace BudgetPilot.Application.Dtos;

/// <summary>Eine konkrete (Cashflow-)Fälligkeit einer Position in einem bestimmten Monat/Jahr.</summary>
public class PaymentScheduleEntry
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }
    public BudgetFrequency Frequency { get; set; }
}

/// <summary>Jahressummen einer Position oder des Gesamtbudgets (für die Mehrjahres-Übersicht).</summary>
public class YearSummaryDto
{
    public int Year { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Balance { get; set; }
}
