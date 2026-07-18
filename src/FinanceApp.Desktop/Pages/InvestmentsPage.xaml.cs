using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinanceApp.Desktop.ViewModels;
using FinanceApp.Desktop.Services;
using FinanceApp.Contracts.Investments;
using FinanceApp.Contracts.Transactions;
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
        Loaded += async (_, _) => 
        {
            await ViewModel.LoadCommand.ExecuteAsync(null);
            ViewModel.LoadRebalanceData();
        };
    }

    private void AssetsContainer_SizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
    {
        if (sender is Border border && border.ActualWidth > 100)
        {
            double availableWidth = border.ActualWidth - 40; // account for 20px padding on each side
            int targetColumns = (int)Math.Max(1, Math.Floor(availableWidth / 380.0));
            double itemWidth = Math.Floor((availableWidth - (targetColumns * 8)) / targetColumns);

            if (AssetsGridView.ItemsPanelRoot is ItemsWrapGrid wrapGrid)
            {
                wrapGrid.ItemWidth = itemWidth;
                wrapGrid.ItemHeight = 280;
            }
        }
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
                CurrencyCode = "BRL",
                IndexerType = dialog.IndexerType,
                IndexerRate = dialog.IndexerRate,
                IndexerAdditionalRate = dialog.IndexerAdditionalRate,
                IsWatchlist = dialog.IsWatchlist
            };

            await ViewModel.CreateInvestmentCommand.ExecuteAsync(request);
        }
    }

    private void PortfolioPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PortfolioPivot == null || ViewModel == null) return;
        ViewModel.IsWatchlistMode = PortfolioPivot.SelectedIndex == 1;
        ViewModel.IsRebalanceMode = PortfolioPivot.SelectedIndex == 2;
    }

    private void BentoCard_EditClicked(object sender, InvestmentDto existing)
    {
        EditInvestment(existing);
    }

    private void BentoCard_DeleteClicked(object sender, Guid id)
    {
        DeleteInvestment(id);
    }

    private void EditInvestment_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is InvestmentDto existing)
        {
            EditInvestment(existing);
        }
    }

    private void DeleteInvestment_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Guid id)
        {
            DeleteInvestment(id);
        }
    }

    internal async void EditInvestment(InvestmentDto existing)
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
                IndexerType = dialog.IndexerType,
                IndexerRate = dialog.IndexerRate,
                IndexerAdditionalRate = dialog.IndexerAdditionalRate,
                IsWatchlist = dialog.IsWatchlist,
                LockVersion = existing.LockVersion
            };

            await ViewModel.UpdateInvestmentCommand.ExecuteAsync((existing.Id, request));
        }
    }

    internal async void DeleteInvestment(Guid id)
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

    internal async void RecordInvestmentTransaction(InvestmentDto investment, string type)
    {
        try
        {
            var apiClient = App.Host.Services.GetRequiredService<ApiClient>();
            var accounts = await apiClient.GetAccountsAsync();
            var investments = new List<InvestmentDto> { investment };
            
            var dialog = new AddTransactionDialog(accounts, [], investments, type, investment.Id)
            {
                XamlRoot = this.XamlRoot,
                Title = type switch
                {
                    "investment_buy" => $"Comprar {investment.Ticker ?? investment.Name}",
                    "investment_sell" => $"Vender {investment.Ticker ?? investment.Name}",
                    "investment_yield" => $"Rendimento de {investment.Ticker ?? investment.Name}",
                    _ => "Nova Transação de Investimento"
                }
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
                    Status = "confirmed",
                    InvestmentId = dialog.InvestmentId,
                    InvestmentQuantity = dialog.InvestmentQuantity,
                    UnitPrice = dialog.UnitPrice
                };

                await apiClient.CreateTransactionAsync(request);
                
                // Refresh investments lists and KPI cards
                await ViewModel.LoadCommand.ExecuteAsync(null);

                var infoBarService = App.Host.Services.GetRequiredService<InfoBarService>();
                infoBarService.Success("Transação de investimento registrada com sucesso!");
            }
        }
        catch (Exception ex)
        {
            var infoBarService = App.Host.Services.GetRequiredService<InfoBarService>();
            infoBarService.Error($"Erro ao registrar transação: {ex.Message}");
        }
    }

    private void ClearSelection_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.SelectedInvestment = null;
    }
}
