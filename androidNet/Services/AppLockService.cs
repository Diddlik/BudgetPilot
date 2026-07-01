using System.Security.Cryptography;

namespace BudgetPilot.Mobile.Services;

public sealed class AppLockService
{
    private const string EnabledKey = "budgetpilot.appLock.enabled";
    private const string HashKey = "budgetpilot.appLock.hash";
    private const string SaltKey = "budgetpilot.appLock.salt";
    private bool loaded;

    public event Action? StateChanged;

    public bool IsEnabled { get; private set; }
    public bool IsUnlocked { get; private set; } = true;

    public async Task LoadAsync()
    {
        if (loaded)
        {
            return;
        }

        IsEnabled = Preferences.Default.Get(EnabledKey, false);
        IsUnlocked = !IsEnabled;
        loaded = true;
        await Task.CompletedTask;
    }

    public async Task ConfigureAsync(string pin)
    {
        PinPolicy.Validate(pin);
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Hash(pin, salt);
        await SecureStorage.Default.SetAsync(SaltKey, Convert.ToBase64String(salt));
        await SecureStorage.Default.SetAsync(HashKey, Convert.ToBase64String(hash));
        Preferences.Default.Set(EnabledKey, true);
        IsEnabled = true;
        IsUnlocked = true;
        loaded = true;
        StateChanged?.Invoke();
    }

    public async Task<bool> UnlockAsync(string pin)
    {
        await LoadAsync();
        if (!IsEnabled)
        {
            IsUnlocked = true;
            return true;
        }

        var saltValue = await SecureStorage.Default.GetAsync(SaltKey);
        var hashValue = await SecureStorage.Default.GetAsync(HashKey);
        if (string.IsNullOrWhiteSpace(saltValue) || string.IsNullOrWhiteSpace(hashValue))
        {
            return false;
        }

        var actual = Hash(pin, Convert.FromBase64String(saltValue));
        IsUnlocked = CryptographicOperations.FixedTimeEquals(actual, Convert.FromBase64String(hashValue));
        StateChanged?.Invoke();
        return IsUnlocked;
    }

    public void UnlockAfterBiometricAuthentication()
    {
        if (!IsEnabled)
        {
            return;
        }

        IsUnlocked = true;
        StateChanged?.Invoke();
    }

    public Task DisableAsync()
    {
        Preferences.Default.Remove(EnabledKey);
        SecureStorage.Default.Remove(HashKey);
        SecureStorage.Default.Remove(SaltKey);
        IsEnabled = false;
        IsUnlocked = true;
        loaded = true;
        StateChanged?.Invoke();
        return Task.CompletedTask;
    }

    public void Lock()
    {
        if (!IsEnabled)
        {
            return;
        }

        IsUnlocked = false;
        StateChanged?.Invoke();
    }

    private static byte[] Hash(string pin, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(pin, salt, 120_000, HashAlgorithmName.SHA256, 32);
}
