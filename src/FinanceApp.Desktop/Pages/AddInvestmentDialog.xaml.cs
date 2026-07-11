using System;
using Microsoft.UI.Xaml.Controls;
using FinanceApp.Contracts.Investments;

namespace FinanceApp.Desktop.Pages;

public sealed partial class AddInvestmentDialog : ContentDialog
{
    public string InvestmentName => NameInput.Text;
    public string? Ticker => string.IsNullOrWhiteSpace(TickerInput.Text) ? null : TickerInput.Text.Trim().ToUpper();
    public string AssetType => (AssetTypeInput.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "stock";
    public decimal Quantity => (decimal)QuantityInput.Value;
    public decimal AveragePrice => (decimal)AveragePriceInput.Value;
    public decimal CurrentPrice => (decimal)CurrentPriceInput.Value;
    public string RiskLevel => (RiskLevelInput.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "moderate";

    public AddInvestmentDialog()
    {
        InitializeComponent();
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
}
