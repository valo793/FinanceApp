using System;
using FinanceApp.Contracts.Categories;
using FinanceApp.Desktop.ViewModels;
using FinanceApp.Desktop.Services;
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
            Title = "Nova Categoria de Despesa",
            IsExpense = true
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
                Icon = dialog.CategoryIcon,
                MonthlyBudgetLimit = dialog.CategoryBudgetLimit
            };

            await ViewModel.CreateExpenseCategoryCommand.ExecuteAsync(request);
            var infoBarService = App.Host.Services.GetRequiredService<InfoBarService>();
            infoBarService.Success("Categoria de despesa criada!");
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
            var infoBarService = App.Host.Services.GetRequiredService<InfoBarService>();
            infoBarService.Success("Categoria de receita criada!");
        }
    }

    private async void DeleteExpenseCategory_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Guid id)
        {
            var dialog = new ContentDialog
            {
                Title = "Apagar Categoria",
                Content = "Tem certeza que deseja apagar esta categoria de despesa? Esta ação não pode ser desfeita.",
                PrimaryButtonText = "Apagar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot,
                RequestedTheme = Microsoft.UI.Xaml.ElementTheme.Dark
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await ViewModel.DeleteExpenseCategoryCommand.ExecuteAsync(id);
                var infoBarService = App.Host.Services.GetRequiredService<InfoBarService>();
                infoBarService.Success("Categoria de despesa apagada!");
            }
        }
    }

    private async void DeleteIncomeCategory_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Guid id)
        {
            var dialog = new ContentDialog
            {
                Title = "Apagar Categoria",
                Content = "Tem certeza que deseja apagar esta categoria de receita? Esta ação não pode ser desfeita.",
                PrimaryButtonText = "Apagar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot,
                RequestedTheme = Microsoft.UI.Xaml.ElementTheme.Dark
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await ViewModel.DeleteIncomeCategoryCommand.ExecuteAsync(id);
                var infoBarService = App.Host.Services.GetRequiredService<InfoBarService>();
                infoBarService.Success("Categoria de receita apagada!");
            }
        }
    }
}
