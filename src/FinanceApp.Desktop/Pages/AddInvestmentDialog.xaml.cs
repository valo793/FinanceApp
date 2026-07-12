using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using FinanceApp.Contracts.Investments;
using FinanceApp.Desktop.Services;

namespace FinanceApp.Desktop.Pages;

public sealed partial class AddInvestmentDialog : ContentDialog
{
    private readonly ApiClient _apiClient;
    private bool _isValidating = false;

    public string InvestmentName => NameInput.Text;
    public string? Ticker => string.IsNullOrWhiteSpace(TickerInput.Text) ? null : TickerInput.Text.Trim().ToUpper();
    public string AssetType => (AssetTypeInput.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "stock";
    public decimal Quantity => (decimal)QuantityInput.Value;
    public decimal AveragePrice => (decimal)AveragePriceInput.Value;
    public decimal CurrentPrice => (decimal)CurrentPriceInput.Value;
    public string RiskLevel => (RiskLevelInput.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "moderate";

    public string? IndexerType => IndexerPanel.Visibility == Visibility.Visible ? (IndexerTypeInput.SelectedItem as ComboBoxItem)?.Tag?.ToString() : null;
    public decimal? IndexerRate => IndexerPanel.Visibility == Visibility.Visible ? (decimal?)IndexerRateInput.Value : null;
    public decimal? IndexerAdditionalRate => (IndexerPanel.Visibility == Visibility.Visible && IndexerAdditionalRateInput.Visibility == Visibility.Visible) ? (decimal?)IndexerAdditionalRateInput.Value : null;

    public AddInvestmentDialog()
    {
        InitializeComponent();
        _apiClient = App.Host.Services.GetRequiredService<ApiClient>();
    }

    public AddInvestmentDialog(InvestmentDto existing) : this()
    {
        Title = "Editar Investimento";
        NameInput.Text = existing.Name;
        TickerInput.Text = existing.Ticker;
        QuantityInput.Value = (double)existing.Quantity;
        AveragePriceInput.Value = (double)existing.AveragePrice;
        CurrentPriceInput.Value = (double)existing.CurrentPrice;

        SetComboBoxSelectedTag(AssetTypeInput, existing.AssetType);
        SetComboBoxSelectedTag(RiskLevelInput, existing.RiskLevel);

        if (existing.AssetType == "cdb" || existing.AssetType == "tesouro")
        {
            IndexerPanel.Visibility = Visibility.Visible;
            if (existing.IndexerType != null)
            {
                SetComboBoxSelectedTag(IndexerTypeInput, existing.IndexerType);
                IndexerRateInput.Value = (double)(existing.IndexerRate ?? 0m);
                if (existing.IndexerType == "ipca")
                {
                    IndexerAdditionalRateInput.Visibility = Visibility.Visible;
                    IndexerAdditionalRateInput.Value = (double)(existing.IndexerAdditionalRate ?? 0m);
                }
            }
        }
    }

    private void SetComboBoxSelectedTag(ComboBox comboBox, string tagValue)
    {
        for (int i = 0; i < comboBox.Items.Count; i++)
        {
            if (comboBox.Items[i] is ComboBoxItem item && item.Tag?.ToString() == tagValue)
            {
                comboBox.SelectedIndex = i;
                break;
            }
        }
    }

    private void AssetTypeInput_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IndexerPanel == null) return;

        var selectedTag = (AssetTypeInput.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        if (selectedTag == "cdb" || selectedTag == "tesouro")
        {
            IndexerPanel.Visibility = Visibility.Visible;
        }
        else
        {
            IndexerPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void IndexerTypeInput_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IndexerAdditionalRateInput == null) return;

        var selectedTag = (IndexerTypeInput.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        if (selectedTag == "ipca")
        {
            IndexerAdditionalRateInput.Visibility = Visibility.Visible;
        }
        else
        {
            IndexerAdditionalRateInput.Visibility = Visibility.Collapsed;
        }
    }

    private async void TickerInput_LostFocus(object sender, RoutedEventArgs e)
    {
        await RunTickerValidationAsync();
    }

    private async void ValidateTickerButton_Click(object sender, RoutedEventArgs e)
    {
        await RunTickerValidationAsync();
    }

    private async Task RunTickerValidationAsync()
    {
        var ticker = TickerInput.Text?.Trim();
        if (string.IsNullOrWhiteSpace(ticker) || _isValidating) return;

        _isValidating = true;
        ValidationProgress.Visibility = Visibility.Visible;
        ValidationProgress.IsActive = true;
        ValidationStatusText.Visibility = Visibility.Collapsed;
        ValidateTickerButton.IsEnabled = false;

        try
        {
            var result = await _apiClient.ValidateTickerAsync(ticker);
            if (result != null && result.IsValid)
            {
                NameInput.Text = result.Name;
                CurrentPriceInput.Value = (double)result.CurrentPrice;
                SetComboBoxSelectedTag(AssetTypeInput, result.AssetType);

                ValidationStatusText.Text = "Ticker validado com sucesso!";
                ValidationStatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 200, 83)); // Green
                ValidationStatusText.Visibility = Visibility.Visible;
            }
            else
            {
                ValidationStatusText.Text = "Ticker inválido ou não encontrado.";
                ValidationStatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 244, 67, 54)); // Red
                ValidationStatusText.Visibility = Visibility.Visible;
            }
        }
        catch
        {
            ValidationStatusText.Text = "Erro ao validar o ticker.";
            ValidationStatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 244, 67, 54)); // Red
            ValidationStatusText.Visibility = Visibility.Visible;
        }
        finally
        {
            ValidationProgress.IsActive = false;
            ValidationProgress.Visibility = Visibility.Collapsed;
            ValidateTickerButton.IsEnabled = true;
            _isValidating = false;
        }
    }
}
