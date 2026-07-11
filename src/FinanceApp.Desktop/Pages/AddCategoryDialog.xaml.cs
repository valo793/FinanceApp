using Microsoft.UI.Xaml.Controls;

namespace FinanceApp.Desktop.Pages;

public sealed partial class AddCategoryDialog : ContentDialog
{
    public AddCategoryDialog()
    {
        InitializeComponent();
        ColorPickerInput.Color = Microsoft.UI.ColorHelper.FromArgb(255, 124, 92, 255);
    }

    public string CategoryName => NameInput.Text;
    public string? CategoryColor => $"#{ColorPickerInput.Color.R:X2}{ColorPickerInput.Color.G:X2}{ColorPickerInput.Color.B:X2}";
    public string? CategoryIcon => string.IsNullOrWhiteSpace(IconInput.Text) ? null : IconInput.Text;
    public bool IsExpense
    {
        get => BudgetPanel.Visibility == Microsoft.UI.Xaml.Visibility.Visible;
        set => BudgetPanel.Visibility = value ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
    }
    public decimal? CategoryBudgetLimit => double.IsNaN(BudgetLimitInput.Value) ? null : (decimal)BudgetLimitInput.Value;
}
