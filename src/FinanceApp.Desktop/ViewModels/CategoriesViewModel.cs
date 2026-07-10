using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceApp.Contracts.Categories;
using FinanceApp.Desktop.Services;

namespace FinanceApp.Desktop.ViewModels;

public partial class CategoriesViewModel(ApiClient apiClient) : ObservableObject
{
    [ObservableProperty] private ObservableCollection<CategoryDto> expenseCategories = [];
    [ObservableProperty] private ObservableCollection<CategoryDto> incomeCategories = [];
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            var expenses = await apiClient.GetExpenseCategoriesAsync();
            var incomes = await apiClient.GetIncomeCategoriesAsync();

            ExpenseCategories = new ObservableCollection<CategoryDto>(expenses);
            IncomeCategories = new ObservableCollection<CategoryDto>(incomes);
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
