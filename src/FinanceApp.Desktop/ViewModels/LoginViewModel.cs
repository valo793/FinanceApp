using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceApp.Contracts.Auth;
using FinanceApp.Desktop.Services;

namespace FinanceApp.Desktop.ViewModels;

public partial class LoginViewModel(ApiClient apiClient) : ObservableObject
{
    [ObservableProperty] private string email = "demo@financeapp.local";
    [ObservableProperty] private string password = "StrongPassword123!";
    [ObservableProperty] private string? statusMessage;
    [ObservableProperty] private bool isBusy;

    public event Action? LoginSucceeded;
    public event Action<string>? MfaChallengeRequested;

    [RelayCommand]
    private async Task LoginAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = null;

            var response = await apiClient.LoginAsync(new LoginRequest
            {
                Email = Email,
                Password = Password,
                DeviceName = Environment.MachineName,
                TrustDevice = true
            });

            if (response is not null && response.RequiresMfa)
            {
                MfaChallengeRequested?.Invoke(response.AccessToken);
                return;
            }

            StatusMessage = "Login realizado com sucesso.";
            LoginSucceeded?.Invoke();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task<bool> VerifyMfaCodeAsync(MfaVerifyRequest request)
    {
        try
        {
            IsBusy = true;
            StatusMessage = null;

            await apiClient.LoginMfaAsync(request);

            StatusMessage = "Login realizado com sucesso.";
            LoginSucceeded?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Código MFA inválido ou expirado: {ex.Message}";
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = null;

            await apiClient.RegisterAsync(new LoginRequest
            {
                Email = Email,
                Password = Password,
                DeviceName = Environment.MachineName,
                TrustDevice = true
            });

            StatusMessage = "Cadastro realizado com sucesso.";
            LoginSucceeded?.Invoke();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
