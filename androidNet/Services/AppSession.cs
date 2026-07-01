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
        var normalized = InstanceAddress.Normalize(value);
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

}
