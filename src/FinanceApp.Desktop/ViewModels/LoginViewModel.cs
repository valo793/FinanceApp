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

    [RelayCommand]
    private async Task LoginAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = null;

            await apiClient.LoginAsync(new LoginRequest
            {
                Email = Email,
                Password = Password,
                DeviceName = Environment.MachineName,
                TrustDevice = true
            });

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
