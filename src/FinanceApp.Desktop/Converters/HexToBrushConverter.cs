using System;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace FinanceApp.Desktop.Converters;

public class HexToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string strVal && !string.IsNullOrWhiteSpace(strVal))
        {
            if (strVal == "income")
            {
                if (App.Current.Resources.TryGetValue("AccentCyanBrush", out var resCyan) && resCyan is Brush brushCyan)
                    return brushCyan;
            }
            else if (strVal == "expense")
            {
                if (App.Current.Resources.TryGetValue("AccentVioletBrush", out var resViolet) && resViolet is Brush brushViolet)
                    return brushViolet;
            }

            try
            {
                var hex = strVal.Replace("#", "").Trim();
                if (hex.Length == 6)
                {
                    byte r = System.Convert.ToByte(hex.Substring(0, 2), 16);
                    byte g = System.Convert.ToByte(hex.Substring(2, 2), 16);
                    byte b = System.Convert.ToByte(hex.Substring(4, 2), 16);
                    return new SolidColorBrush(ColorHelper.FromArgb(255, r, g, b));
                }
            }
            catch
            {
                // Fallback
            }
        }

        if (parameter is string defaultKey && App.Current.Resources.TryGetValue(defaultKey, out var res) && res is Brush defaultBrush)
        {
            return defaultBrush;
        }

        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
