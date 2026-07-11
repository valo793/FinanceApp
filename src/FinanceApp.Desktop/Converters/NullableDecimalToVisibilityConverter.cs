using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace FinanceApp.Desktop.Converters;

public class NullableDecimalToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is decimal decVal)
        {
            return decVal > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        
        var nullableDec = value as decimal?;
        if (nullableDec.HasValue && nullableDec.Value > 0)
        {
            return Visibility.Visible;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
