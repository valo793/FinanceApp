using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanceApp.Contracts.Accounts;
using FinanceApp.Contracts.Auth;
using FinanceApp.Contracts.Categories;
using FinanceApp.Contracts.Common;
using FinanceApp.Contracts.Dashboards;
using FinanceApp.Contracts.Transactions;
using FinanceApp.Contracts.Wallets;
using FinanceApp.Contracts.Recurring;

namespace FinanceApp.Desktop.Services;

public sealed class ApiClient
{
    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri("http://localhost:5000/")
    };

    private readonly SessionStorage _sessionStorage = new();
    private string? _refreshToken;

    public string? AccessToken { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken);

    // ── Session Restore ──────────────────────────────────────────
    /// <summary>
    /// Attempts to restore a previously saved session (auto-login).
    /// Returns true if session was restored and refresh succeeded.
    /// </summary>
    public async Task<bool> TryRestoreSessionAsync(CancellationToken cancellationToken = default)
    {
        var session = await _sessionStorage.LoadAsync();
        if (session is null)
            return false;

        try
        {
            _refreshToken = session.RefreshToken;
            var response = await _httpClient.PostAsJsonAsync("api/v1/auth/refresh",
                new { RefreshToken = session.RefreshToken }, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _sessionStorage.Clear();
                return false;
            }

            var content = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken);
            if (content is null)
            {
                _sessionStorage.Clear();
                return false;
            }

            ApplyTokens(content);
            await _sessionStorage.SaveAsync(content.AccessToken, content.RefreshToken, content.ExpiresIn);
            return true;
        }
        catch
        {
            _sessionStorage.Clear();
            return false;
        }
    }

    // ── Auth ─────────────────────────────────────────────────────
    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/v1/auth/login", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken);

        if (content is not null)
        {
            ApplyTokens(content);
            await _sessionStorage.SaveAsync(content.AccessToken, content.RefreshToken, content.ExpiresIn);
        }

        return content;
    }

    public async Task<LoginResponse?> RegisterAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/v1/auth/register", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken);

        if (content is not null)
        {
            ApplyTokens(content);
            await _sessionStorage.SaveAsync(content.AccessToken, content.RefreshToken, content.ExpiresIn);
        }

        return content;
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _httpClient.PostAsync("api/v1/auth/logout", null, cancellationToken);
        }
        catch { /* best-effort */ }
        finally
        {
            AccessToken = null;
            _refreshToken = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
            _sessionStorage.Clear();
        }
    }

    // ── Dashboard ────────────────────────────────────────────────
    public async Task<DashboardOverviewDto?> GetDashboardOverviewAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendWithRefreshAsync(() =>
            new HttpRequestMessage(HttpMethod.Get, "api/v1/dashboards/overview"), cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DashboardOverviewDto>(cancellationToken: cancellationToken);
    }

    // ── Accounts ─────────────────────────────────────────────────
    public async Task<IReadOnlyCollection<AccountDto>> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendWithRefreshAsync(() =>
            new HttpRequestMessage(HttpMethod.Get, "api/v1/accounts"), cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyCollection<AccountDto>>(cancellationToken: cancellationToken) ?? [];
    }

    public async Task<AccountDto?> CreateAccountAsync(CreateAccountRequest request, CancellationToken cancellationToken = default)
    {
        var response = await SendWithRefreshAsync(() =>
        {
            var msg = new HttpRequestMessage(HttpMethod.Post, "api/v1/accounts");
            msg.Content = JsonContent.Create(request);
            return msg;
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AccountDto>(cancellationToken: cancellationToken);
    }

    public async Task<AccountDto?> UpdateAccountAsync(Guid id, UpdateAccountRequest request, CancellationToken cancellationToken = default)
    {
        var response = await SendWithRefreshAsync(() =>
        {
            var msg = new HttpRequestMessage(HttpMethod.Put, $"api/v1/accounts/{id}");
            msg.Content = JsonContent.Create(request);
            return msg;
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AccountDto>(cancellationToken: cancellationToken);
    }

    // ── Transactions ─────────────────────────────────────────────
    public async Task<IReadOnlyCollection<TransactionDto>> GetTransactionsAsync(string? typeFilter = null, CancellationToken cancellationToken = default)
    {
        var response = await SendWithRefreshAsync(() =>
            new HttpRequestMessage(HttpMethod.Get, "api/v1/transactions?page=1&pageSize=100"), cancellationToken);
        response.EnsureSuccessStatusCode();
        var paged = await response.Content.ReadFromJsonAsync<PagedResult<TransactionDto>>(cancellationToken: cancellationToken);
        var items = paged?.Items ?? [];
        if (!string.IsNullOrEmpty(typeFilter))
            return items.Where(x => x.TransactionType == typeFilter).ToArray();
        return items;
    }

    public async Task<TransactionDto?> CreateTransactionAsync(UpsertTransactionRequest request, CancellationToken cancellationToken = default)
    {
        var response = await SendWithRefreshAsync(() =>
        {
            var msg = new HttpRequestMessage(HttpMethod.Post, "api/v1/transactions");
            msg.Content = JsonContent.Create(request);
            return msg;
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TransactionDto>(cancellationToken: cancellationToken);
    }

    public async Task DeleteTransactionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await SendWithRefreshAsync(() =>
            new HttpRequestMessage(HttpMethod.Delete, $"api/v1/transactions/{id}"), cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    // ── Categories ───────────────────────────────────────────────
    public async Task<IReadOnlyCollection<CategoryDto>> GetExpenseCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendWithRefreshAsync(() =>
            new HttpRequestMessage(HttpMethod.Get, "api/v1/categories/expense"), cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyCollection<CategoryDto>>(cancellationToken: cancellationToken) ?? [];
    }

    public async Task<IReadOnlyCollection<CategoryDto>> GetIncomeCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendWithRefreshAsync(() =>
            new HttpRequestMessage(HttpMethod.Get, "api/v1/categories/income"), cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyCollection<CategoryDto>>(cancellationToken: cancellationToken) ?? [];
    }

    public async Task<CategoryDto?> CreateExpenseCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var response = await SendWithRefreshAsync(() =>
        {
            var msg = new HttpRequestMessage(HttpMethod.Post, "api/v1/categories/expense");
            msg.Content = JsonContent.Create(request);
            return msg;
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CategoryDto>(cancellationToken: cancellationToken);
    }

    public async Task<CategoryDto?> CreateIncomeCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var response = await SendWithRefreshAsync(() =>
        {
            var msg = new HttpRequestMessage(HttpMethod.Post, "api/v1/categories/income");
            msg.Content = JsonContent.Create(request);
            return msg;
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CategoryDto>(cancellationToken: cancellationToken);
    }

    public async Task DeleteExpenseCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await SendWithRefreshAsync(() =>
            new HttpRequestMessage(HttpMethod.Delete, $"api/v1/categories/expense/{id}"), cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteIncomeCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await SendWithRefreshAsync(() =>
            new HttpRequestMessage(HttpMethod.Delete, $"api/v1/categories/income/{id}"), cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    // ── Wallets ──────────────────────────────────────────────────
    public async Task<IReadOnlyCollection<WalletDto>> GetWalletsAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendWithRefreshAsync(() =>
            new HttpRequestMessage(HttpMethod.Get, "api/v1/wallets"), cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyCollection<WalletDto>>(cancellationToken: cancellationToken) ?? [];
    }

    // ── Recurring Transactions ─────────────────────────────────────
    public async Task<IReadOnlyCollection<RecurringDto>> GetRecurringTransactionsAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendWithRefreshAsync(() =>
            new HttpRequestMessage(HttpMethod.Get, "api/v1/recurring"), cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyCollection<RecurringDto>>(cancellationToken: cancellationToken) ?? [];
    }

    public async Task<RecurringDto?> CreateRecurringTransactionAsync(CreateRecurringRequest request, CancellationToken cancellationToken = default)
    {
        var response = await SendWithRefreshAsync(() =>
        {
            var msg = new HttpRequestMessage(HttpMethod.Post, "api/v1/recurring");
            msg.Content = JsonContent.Create(request);
            return msg;
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RecurringDto>(cancellationToken: cancellationToken);
    }

    public async Task PauseRecurringTransactionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await SendWithRefreshAsync(() =>
            new HttpRequestMessage(HttpMethod.Post, $"api/v1/recurring/{id}/pause"), cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task ResumeRecurringTransactionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await SendWithRefreshAsync(() =>
            new HttpRequestMessage(HttpMethod.Post, $"api/v1/recurring/{id}/resume"), cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    // ── Private helpers ──────────────────────────────────────────

    private void ApplyTokens(LoginResponse response)
    {
        AccessToken = response.AccessToken;
        _refreshToken = response.RefreshToken;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
    }

    /// <summary>
    /// Sends a request. If the server responds with 401 and we have a refresh token,
    /// automatically refreshes and retries the request once.
    /// </summary>
    private async Task<HttpResponseMessage> SendWithRefreshAsync(Func<HttpRequestMessage> requestFactory, CancellationToken cancellationToken)
    {
        var response = await _httpClient.SendAsync(requestFactory(), cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized || string.IsNullOrWhiteSpace(_refreshToken))
            return response;

        // Attempt to refresh
        var refreshResponse = await _httpClient.PostAsJsonAsync("api/v1/auth/refresh",
            new { RefreshToken = _refreshToken }, cancellationToken);

        if (!refreshResponse.IsSuccessStatusCode)
        {
            _sessionStorage.Clear();
            AccessToken = null;
            _refreshToken = null;
            return response; // return the original 401
        }

        var tokens = await refreshResponse.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken);
        if (tokens is null)
            return response;

        ApplyTokens(tokens);
        await _sessionStorage.SaveAsync(tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresIn);

        // Retry original request with new token
        return await _httpClient.SendAsync(requestFactory(), cancellationToken);
    }
}
