using System;
using Microsoft.UI.Xaml.Data;

namespace FinanceApp.Desktop.Converters;

public sealed class AssetTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string type)
        {
            return type switch
            {
                "stock" => "Ações",
                "fii" => "FIIs",
                "cdb" => "CDB",
                "tesouro" => "Tesouro Direto",
                "crypto" => "Cripto",
                "fund" => "Outros Fundos",
                _ => type
            };
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
