using System;
using FinanceApp.Contracts.Categories;
using FinanceApp.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace FinanceApp.Desktop.Pages;

public sealed partial class CategoriesPage : Page
{
    public CategoriesViewModel ViewModel { get; }

    public CategoriesPage()
    {
        ViewModel = App.Host.Services.GetRequiredService<CategoriesViewModel>();
        InitializeComponent();
        Loaded += async (_, _) => await ViewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void NewExpenseCategory_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var dialog = new AddCategoryDialog
        {
            XamlRoot = this.XamlRoot,
            Title = "Nova Categoria de Despesa"
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            if (string.IsNullOrWhiteSpace(dialog.CategoryName))
            {
                return;
            }

            var request = new CreateCategoryRequest
            {
                Name = dialog.CategoryName,
                Color = dialog.CategoryColor,
                Icon = dialog.CategoryIcon
            };

            await ViewModel.CreateExpenseCategoryCommand.ExecuteAsync(request);
        }
    }

    private async void NewIncomeCategory_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var dialog = new AddCategoryDialog
        {
            XamlRoot = this.XamlRoot,
            Title = "Nova Categoria de Receita"
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            if (string.IsNullOrWhiteSpace(dialog.CategoryName))
            {
                return;
            }

            var request = new CreateCategoryRequest
            {
                Name = dialog.CategoryName,
                Color = dialog.CategoryColor,
                Icon = dialog.CategoryIcon
            };

            await ViewModel.CreateIncomeCategoryCommand.ExecuteAsync(request);
        }
    }

    private async void DeleteExpenseCategory_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Guid id)
        {
            await ViewModel.DeleteExpenseCategoryCommand.ExecuteAsync(id);
        }
    }

    private async void DeleteIncomeCategory_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Guid id)
        {
            await ViewModel.DeleteIncomeCategoryCommand.ExecuteAsync(id);
        }
    }
}
