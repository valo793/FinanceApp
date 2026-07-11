using System;
using FinanceApp.Desktop.ViewModels;
using FinanceApp.Desktop.Services;
using FinanceApp.Contracts.Investments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace FinanceApp.Desktop.Pages;

public sealed partial class InvestmentsPage : Page
{
    public InvestmentsViewModel ViewModel { get; }

    public InvestmentsPage()
    {
        ViewModel = App.Host.Services.GetRequiredService<InvestmentsViewModel>();
        InitializeComponent();
        Loaded += async (_, _) => await ViewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void AddInvestment_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var dialog = new AddInvestmentDialog
        {
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            if (string.IsNullOrWhiteSpace(dialog.InvestmentName) || dialog.Quantity <= 0 || dialog.AveragePrice < 0)
            {
                return;
            }

            var request = new CreateInvestmentRequest
            {
                Name = dialog.InvestmentName,
                Ticker = dialog.Ticker,
                AssetType = dialog.AssetType,
                Quantity = dialog.Quantity,
                AveragePrice = dialog.AveragePrice,
                CurrentPrice = dialog.CurrentPrice,
                RiskLevel = dialog.RiskLevel,
                CurrencyCode = "BRL"
            };

            await ViewModel.CreateInvestmentCommand.ExecuteAsync(request);
        }
    }

    private async void EditInvestment_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is InvestmentDto existing)
        {
            var dialog = new AddInvestmentDialog(existing)
            {
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                if (string.IsNullOrWhiteSpace(dialog.InvestmentName) || dialog.Quantity <= 0 || dialog.AveragePrice < 0)
                {
                    return;
                }

                var request = new UpdateInvestmentRequest
                {
                    Name = dialog.InvestmentName,
                    Ticker = dialog.Ticker,
                    AssetType = dialog.AssetType,
                    Quantity = dialog.Quantity,
                    AveragePrice = dialog.AveragePrice,
                    CurrentPrice = dialog.CurrentPrice,
                    RiskLevel = dialog.RiskLevel,
                    IsActive = existing.IsActive,
                    LockVersion = existing.LockVersion
                };

                await ViewModel.UpdateInvestmentCommand.ExecuteAsync((existing.Id, request));
            }
        }
    }

    private async void DeleteInvestment_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Guid id)
        {
            var dialog = new ContentDialog
            {
                Title = "Excluir Investimento",
                Content = "Tem certeza que deseja remover este investimento de sua carteira?",
                PrimaryButtonText = "Excluir",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot,
                RequestedTheme = Microsoft.UI.Xaml.ElementTheme.Dark
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await ViewModel.DeleteInvestmentCommand.ExecuteAsync(id);
            }
        }
    }
}
