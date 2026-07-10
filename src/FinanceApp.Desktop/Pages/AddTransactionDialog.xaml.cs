using System;
using System.Collections.Generic;
using FinanceApp.Contracts.Accounts;
using FinanceApp.Contracts.Categories;
using Microsoft.UI.Xaml.Controls;

namespace FinanceApp.Desktop.Pages;

public sealed partial class AddTransactionDialog : ContentDialog
{
    public AddTransactionDialog(IReadOnlyCollection<AccountDto> accounts, IReadOnlyCollection<CategoryDto> categories, string transactionType)
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
        CategoryInput.Header = transactionType == "expense" ? "Categoria de Despesa" : "Fonte de Receita";

        DateInput.Date = DateTimeOffset.Now;
    }

    public string Description => DescriptionInput.Text;
    public decimal Amount => string.IsNullOrWhiteSpace(AmountInput.Text) ? 0m : (decimal)AmountInput.Value;
    public DateOnly CompetenceDate => DateOnly.FromDateTime(DateInput.Date.DateTime);
    public Guid? AccountId => AccountInput.SelectedValue as Guid?;
    public Guid? CategoryId => CategoryInput.SelectedValue as Guid?;
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
}
