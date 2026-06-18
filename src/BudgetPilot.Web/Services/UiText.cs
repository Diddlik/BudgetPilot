using BudgetPilot.Domain.Enums;

namespace BudgetPilot.Web.Services;

public static class UiText
{
    public static string TypeLabel(BudgetItemType type) => type switch
    {
        BudgetItemType.Income => "Einnahme",
        BudgetItemType.Expense => "Ausgabe",
        _ => type.ToString()
    };

    public static string FrequencyLabel(BudgetFrequency frequency) => frequency switch
    {
        BudgetFrequency.Monthly => "Monatlich",
        BudgetFrequency.Quarterly => "Quartalsweise",
        BudgetFrequency.Yearly => "Jährlich",
        BudgetFrequency.Once => "Einmalig",
        _ => frequency.ToString()
    };

    public static string ViewModeLabel(BudgetViewMode viewMode) => viewMode switch
    {
        BudgetViewMode.Budget => "Budget-Sicht",
        BudgetViewMode.Cashflow => "Cashflow-Sicht",
        _ => viewMode.ToString()
    };
}
