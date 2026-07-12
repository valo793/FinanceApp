using System;
using System.Threading.Tasks;
using FinanceApp.Desktop.ViewModels;
using FinanceApp.Desktop.Services;
using FinanceApp.Contracts.Transactions;
using FinanceApp.Contracts.Categories;
using FinanceApp.Contracts.Recurring;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FinanceApp.Desktop.Pages;

public sealed partial class TransactionsPage : Page
{
    public TransactionsViewModel ViewModel { get; }

    public TransactionsPage()
    {
        ViewModel = App.Host.Services.GetRequiredService<TransactionsViewModel>();
        InitializeComponent();
        Loaded += async (_, _) => await ViewModel.LoadCommand.ExecuteAsync(null);
    }

    private void TypeFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox)
        {
            ViewModel.SelectedTypeFilter = comboBox.SelectedIndex switch
            {
                1 => "income",
                2 => "expense",
                _ => "all"
            };
        }
    }

    private async void AddIncome_Click(object sender, RoutedEventArgs e)
    {
        await OpenAddTransactionDialogAsync("income", "Nova Receita");
    }

    private async void AddExpense_Click(object sender, RoutedEventArgs e)
    {
        await OpenAddTransactionDialogAsync("expense", "Nova Despesa");
    }

    private async Task OpenAddTransactionDialogAsync(string type, string title)
    {
        try
        {
            var apiClient = App.Host.Services.GetRequiredService<ApiClient>();
            var accounts = await apiClient.GetAccountsAsync();
            var categories = type == "income" 
                ? await apiClient.GetIncomeCategoriesAsync()
                : await apiClient.GetExpenseCategoriesAsync();
            var investments = await apiClient.GetInvestmentsAsync();
            
            var dialog = new AddTransactionDialog(accounts, categories, investments, type)
            {
                XamlRoot = this.XamlRoot,
                Title = title
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                if (string.IsNullOrWhiteSpace(dialog.Description) || dialog.Amount <= 0)
                {
                    return;
                }

                var request = new UpsertTransactionRequest
                {
                    TransactionType = type,
                    Description = dialog.Description,
                    AmountExpected = dialog.Amount,
                    AmountActual = dialog.Amount,
                    CompetenceDate = dialog.CompetenceDate,
                    AccountId = dialog.AccountId,
                    ExpenseCategoryId = type == "expense" ? dialog.CategoryId : null,
                    IncomeCategoryId = type == "income" ? dialog.CategoryId : null,
                    Status = "confirmed",
                    IsFixed = dialog.IsFixed
                };
                
                await ViewModel.CreateTransactionCommand.ExecuteAsync(request);
                
                var infoBarService = App.Host.Services.GetRequiredService<InfoBarService>();
                infoBarService.Success($"{title} criada com sucesso!");

                if (dialog.IsFixed && dialog.AccountId.HasValue)
                {
                    var nextDate = dialog.Frequency switch
                    {
                        "daily" => dialog.CompetenceDate.AddDays(dialog.IntervalValue),
                        "weekly" => dialog.CompetenceDate.AddDays(7 * dialog.IntervalValue),
                        "monthly" => dialog.CompetenceDate.AddMonths(dialog.IntervalValue),
                        "yearly" => dialog.CompetenceDate.AddYears(dialog.IntervalValue),
                        _ => dialog.CompetenceDate.AddMonths(dialog.IntervalValue)
                    };

                    await apiClient.CreateRecurringTransactionAsync(new CreateRecurringRequest
                    {
                        TransactionKind = type,
                        AccountId = dialog.AccountId.Value,
                        ExpenseCategoryId = type == "expense" ? dialog.CategoryId : null,
                        IncomeCategoryId = type == "income" ? dialog.CategoryId : null,
                        Description = dialog.Description,
                        Frequency = dialog.Frequency,
                        IntervalValue = dialog.IntervalValue,
                        StartDate = nextDate,
                        DefaultAmount = dialog.Amount,
                        AutoConfirm = dialog.AutoConfirm,
                        CurrencyCode = "BRL"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            ViewModel.ErrorMessage = ex.Message;
        }
    }

    private async void DeleteTransaction_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Guid id)
        {
            var dialog = new ContentDialog
            {
                Title = "Apagar Lançamento",
                Content = "Tem certeza que deseja apagar este lançamento? Esta ação não pode ser desfeita.",
                PrimaryButtonText = "Apagar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot,
                RequestedTheme = ElementTheme.Dark
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await ViewModel.DeleteTransactionCommand.ExecuteAsync(id);
                var infoBarService = App.Host.Services.GetRequiredService<InfoBarService>();
                infoBarService.Success("Lançamento apagado com sucesso!");
            }
        }
    }

    private async void NewExpenseCategory_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddCategoryDialog { IsExpense = true, XamlRoot = this.XamlRoot };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            if (!string.IsNullOrWhiteSpace(dialog.CategoryName))
            {
                await ViewModel.CreateExpenseCategoryCommand.ExecuteAsync(new CreateCategoryRequest
                {
                    Name = dialog.CategoryName,
                    Color = dialog.CategoryColor,
                    Icon = dialog.CategoryIcon
                });
                App.Host.Services.GetRequiredService<InfoBarService>().Success("Categoria de despesa criada!");
            }
        }
    }

    private async void NewIncomeCategory_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddCategoryDialog { IsExpense = false, XamlRoot = this.XamlRoot };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            if (!string.IsNullOrWhiteSpace(dialog.CategoryName))
            {
                await ViewModel.CreateIncomeCategoryCommand.ExecuteAsync(new CreateCategoryRequest
                {
                    Name = dialog.CategoryName,
                    Color = dialog.CategoryColor,
                    Icon = dialog.CategoryIcon
                });
                App.Host.Services.GetRequiredService<InfoBarService>().Success("Categoria de receita criada!");
            }
        }
    }

    private async void DeleteExpenseCategory_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Guid id)
        {
            var dialog = new ContentDialog
            {
                Title = "Apagar Categoria",
                Content = "Tem certeza que deseja apagar esta categoria? Esta ação não pode ser desfeita.",
                PrimaryButtonText = "Apagar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot,
                RequestedTheme = ElementTheme.Dark
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await ViewModel.DeleteExpenseCategoryCommand.ExecuteAsync(id);
                App.Host.Services.GetRequiredService<InfoBarService>().Success("Categoria de despesa apagada!");
            }
        }
    }

    private async void DeleteIncomeCategory_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Guid id)
        {
            var dialog = new ContentDialog
            {
                Title = "Apagar Categoria",
                Content = "Tem certeza que deseja apagar esta categoria? Esta ação não pode ser desfeita.",
                PrimaryButtonText = "Apagar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot,
                RequestedTheme = ElementTheme.Dark
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await ViewModel.DeleteIncomeCategoryCommand.ExecuteAsync(id);
                App.Host.Services.GetRequiredService<InfoBarService>().Success("Categoria de receita apagada!");
            }
        }
    }
}
