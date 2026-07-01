using BudgetPilot.Domain.Enums;

namespace BudgetPilot.Mobile.Services;

public sealed class MobilePreferences
{
    private const string DefaultViewModeKey = "budgetpilot.preferences.defaultViewMode";
    private const string StartPageKey = "budgetpilot.preferences.startPage";

    public BudgetViewMode DefaultViewMode
    {
        get => (BudgetViewMode)Preferences.Default.Get(DefaultViewModeKey, (int)BudgetViewMode.Budget);
        set => Preferences.Default.Set(DefaultViewModeKey, (int)value);
    }

    public MobileStartPage StartPage
    {
        get => (MobileStartPage)Preferences.Default.Get(StartPageKey, (int)MobileStartPage.Month);
        set => Preferences.Default.Set(StartPageKey, (int)value);
    }

    public string StartRoute => StartPage == MobileStartPage.Year ? "/year" : "/dashboard";
}

public enum MobileStartPage
{
    Month = 1,
    Year = 2
}
