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
}
