using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceApp.Contracts.Transactions;
using FinanceApp.Desktop.Services;

namespace FinanceApp.Desktop.ViewModels;

public partial class IncomesViewModel(ApiClient apiClient) : ObservableObject
{
    [ObservableProperty] private ObservableCollection<TransactionDto> incomes = [];
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            var result = await apiClient.GetTransactionsAsync("income");
            Incomes = new ObservableCollection<TransactionDto>(result);
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
    public async Task CreateTransactionAsync(UpsertTransactionRequest request)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await apiClient.CreateTransactionAsync(request);
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
    public async Task DeleteTransactionAsync(Guid id)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await apiClient.DeleteTransactionAsync(id);
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
