using System;
using System.Collections.Generic;
using FinanceApp.Contracts.Accounts;
using FinanceApp.Contracts.Categories;
using Microsoft.UI.Xaml.Controls;

namespace FinanceApp.Desktop.Pages;

public sealed partial class AddTransactionDialog : ContentDialog
{
    public AddTransactionDialog(
        IReadOnlyCollection<AccountDto> accounts,
        IReadOnlyCollection<CategoryDto> categories,
        IReadOnlyCollection<FinanceApp.Contracts.Investments.InvestmentDto> investments,
        string transactionType,
        Guid? preSelectedInvestmentId = null)
    {
        InitializeComponent();
        AccountInput.ItemsSource = accounts;
        if (accounts.Count > 0)
        {
            AccountInput.SelectedIndex = 0;
        }

        CategoryInput.ItemsSource = categories;
        if (categories.Count > 0)
        {
            CategoryInput.SelectedIndex = 0;
        }

        InvestmentInput.ItemsSource = investments;
        if (investments != null && investments.Count > 0)
        {
            if (preSelectedInvestmentId.HasValue)
            {
                InvestmentInput.SelectedValue = preSelectedInvestmentId.Value;
            }
            else
            {
                InvestmentInput.SelectedIndex = 0;
            }
        }

        var isInvestment = transactionType == "investment_buy" ||
                           transactionType == "investment_sell" ||
                           transactionType == "investment_yield";

        if (isInvestment)
        {
            CategoryInput.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            IsFixedInput.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            InvestmentFields.Visibility = Microsoft.UI.Xaml.Visibility.Visible;

            if (transactionType == "investment_buy" || transactionType == "investment_sell")
            {
                BuySellFields.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                AmountInput.IsEnabled = false;
            }
            else
            {
                BuySellFields.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                AmountInput.IsEnabled = true;
            }
        }
        else
        {
            CategoryInput.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            IsFixedInput.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            InvestmentFields.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            AmountInput.IsEnabled = true;
        }

        CategoryInput.Header = transactionType == "expense" ? "Categoria de Despesa" : "Fonte de Receita";
        DateInput.Date = DateTimeOffset.Now;
    }

    public string Description => DescriptionInput.Text;
    public decimal Amount => string.IsNullOrWhiteSpace(AmountInput.Text) ? 0m : (decimal)AmountInput.Value;
    public DateOnly CompetenceDate => DateOnly.FromDateTime(DateInput.Date.DateTime);
    public Guid? AccountId => AccountInput.SelectedValue as Guid?;
    public Guid? CategoryId => CategoryInput.SelectedValue as Guid?;
    public Guid? InvestmentId => InvestmentInput.SelectedValue as Guid?;
    public decimal? InvestmentQuantity => BuySellFields.Visibility == Microsoft.UI.Xaml.Visibility.Visible ? (decimal?)QuantityInput.Value : null;
    public decimal? UnitPrice => BuySellFields.Visibility == Microsoft.UI.Xaml.Visibility.Visible ? (decimal?)UnitPriceInput.Value : null;
    public bool IsFixed => IsFixedInput.IsChecked ?? false;

    public string Frequency => (FrequencyInput.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "monthly";
    public int IntervalValue => string.IsNullOrWhiteSpace(IntervalInput.Text) ? 1 : (int)IntervalInput.Value;
    public bool AutoConfirm => AutoConfirmInput.IsChecked ?? true;

    private void IsFixedInput_Checked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (RecurrenceFields != null)
        {
            RecurrenceFields.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        }
    }

    private void IsFixedInput_Unchecked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (RecurrenceFields != null)
        {
            RecurrenceFields.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        }
    }

    private void OnBuySellFieldsChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (QuantityInput != null && UnitPriceInput != null && AmountInput != null)
        {
            var qty = double.IsNaN(QuantityInput.Value) ? 0.0 : QuantityInput.Value;
            var price = double.IsNaN(UnitPriceInput.Value) ? 0.0 : UnitPriceInput.Value;
            AmountInput.Value = qty * price;
        }
    }
}
