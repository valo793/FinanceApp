using System;
using FinanceApp.Desktop.ViewModels;
using FinanceApp.Desktop.Services;
using FinanceApp.Contracts.Transactions;
using FinanceApp.Contracts.Recurring;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace FinanceApp.Desktop.Pages;

public sealed partial class IncomesPage : Page
{
    public IncomesViewModel ViewModel { get; }

    public IncomesPage()
    {
        ViewModel = App.Host.Services.GetRequiredService<IncomesViewModel>();
        InitializeComponent();
        Loaded += async (_, _) => await ViewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void AddIncome_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            var apiClient = App.Host.Services.GetRequiredService<ApiClient>();
            var accounts = await apiClient.GetAccountsAsync();
            var categories = await apiClient.GetIncomeCategoriesAsync();
            
            var dialog = new AddTransactionDialog(accounts, categories, "income")
            {
                XamlRoot = this.XamlRoot,
                Title = "Nova Receita"
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
                    TransactionType = "income",
                    Description = dialog.Description,
                    AmountExpected = dialog.Amount,
                    AmountActual = dialog.Amount,
                    CompetenceDate = dialog.CompetenceDate,
                    AccountId = dialog.AccountId,
                    IncomeCategoryId = dialog.CategoryId,
                    Status = "confirmed",
                    IsFixed = dialog.IsFixed
                };
                
                await ViewModel.CreateTransactionCommand.ExecuteAsync(request);

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
                        TransactionKind = "income",
                        AccountId = dialog.AccountId.Value,
                        IncomeCategoryId = dialog.CategoryId,
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

    private async void DeleteIncome_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Guid id)
        {
            await ViewModel.DeleteTransactionCommand.ExecuteAsync(id);
        }
    }
}
