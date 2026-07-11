using System;
using FinanceApp.Desktop.ViewModels;
using FinanceApp.Desktop.Services;
using FinanceApp.Contracts.Transactions;
using FinanceApp.Contracts.Recurring;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace FinanceApp.Desktop.Pages;

public sealed partial class ExpensesPage : Page
{
    public ExpensesViewModel ViewModel { get; }

    public ExpensesPage()
    {
        ViewModel = App.Host.Services.GetRequiredService<ExpensesViewModel>();
        InitializeComponent();
        Loaded += async (_, _) => await ViewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void AddExpense_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            var apiClient = App.Host.Services.GetRequiredService<ApiClient>();
            var accounts = await apiClient.GetAccountsAsync();
            var categories = await apiClient.GetExpenseCategoriesAsync();
            
            var dialog = new AddTransactionDialog(accounts, categories, "expense")
            {
                XamlRoot = this.XamlRoot,
                Title = "Nova Despesa"
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
                    TransactionType = "expense",
                    Description = dialog.Description,
                    AmountExpected = dialog.Amount,
                    AmountActual = dialog.Amount,
                    CompetenceDate = dialog.CompetenceDate,
                    AccountId = dialog.AccountId,
                    ExpenseCategoryId = dialog.CategoryId,
                    Status = "confirmed",
                    IsFixed = dialog.IsFixed
                };
                
                await ViewModel.CreateTransactionCommand.ExecuteAsync(request);
                
                var infoBarService = App.Host.Services.GetRequiredService<InfoBarService>();
                infoBarService.Success("Despesa criada com sucesso!");

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
                        TransactionKind = "expense",
                        AccountId = dialog.AccountId.Value,
                        ExpenseCategoryId = dialog.CategoryId,
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

    private async void DeleteExpense_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Guid id)
        {
            var dialog = new ContentDialog
            {
                Title = "Apagar Despesa",
                Content = "Tem certeza que deseja apagar esta despesa? Esta ação não pode ser desfeita.",
                PrimaryButtonText = "Apagar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot,
                RequestedTheme = Microsoft.UI.Xaml.ElementTheme.Dark
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await ViewModel.DeleteTransactionCommand.ExecuteAsync(id);
                var infoBarService = App.Host.Services.GetRequiredService<InfoBarService>();
                infoBarService.Success("Despesa apagada com sucesso!");
            }
        }
    }
}
