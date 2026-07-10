using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceApp.Contracts.Accounts;
using FinanceApp.Desktop.Services;

namespace FinanceApp.Desktop.ViewModels;

public partial class AccountsViewModel(ApiClient apiClient) : ObservableObject
{
    [ObservableProperty] private ObservableCollection<AccountDto> accounts = [];
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            var result = await apiClient.GetAccountsAsync();
            Accounts = new ObservableCollection<AccountDto>(result);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task CreateAccountAsync(CreateAccountRequest request)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await apiClient.CreateAccountAsync(request);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task UpdateAccountAsync((Guid Id, UpdateAccountRequest Request) tuple)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await apiClient.UpdateAccountAsync(tuple.Id, tuple.Request);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
