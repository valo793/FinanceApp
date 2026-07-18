using System;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml;

namespace FinanceApp.Desktop.Converters;

public sealed class StringToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string s && parameter is string param)
        {
            return s == param;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b && b && parameter is string param)
        {
            return param;
        }
        return DependencyProperty.UnsetValue;
    }
}
