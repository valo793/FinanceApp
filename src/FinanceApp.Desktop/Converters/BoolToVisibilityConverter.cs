using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace FinanceApp.Desktop.Converters;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public bool Inverse { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b)
        {
            var isVisible = Inverse ? !b : b;
            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
