namespace BudgetPilot.Mobile;

public partial class App : Microsoft.Maui.Controls.Application
{
    private readonly Services.AppLockService appLock;

    public App(Services.AppLockService appLock)
    {
        this.appLock = appLock;
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new MainPage());
        window.Stopped += (_, _) => appLock.Lock();
        return window;
    }
}
