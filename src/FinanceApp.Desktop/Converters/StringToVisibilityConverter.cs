using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace FinanceApp.Desktop.Converters;

public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string strVal && !string.IsNullOrWhiteSpace(strVal))
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
