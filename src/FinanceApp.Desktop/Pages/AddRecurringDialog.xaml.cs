using System;
using System.Collections.Generic;
using FinanceApp.Contracts.Accounts;
using FinanceApp.Contracts.Categories;
using Microsoft.UI.Xaml.Controls;

namespace FinanceApp.Desktop.Pages;

public sealed partial class AddRecurringDialog : ContentDialog
{
    private readonly IReadOnlyCollection<CategoryDto> _expenseCategories;
    private readonly IReadOnlyCollection<CategoryDto> _incomeCategories;

    public AddRecurringDialog(IReadOnlyCollection<AccountDto> accounts, IReadOnlyCollection<CategoryDto> expenseCategories, IReadOnlyCollection<CategoryDto> incomeCategories)
    {
        InitializeComponent();
        _expenseCategories = expenseCategories;
        _incomeCategories = incomeCategories;

        AccountInput.ItemsSource = accounts;
        if (accounts.Count > 0)
        {
            AccountInput.SelectedIndex = 0;
        }
        DateInput.Date = DateTimeOffset.Now;

        KindInput.SelectionChanged += KindInput_SelectionChanged;
        UpdateCategories();
    }

    private void KindInput_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateCategories();
    }

    private void UpdateCategories()
    {
        if (CategoryInput == null) return;

        var isExpense = TransactionKind == "expense";
        CategoryInput.Header = isExpense ? "Categoria de Despesa" : "Fonte de Receita";
        
        var categories = isExpense ? _expenseCategories : _incomeCategories;
        CategoryInput.ItemsSource = categories;
        if (categories.Count > 0)
        {
            CategoryInput.SelectedIndex = 0;
        }
        else
        {
            CategoryInput.SelectedIndex = -1;
        }
    }

    public string Description => DescriptionInput.Text;
    public decimal DefaultAmount => string.IsNullOrWhiteSpace(AmountInput.Text) ? 0m : (decimal)AmountInput.Value;
    public string TransactionKind => (KindInput.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "expense";
    public Guid AccountId => AccountInput.SelectedValue is Guid guid ? guid : Guid.Empty;
    public Guid? CategoryId => CategoryInput.SelectedValue as Guid?;
    public string Frequency => (FrequencyInput.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "monthly";
    public int IntervalValue => string.IsNullOrWhiteSpace(IntervalInput.Text) ? 1 : (int)IntervalInput.Value;
    public DateOnly StartDate => DateOnly.FromDateTime(DateInput.Date.DateTime);
    public bool AutoConfirm => AutoConfirmInput.IsChecked ?? true;
}
