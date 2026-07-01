using AndroidX.Biometric;
using AndroidX.Core.Content;
using AndroidX.Fragment.App;

namespace BudgetPilot.Mobile.Services;

public sealed class BiometricAuthenticationService
{
    private const string EnabledKey = "budgetpilot.biometric.enabled";
    private const int Authenticators = BiometricManager.Authenticators.BiometricWeak
        | BiometricManager.Authenticators.BiometricStrong;

    public bool IsEnabled => Preferences.Default.Get(EnabledKey, false);

    public BiometricAvailability GetAvailability()
    {
        var manager = BiometricManager.From(Platform.AppContext);
        return manager.CanAuthenticate(Authenticators) switch
        {
            BiometricManager.BiometricSuccess => BiometricAvailability.Available,
            BiometricManager.BiometricErrorNoneEnrolled => BiometricAvailability.NotEnrolled,
            BiometricManager.BiometricErrorNoHardware => BiometricAvailability.NoHardware,
            BiometricManager.BiometricErrorHwUnavailable => BiometricAvailability.Unavailable,
            _ => BiometricAvailability.Unavailable
        };
    }

    public async Task<BiometricAuthenticationResult> AuthenticateAsync(
        string subtitle,
        CancellationToken cancellationToken = default)
    {
        if (GetAvailability() != BiometricAvailability.Available)
        {
            return new(false, false, "Biometrische Anmeldung ist auf diesem Gerät nicht verfügbar.");
        }

        if (Platform.CurrentActivity is not FragmentActivity activity)
        {
            return new(false, false, "Das Android-Fenster ist noch nicht bereit.");
        }

        var completion = new TaskCompletionSource<BiometricAuthenticationResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var callback = new AuthenticationCallback(completion);
        var executor = ContextCompat.GetMainExecutor(activity)
            ?? throw new InvalidOperationException("Android konnte keinen UI-Executor bereitstellen.");
        var prompt = new BiometricPrompt(activity, executor, callback);
        var promptInfo = new BiometricPrompt.PromptInfo.Builder()
            .SetTitle("BudgetPilot entsperren")
            .SetSubtitle(subtitle)
            .SetAllowedAuthenticators(Authenticators)
            .SetNegativeButtonText("PIN verwenden")
            .SetConfirmationRequired(false)
            .Build();

        using var registration = cancellationToken.Register(() =>
        {
            prompt.CancelAuthentication();
            completion.TrySetCanceled(cancellationToken);
        });

        prompt.Authenticate(promptInfo);
        return await completion.Task;
    }

    public void SetEnabled(bool enabled)
    {
        if (enabled && GetAvailability() != BiometricAvailability.Available)
        {
            throw new InvalidOperationException("Biometrie ist nicht eingerichtet oder derzeit nicht verfügbar.");
        }

        Preferences.Default.Set(EnabledKey, enabled);
    }

    private sealed class AuthenticationCallback(
        TaskCompletionSource<BiometricAuthenticationResult> completion)
        : BiometricPrompt.AuthenticationCallback
    {
        public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult result)
        {
            base.OnAuthenticationSucceeded(result);
            completion.TrySetResult(new(true, false, null));
        }

        public override void OnAuthenticationFailed()
        {
            base.OnAuthenticationFailed();
            // Der Systemdialog bleibt offen und erlaubt einen weiteren Versuch.
        }

        public override void OnAuthenticationError(int errorCode, Java.Lang.ICharSequence errorMessage)
        {
            base.OnAuthenticationError(errorCode, errorMessage);
            var cancelled = errorCode is BiometricPrompt.ErrorNegativeButton
                or BiometricPrompt.ErrorUserCanceled
                or BiometricPrompt.ErrorCanceled;
            completion.TrySetResult(new(false, cancelled, cancelled ? null : errorMessage.ToString()));
        }
    }
}

public enum BiometricAvailability
{
    Available,
    NotEnrolled,
    NoHardware,
    Unavailable
}

public sealed record BiometricAuthenticationResult(bool Success, bool Cancelled, string? ErrorMessage);
