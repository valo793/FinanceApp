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
using FinanceApp.Contracts.Investments;
using FinanceApp.Contracts.UserPreferences;
using FinanceApp.Contracts.Projections;
using FinanceApp.Desktop.Exceptions;

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
    public bool MfaEnabled { get; private set; }

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

    // ── Investments ──────────────────────────────────────────────
    public async Task<IReadOnlyCollection<InvestmentDto>> GetInvestmentsAsync(Guid? walletId = null, CancellationToken cancellationToken = default)
    {
        var url = "api/v1/investments";
        if (walletId.HasValue)
            url += $"?walletId={walletId.Value}";

        var response = await SendWithRefreshAsync(() =>
            new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyCollection<InvestmentDto>>(cancellationToken: cancellationToken) ?? [];
    }

    public async Task<PortfolioSummaryDto?> GetInvestmentSummaryAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendWithRefreshAsync(() =>
            new HttpRequestMessage(HttpMethod.Get, "api/v1/investments/summary"), cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PortfolioSummaryDto>(cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyCollection<InvestmentHistoryPointDto>> GetInvestmentHistoryAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendWithRefreshAsync(() =>
            new HttpRequestMessage(HttpMethod.Get, "api/v1/investments/history"), cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyCollection<InvestmentHistoryPointDto>>(cancellationToken: cancellationToken) ?? [];
    }

    public async Task<InvestmentDto?> CreateInvestmentAsync(CreateInvestmentRequest request, CancellationToken cancellationToken = default)
    {
        var response = await SendWithRefreshAsync(() =>
        {
            var msg = new HttpRequestMessage(HttpMethod.Post, "api/v1/investments");
            msg.Content = JsonContent.Create(request);
            return msg;
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InvestmentDto>(cancellationToken: cancellationToken);
    }

    public async Task<InvestmentDto?> UpdateInvestmentAsync(Guid id, UpdateInvestmentRequest request, CancellationToken cancellationToken = default)
    {
        var response = await SendWithRefreshAsync(() =>
        {
            var msg = new HttpRequestMessage(HttpMethod.Put, $"api/v1/investments/{id}");
            msg.Content = JsonContent.Create(request);
            return msg;
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InvestmentDto>(cancellationToken: cancellationToken);
    }

    public async Task DeleteInvestmentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await SendWithRefreshAsync(() =>
            new HttpRequestMessage(HttpMethod.Delete, $"api/v1/investments/{id}"), cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<TickerValidationResultDto?> ValidateTickerAsync(string ticker, CancellationToken cancellationToken = default)
    {
        var response = await SendWithRefreshAsync(() =>
            new HttpRequestMessage(HttpMethod.Get, $"api/v1/investments/validate?ticker={Uri.EscapeDataString(ticker)}"), cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<TickerValidationResultDto>(cancellationToken: cancellationToken);
    }

    public async Task SyncInvestmentsAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendWithRefreshAsync(() =>
            new HttpRequestMessage(HttpMethod.Post, "api/v1/investments/sync"), cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    // ── User Preferences ──────────────────────────────────────────
    public async Task<UserPreferenceDto?> GetPreferencesAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendWithRefreshAsync(() =>
            new HttpRequestMessage(HttpMethod.Get, "api/v1/preferences"), cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserPreferenceDto>(cancellationToken: cancellationToken);
    }

    public async Task<UserPreferenceDto?> UpdatePreferencesAsync(UpdatePreferenceRequest request, CancellationToken cancellationToken = default)
    {
        var response = await SendWithRefreshAsync(() =>
        {
            var msg = new HttpRequestMessage(HttpMethod.Put, "api/v1/preferences");
            msg.Content = JsonContent.Create(request);
            return msg;
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserPreferenceDto>(cancellationToken: cancellationToken);
    }

    // ── MFA Security ──────────────────────────────────────────────
    public async Task<MfaSetupResponse?> SetupMfaAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendWithRefreshAsync(() =>
            new HttpRequestMessage(HttpMethod.Get, "api/v1/auth/mfa/setup"), cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MfaSetupResponse>(cancellationToken: cancellationToken);
    }

    public async Task EnableMfaAsync(MfaEnableRequest request, CancellationToken cancellationToken = default)
    {
        var response = await SendWithRefreshAsync(() =>
        {
            var msg = new HttpRequestMessage(HttpMethod.Post, "api/v1/auth/mfa/enable");
            msg.Content = JsonContent.Create(request);
            return msg;
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        MfaEnabled = true;
    }

    public async Task DisableMfaAsync(MfaEnableRequest request, CancellationToken cancellationToken = default)
    {
        var response = await SendWithRefreshAsync(() =>
        {
            var msg = new HttpRequestMessage(HttpMethod.Post, "api/v1/auth/mfa/disable");
            msg.Content = JsonContent.Create(request);
            return msg;
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        MfaEnabled = false;
    }

    public async Task<LoginResponse?> LoginMfaAsync(MfaVerifyRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/v1/auth/login/mfa", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken);

        if (content is not null)
        {
            ApplyTokens(content);
            await _sessionStorage.SaveAsync(content.AccessToken, content.RefreshToken, content.ExpiresIn);
        }

        return content;
    }

    // ── Projections ────────────────────────────────────────────────
    public async Task<IReadOnlyCollection<ProjectionPointDto>> GetProjectionsAsync(int months, CancellationToken cancellationToken = default)
    {
        var response = await SendWithRefreshAsync(() =>
            new HttpRequestMessage(HttpMethod.Get, $"api/v1/projections?months={months}"), cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyCollection<ProjectionPointDto>>(cancellationToken: cancellationToken) ?? [];
    }

    // ── Private helpers ──────────────────────────────────────────

    private void ApplyTokens(LoginResponse response)
    {
        AccessToken = response.AccessToken;
        _refreshToken = response.RefreshToken;
        MfaEnabled = response.User?.MfaEnabled ?? false;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
    }

    /// <summary>
    /// Sends a request. If the server responds with 401 and we have a refresh token,
    /// automatically refreshes and retries the request once.
    /// Handles 409 Conflict status code by throwing a ConcurrencyConflictException.
    /// </summary>
    private async Task<HttpResponseMessage> SendWithRefreshAsync(Func<HttpRequestMessage> requestFactory, CancellationToken cancellationToken)
    {
        var response = await _httpClient.SendAsync(requestFactory(), cancellationToken);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new ConcurrencyConflictException();
        }

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
        var retryResponse = await _httpClient.SendAsync(requestFactory(), cancellationToken);
        if (retryResponse.StatusCode == HttpStatusCode.Conflict)
        {
            throw new ConcurrencyConflictException();
        }
        return retryResponse;
    }
}
