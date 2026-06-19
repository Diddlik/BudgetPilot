namespace BudgetPilot.Domain.Enums;

/// <summary>Art einer Budgetposition.</summary>
public enum BudgetItemType
{
    Income = 1,
    Expense = 2
}

/// <summary>Rhythmus, in dem ein Betrag anfällt.</summary>
public enum BudgetFrequency
{
    Monthly = 1,
    Quarterly = 2,
    Yearly = 3,
    Once = 4
}

/// <summary>Art einer protokollierten Änderung (Änderungsprotokoll, „wer hat was angepasst").</summary>
public enum AuditAction
{
    Created = 1,
    Updated = 2,
    VersionAdded = 3,
    Deactivated = 4,
    Reactivated = 5,
    Deleted = 6
}

/// <summary>Berechnungsart der Projektion.</summary>
public enum BudgetViewMode
{
    /// <summary>Lumpy Kosten anteilig auf Monate verteilt.</summary>
    Budget = 1,

    /// <summary>Beträge im tatsächlichen Zahlungsmonat.</summary>
    Cashflow = 2
}
