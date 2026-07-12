using System;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace FinanceApp.Desktop.Converters;

public sealed class TransactionTypeToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        byte alpha = 255;
        if (parameter is string p && string.Equals(p, "background", StringComparison.OrdinalIgnoreCase))
        {
            alpha = 38; // 15% opacity
        }

        if (value is string type)
        {
            if (string.Equals(type, "income", StringComparison.OrdinalIgnoreCase))
            {
                return new SolidColorBrush(Color.FromArgb(alpha, 52, 199, 89)); // Green
            }
            if (string.Equals(type, "expense", StringComparison.OrdinalIgnoreCase))
            {
                return new SolidColorBrush(Color.FromArgb(alpha, 255, 69, 58)); // Red
            }
            if (type.StartsWith("investment", StringComparison.OrdinalIgnoreCase))
            {
                return new SolidColorBrush(Color.FromArgb(alpha, 142, 89, 255)); // Violet/Purple
            }
        }
        return new SolidColorBrush(Color.FromArgb(alpha, 128, 128, 128));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public sealed class TransactionTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string type)
        {
            if (string.Equals(type, "income", StringComparison.OrdinalIgnoreCase))
            {
                return "\uE11C"; // Arrow Up
            }
            if (string.Equals(type, "expense", StringComparison.OrdinalIgnoreCase))
            {
                return "\uE11D"; // Arrow Down
            }
            if (string.Equals(type, "investment_buy", StringComparison.OrdinalIgnoreCase))
            {
                return "\uE8A1"; // ShoppingCart (buying asset)
            }
            if (string.Equals(type, "investment_sell", StringComparison.OrdinalIgnoreCase))
            {
                return "\uE89C"; // SaveLocal (selling asset)
            }
            if (string.Equals(type, "investment_yield", StringComparison.OrdinalIgnoreCase))
            {
                return "\uEAFC"; // Financial (yield/dividend)
            }
        }
        return "\uE10F"; // Document/Generic
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public sealed class TransactionTypeToPrefixConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string type)
        {
            if (string.Equals(type, "income", StringComparison.OrdinalIgnoreCase))
            {
                return "+ R$ ";
            }
            if (string.Equals(type, "expense", StringComparison.OrdinalIgnoreCase))
            {
                return "- R$ ";
            }
            if (string.Equals(type, "investment_buy", StringComparison.OrdinalIgnoreCase))
            {
                return "- R$ ";
            }
            if (string.Equals(type, "investment_sell", StringComparison.OrdinalIgnoreCase))
            {
                return "+ R$ ";
            }
            if (string.Equals(type, "investment_yield", StringComparison.OrdinalIgnoreCase))
            {
                return "+ R$ ";
            }
        }
        return "R$ ";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
