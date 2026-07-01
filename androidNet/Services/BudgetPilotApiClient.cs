using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BudgetPilot.Application.Dtos;
using BudgetPilot.Application.Requests;
using BudgetPilot.Domain.Enums;

namespace BudgetPilot.Mobile.Services;

public sealed class BudgetPilotApiClient
{
    private readonly HttpClient httpClient = new();
    private readonly AppSession session;
    private readonly OfflineCache cache;
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public BudgetPilotApiClient(AppSession session, OfflineCache cache)
    {
        this.session = session;
        this.cache = cache;
        httpClient.Timeout = TimeSpan.FromSeconds(20);
    }

    public bool LastReadWasOffline { get; private set; }
    public DateTime? LastCacheTimestamp { get; private set; }

    public async Task LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("Bitte E-Mail und Passwort eingeben.");
        }

        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsJsonAsync(
                BuildUri("/api/auth/login"),
                new LoginRequest(email.Trim(), password),
                jsonOptions,
                cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            // Server nicht erreichbar, falsche URL, oder HTTP→HTTPS-Redirect/Zertifikat:
            // klar von „falsches Passwort" unterscheiden.
            throw new InvalidOperationException(UnreachableMessage(ex), ex);
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException("E-Mail oder Passwort ist falsch.");
        }

        var tokens = await ReadResponseAsync<TokenResponse>(response, cancellationToken);
        await session.StoreTokensAsync(tokens);
        await session.StoreAccountEmailAsync(email);
    }

    public async Task<MonthlyBudgetProjectionDto> GetMonthlyProjectionAsync(
        int year,
        int month,
        BudgetViewMode mode,
        CancellationToken cancellationToken = default)
    {
        var relative = $"/api/v1/projections/monthly?year={year}&month={month}&mode={mode}";
        return await GetAsync<MonthlyBudgetProjectionDto>(relative, cancellationToken);
    }

    public Task<List<BudgetItemDto>> GetBudgetItemsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<List<BudgetItemDto>>("/api/v1/budget-items", cancellationToken);

    public Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<List<CategoryDto>>("/api/v1/categories", cancellationToken);

    public Task<YearlyBudgetProjectionDto> GetYearlyProjectionAsync(
        int year,
        BudgetViewMode mode,
        CancellationToken cancellationToken = default) =>
        GetAsync<YearlyBudgetProjectionDto>(
            $"/api/v1/projections/yearly?year={year}&mode={mode}",
            cancellationToken);

    public Task<List<YearSummaryDto>> GetMultiYearSummaryAsync(
        int from,
        int to,
        BudgetViewMode mode,
        CancellationToken cancellationToken = default) =>
        GetAsync<List<YearSummaryDto>>(
            $"/api/v1/projections/multi-year?from={from}&to={to}&mode={mode}",
            cancellationToken);

    public Task<List<AuditEntryDto>> GetAuditEntriesAsync(
        int max = 100,
        CancellationToken cancellationToken = default) =>
        GetAsync<List<AuditEntryDto>>($"/api/v1/audit?max={max}", cancellationToken);

    public Task<BudgetItemDto> GetBudgetItemAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetAsync<BudgetItemDto>($"/api/v1/budget-items/{id}", cancellationToken);

    public Task<CategoryDto> CreateCategoryAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<CategoryDto>(HttpMethod.Post, "/api/v1/categories", request, cancellationToken);

    public Task RenameCategoryAsync(
        Guid id,
        RenameCategoryRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Put, $"/api/v1/categories/{id}", request, cancellationToken);

    public Task DeactivateCategoryAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, $"/api/v1/categories/{id}/deactivate", null, cancellationToken);

    public Task<BudgetItemDto> CreateBudgetItemAsync(
        CreateBudgetItemRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<BudgetItemDto>(HttpMethod.Post, "/api/v1/budget-items", request, cancellationToken);

    public Task<BudgetItemDto> UpdateBudgetItemAsync(
        Guid id,
        UpdateBudgetItemMetadataRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<BudgetItemDto>(HttpMethod.Put, $"/api/v1/budget-items/{id}", request, cancellationToken);

    public Task<BudgetItemVersionDto> AddBudgetItemVersionAsync(
        Guid id,
        CreateBudgetItemVersionRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<BudgetItemVersionDto>(HttpMethod.Post, $"/api/v1/budget-items/{id}/versions", request, cancellationToken);

    public Task UpdateBudgetItemVersionAsync(
        Guid itemId,
        Guid versionId,
        UpdateVersionRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Put, $"/api/v1/budget-items/{itemId}/versions/{versionId}", request, cancellationToken);

    public Task SetBudgetItemActiveAsync(
        Guid id,
        bool active,
        CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, $"/api/v1/budget-items/{id}/{(active ? "reactivate" : "deactivate")}", null, cancellationToken);

    public Task DeleteBudgetItemAsync(Guid id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"/api/v1/budget-items/{id}", null, cancellationToken);

    private async Task<T> GetAsync<T>(string relativeUri, CancellationToken cancellationToken)
    {
        LastReadWasOffline = false;
        LastCacheTimestamp = null;
        await session.LoadAsync();

        try
        {
            using var response = await SendAuthorizedGetAsync(relativeUri, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized && await RefreshAsync(cancellationToken))
            {
                using var retry = await SendAuthorizedGetAsync(relativeUri, cancellationToken);
                var retryValue = await ReadResponseAsync<T>(retry, cancellationToken);
                await StoreCacheAsync(relativeUri, retryValue, cancellationToken);
                return retryValue;
            }

            var value = await ReadResponseAsync<T>(response, cancellationToken);
            await StoreCacheAsync(relativeUri, value, cancellationToken);
            return value;
        }
        catch (HttpRequestException ex)
        {
            return await ReadCacheOrThrowAsync<T>(relativeUri, UnreachableMessage(ex), ex, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return await ReadCacheOrThrowAsync<T>(
                relativeUri,
                "Instanz nicht erreichbar. Die Anfrage hat zu lange gedauert.",
                ex,
                cancellationToken);
        }
    }

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string relativeUri,
        object? body,
        CancellationToken cancellationToken)
    {
        using var response = await SendWithRefreshAsync(method, relativeUri, body, cancellationToken);
        var value = await ReadResponseAsync<T>(response, cancellationToken);
        await cache.ClearAsync(cancellationToken);
        return value;
    }

    private async Task SendAsync(
        HttpMethod method,
        string relativeUri,
        object? body,
        CancellationToken cancellationToken)
    {
        using var response = await SendWithRefreshAsync(method, relativeUri, body, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        await cache.ClearAsync(cancellationToken);
    }

    private async Task<HttpResponseMessage> SendWithRefreshAsync(
        HttpMethod method,
        string relativeUri,
        object? body,
        CancellationToken cancellationToken)
    {
        await session.LoadAsync();

        try
        {
            var response = await SendAuthorizedAsync(method, relativeUri, body, cancellationToken);
            if (response.StatusCode != HttpStatusCode.Unauthorized || !await RefreshAsync(cancellationToken))
            {
                return response;
            }

            response.Dispose();
            return await SendAuthorizedAsync(method, relativeUri, body, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(UnreachableMessage(ex), ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException("Instanz nicht erreichbar. Die Anfrage hat zu lange gedauert.", ex);
        }
    }

    private async Task<HttpResponseMessage> SendAuthorizedGetAsync(string relativeUri, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(session.AccessToken))
        {
            throw new UnauthorizedAccessException();
        }

        var request = new HttpRequestMessage(HttpMethod.Get, BuildUri(relativeUri));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return await httpClient.SendAsync(request, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(
        HttpMethod method,
        string relativeUri,
        object? body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(session.AccessToken))
        {
            throw new UnauthorizedAccessException();
        }

        using var request = new HttpRequestMessage(method, BuildUri(relativeUri));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, body.GetType(), options: jsonOptions);
        }

        return await httpClient.SendAsync(request, cancellationToken);
    }

    private async Task<bool> RefreshAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(session.RefreshToken))
        {
            return false;
        }

        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsJsonAsync(
                BuildUri("/api/auth/refresh"),
                new RefreshRequest(session.RefreshToken),
                jsonOptions,
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            return false;
        }

        if (!response.IsSuccessStatusCode)
        {
            await session.ClearAsync();
            return false;
        }

        var tokens = await ReadResponseAsync<TokenResponse>(response, cancellationToken);
        await session.StoreTokensAsync(tokens);
        return true;
    }

    private Uri BuildUri(string relativeUri)
    {
        if (string.IsNullOrWhiteSpace(session.BaseUrl))
        {
            throw new InvalidOperationException("Bitte zuerst die BudgetPilot-Instanz einrichten.");
        }

        return new Uri($"{session.BaseUrl.TrimEnd('/')}/{relativeUri.TrimStart('/')}");
    }

    private Task StoreCacheAsync<T>(string key, T value, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(session.BaseUrl)
            ? Task.CompletedTask
            : cache.StoreAsync(session.BaseUrl, key, value, cancellationToken);

    private async Task<T> ReadCacheOrThrowAsync<T>(
        string key,
        string message,
        Exception innerException,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(session.BaseUrl))
        {
            var cached = await cache.TryReadAsync<T>(session.BaseUrl, key, cancellationToken);
            if (cached is { } entry)
            {
                LastReadWasOffline = true;
                LastCacheTimestamp = entry.CachedAt;
                return entry.Value;
            }
        }

        throw new InvalidOperationException(message, innerException);
    }

    private async Task<T> ReadResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await ReadProblemAsync(response, cancellationToken));
        }

        var value = await response.Content.ReadFromJsonAsync<T>(jsonOptions, cancellationToken);
        if (value is null)
        {
            throw new InvalidOperationException("Die API hat keine verwertbare Antwort geliefert.");
        }

        return value;
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await ReadProblemAsync(response, cancellationToken));
        }
    }

    private async Task<string> ReadProblemAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>(jsonOptions, cancellationToken);
            return problem?.Detail ?? problem?.Title ?? $"API-Fehler {(int)response.StatusCode}";
        }
        catch
        {
            return $"API-Fehler {(int)response.StatusCode}";
        }
    }

    private static string UnreachableMessage(HttpRequestException ex) =>
        "Instanz nicht erreichbar. Prüfe die Instanz-URL und ob der Server läuft " +
        $"(im Emulator: http://10.0.2.2:5070). Details: {ex.Message}";

    private sealed record LoginRequest(string Email, string Password);
    private sealed record RefreshRequest(string RefreshToken);
    private sealed record ApiProblemDetails(string? Title, string? Detail);
}

public sealed record TokenResponse(
    string? TokenType,
    string AccessToken,
    long ExpiresIn,
    string RefreshToken);
