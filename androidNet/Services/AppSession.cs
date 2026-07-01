namespace BudgetPilot.Mobile.Services;

public sealed class AppSession
{
    private const string BaseUrlKey = "budgetpilot.baseUrl";
    private const string AccessTokenKey = "budgetpilot.accessToken";
    private const string RefreshTokenKey = "budgetpilot.refreshToken";
    private const string AccountEmailKey = "budgetpilot.accountEmail";

    public string? BaseUrl { get; private set; }
    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public string? AccountEmail { get; private set; }
    public bool IsLoaded { get; private set; }
    public bool IsSignedIn => !string.IsNullOrWhiteSpace(BaseUrl)
        && !string.IsNullOrWhiteSpace(AccessToken)
        && !string.IsNullOrWhiteSpace(RefreshToken);

    public async Task LoadAsync()
    {
        if (IsLoaded)
        {
            return;
        }

        BaseUrl = Preferences.Default.Get(BaseUrlKey, string.Empty);
        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            BaseUrl = null;
        }
        AccessToken = await SecureStorage.Default.GetAsync(AccessTokenKey);
        RefreshToken = await SecureStorage.Default.GetAsync(RefreshTokenKey);
        AccountEmail = Preferences.Default.Get(AccountEmailKey, string.Empty);
        if (string.IsNullOrWhiteSpace(AccountEmail))
        {
            AccountEmail = null;
        }
        IsLoaded = true;
    }

    public Task SetBaseUrlAsync(string value)
    {
        var normalized = NormalizeBaseUrl(value);
        BaseUrl = normalized;
        Preferences.Default.Set(BaseUrlKey, normalized);
        IsLoaded = true;
        return Task.CompletedTask;
    }

    public async Task StoreTokensAsync(TokenResponse response)
    {
        AccessToken = response.AccessToken;
        RefreshToken = response.RefreshToken;
        IsLoaded = true;

        await SecureStorage.Default.SetAsync(AccessTokenKey, response.AccessToken);
        await SecureStorage.Default.SetAsync(RefreshTokenKey, response.RefreshToken);
    }

    public Task StoreAccountEmailAsync(string email)
    {
        AccountEmail = email.Trim();
        Preferences.Default.Set(AccountEmailKey, AccountEmail);
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        AccessToken = null;
        RefreshToken = null;
        SecureStorage.Default.Remove(AccessTokenKey);
        SecureStorage.Default.Remove(RefreshTokenKey);
        return Task.CompletedTask;
    }

    private static string NormalizeBaseUrl(string value)
    {
        var candidate = value.Trim();
        if (string.IsNullOrWhiteSpace(candidate) || candidate is "https://" or "http://")
        {
            throw new InvalidOperationException("Bitte eine gültige Instanz-URL eingeben, z. B. http://10.0.2.2:5070.");
        }

        if (!candidate.Contains("://", StringComparison.Ordinal))
        {
            candidate = IsLocalHost(candidate) ? $"http://{candidate}" : $"https://{candidate}";
        }

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri) || string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new InvalidOperationException("Bitte eine gültige Instanz-URL eingeben, z. B. http://10.0.2.2:5070.");
        }

        var isLocalDev = uri.Scheme == Uri.UriSchemeHttp && IsLocalHost(uri.Host);

        if (uri.Scheme != Uri.UriSchemeHttps && !isLocalDev)
        {
            throw new InvalidOperationException("Aus Sicherheitsgründen ist HTTPS erforderlich. HTTP ist nur für lokale Emulator-Tests erlaubt.");
        }

        return uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
    }

    private static bool IsLocalHost(string value)
    {
        var host = value.Split(':', 2)[0];
        return host.Equals("10.0.2.2", StringComparison.OrdinalIgnoreCase)
            || host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);
    }
}
