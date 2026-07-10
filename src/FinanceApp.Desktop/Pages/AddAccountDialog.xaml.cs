using Microsoft.UI.Xaml.Controls;

namespace FinanceApp.Desktop.Pages;

public sealed partial class AddAccountDialog : ContentDialog
{
    public AddAccountDialog()
    {
        InitializeComponent();
    }

    public string AccountName => NameInput.Text;
    public string AccountType => (TypeInput.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "checking";
    public decimal OpeningBalance => string.IsNullOrWhiteSpace(BalanceInput.Text) ? 0m : (decimal)BalanceInput.Value;
    public string CurrencyCode => string.IsNullOrWhiteSpace(CurrencyInput.Text) ? "BRL" : CurrencyInput.Text;
    public bool IncludeInNetWorth => NetWorthInput.IsChecked ?? true;
}
