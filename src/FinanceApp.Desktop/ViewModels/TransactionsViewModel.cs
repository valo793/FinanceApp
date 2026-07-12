using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceApp.Contracts.Transactions;
using FinanceApp.Contracts.Categories;
using FinanceApp.Desktop.Services;

namespace FinanceApp.Desktop.ViewModels;

public partial class TransactionsViewModel(ApiClient apiClient) : ObservableObject
{
    [ObservableProperty] private ObservableCollection<TransactionDto> allTransactions = [];
    [ObservableProperty] private ObservableCollection<TransactionDto> filteredTransactions = [];
    [ObservableProperty] private ObservableCollection<CategoryDto> expenseCategories = [];
    [ObservableProperty] private ObservableCollection<CategoryDto> incomeCategories = [];
    [ObservableProperty] private string selectedTypeFilter = "all"; // "all", "income", "expense"
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;

    partial void OnSelectedTypeFilterChanged(string value)
    {
        UpdateFilteredTransactions();
    }

    private void UpdateFilteredTransactions()
    {
        if (string.Equals(SelectedTypeFilter, "income", StringComparison.OrdinalIgnoreCase))
        {
            FilteredTransactions = new ObservableCollection<TransactionDto>(
                AllTransactions.Where(x => string.Equals(x.TransactionType, "income", StringComparison.OrdinalIgnoreCase)));
        }
        else if (string.Equals(SelectedTypeFilter, "expense", StringComparison.OrdinalIgnoreCase))
        {
            FilteredTransactions = new ObservableCollection<TransactionDto>(
                AllTransactions.Where(x => string.Equals(x.TransactionType, "expense", StringComparison.OrdinalIgnoreCase)));
        }
        else
        {
            FilteredTransactions = new ObservableCollection<TransactionDto>(AllTransactions);
        }
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var transactionsTask = apiClient.GetTransactionsAsync(null);
            var expenseCatsTask = apiClient.GetExpenseCategoriesAsync();
            var incomeCatsTask = apiClient.GetIncomeCategoriesAsync();

            await Task.WhenAll(transactionsTask, expenseCatsTask, incomeCatsTask);

            AllTransactions = new ObservableCollection<TransactionDto>(transactionsTask.Result);
            ExpenseCategories = new ObservableCollection<CategoryDto>(expenseCatsTask.Result);
            IncomeCategories = new ObservableCollection<CategoryDto>(incomeCatsTask.Result);

            UpdateFilteredTransactions();
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

    [RelayCommand]
    public async Task CreateExpenseCategoryAsync(CreateCategoryRequest request)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await apiClient.CreateExpenseCategoryAsync(request);
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
    public async Task CreateIncomeCategoryAsync(CreateCategoryRequest request)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await apiClient.CreateIncomeCategoryAsync(request);
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
    public async Task DeleteExpenseCategoryAsync(Guid id)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await apiClient.DeleteExpenseCategoryAsync(id);
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
    public async Task DeleteIncomeCategoryAsync(Guid id)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await apiClient.DeleteIncomeCategoryAsync(id);
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
