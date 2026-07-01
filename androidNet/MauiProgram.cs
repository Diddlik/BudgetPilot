using BudgetPilot.Mobile.Services;
using Microsoft.AspNetCore.Components.WebView.Maui;

namespace BudgetPilot.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder.UseMauiApp<App>();

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddSingleton<AppSession>();
        builder.Services.AddSingleton<AppLockService>();
        builder.Services.AddSingleton<BiometricAuthenticationService>();
        builder.Services.AddSingleton<OfflineCache>();
        builder.Services.AddSingleton<MobilePreferences>();
        builder.Services.AddSingleton<BudgetPilotApiClient>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        return builder.Build();
    }
}
