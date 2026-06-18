using BudgetPilot.Domain.Enums;

namespace BudgetPilot.Application.Dtos;

/// <summary>Eine Zeile einer Monatsprojektion (eine Budgetposition in der gewählten Sicht).</summary>
public class BudgetProjectionLine
{
    public Guid BudgetItemId { get; set; }
    public string BudgetItemName { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public BudgetItemType Type { get; set; }
    public BudgetFrequency Frequency { get; set; }

    /// <summary>Originalbetrag der gültigen Version.</summary>
    public decimal Amount { get; set; }

    /// <summary>Der für diesen Monat in der gewählten Sicht angesetzte Betrag.</summary>
    public decimal ProjectedMonthlyAmount { get; set; }

    /// <summary>
    /// Cashflow-Sicht: Zahlung ist in diesem Monat fällig. Budget-Sicht: Betrag wird anteilig
    /// angesetzt (true bei lumpy Kosten Quarterly/Yearly).
    /// </summary>
    public bool IsDue { get; set; }

    public string? Note { get; set; }
}

/// <summary>Summe der Ausgaben (bzw. Beträge) einer Kategorie in der gewählten Sicht.</summary>
public class CategoryProjectionSummary
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}

/// <summary>Projektion eines einzelnen Monats.</summary>
public class MonthlyBudgetProjectionDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public BudgetViewMode ViewMode { get; set; }

    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Balance { get; set; }

    public List<BudgetProjectionLine> Lines { get; set; } = new();
    public List<CategoryProjectionSummary> Categories { get; set; } = new();
}

/// <summary>Projektion eines ganzen Jahres (12 Monatsprojektionen + Jahressummen).</summary>
public class YearlyBudgetProjectionDto
{
    public int Year { get; set; }
    public BudgetViewMode ViewMode { get; set; }
    public List<MonthlyBudgetProjectionDto> Months { get; set; } = new();

    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Balance { get; set; }
}
