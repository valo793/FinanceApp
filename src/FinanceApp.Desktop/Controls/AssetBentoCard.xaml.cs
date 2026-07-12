using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Extensions.DependencyInjection;
using Windows.UI;
using FinanceApp.Contracts.Investments;
using FinanceApp.Desktop.Services;
using FinanceApp.Desktop.Pages;

namespace FinanceApp.Desktop.Controls;

public sealed partial class AssetBentoCard : UserControl
{
    private readonly ApiClient _apiClient;
    private bool _dataLoaded = false;

    public static readonly DependencyProperty InvestmentProperty =
        DependencyProperty.Register(
            nameof(Investment),
            typeof(InvestmentDto),
            typeof(AssetBentoCard),
            new PropertyMetadata(null, OnInvestmentChanged));

    public InvestmentDto? Investment
    {
        get => (InvestmentDto?)GetValue(InvestmentProperty);
        set => SetValue(InvestmentProperty, value);
    }


    public AssetBentoCard()
    {
        InitializeComponent();
        _apiClient = App.Host.Services.GetRequiredService<ApiClient>();
    }

    private static void OnInvestmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AssetBentoCard card)
        {
            card.UpdateVisuals();
        }
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateVisuals();
        if (!_dataLoaded && Investment != null)
        {
            _ = LoadChartsAsync();
        }
    }

    private void UpdateVisuals()
    {
        if (Investment == null) return;

        NameText.Text = Investment.Name;
        TickerText.Text = Investment.Ticker ?? "SEM TICKER";

        // Is it a Watchlist item?
        if (Investment.IsWatchlist)
        {
            QuantityText.Text = "-";
            AveragePriceText.Text = "-";
            TotalValueText.Text = $"Cotação: R$ {Investment.CurrentPrice:N2}";
            TotalValueText.Foreground = new SolidColorBrush(Colors.White);
        }
        else
        {
            QuantityText.Text = $"{Investment.Quantity:N2}";
            AveragePriceText.Text = $"R$ {Investment.AveragePrice:N2}";
            TotalValueText.Text = $"R$ {Investment.CurrentValue:N2}";

            // Color code return
            if (Investment.GainLossPercent >= 0)
            {
                TotalValueText.Foreground = new SolidColorBrush(Color.FromArgb(255, 34, 197, 94)); // Green
            }
            else
            {
                TotalValueText.Foreground = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));  // Red
            }
        }

        CurrentPriceText.Text = $"R$ {Investment.CurrentPrice:N2}";
    }

    private async Task LoadChartsAsync()
    {
        if (Investment == null) return;

        LoadingOverlay.Visibility = Visibility.Visible;
        try
        {
            // Load history snapshots for sparkline/trend line
            var historyTask = _apiClient.GetInvestmentHistoryAsync(investmentId: Investment.Id);
            
            // Load candlestick data (default 30 days)
            var candleTask = _apiClient.GetInvestmentCandlesticksAsync(Investment.Id);

            await Task.WhenAll(historyTask, candleTask);

            var history = await historyTask;
            var candlesticks = await candleTask;

            // Bind trend chart
            TrendChart.ItemsSource = history.Select(x => new ChartDataPoint(x.Date.ToString("dd/MM"), (double)x.Value)).ToList();

            // Bind candlestick chart
            CandleChart.ItemsSource = candlesticks.Select(x => new CandlestickDataPoint(
                x.Date,
                (double)x.Open,
                (double)x.High,
                (double)x.Low,
                (double)x.Close,
                x.Volume
            )).ToList();

            _dataLoaded = true;
        }
        catch
        {
            // Fail silently or fallback, charts remain empty
        }
        finally
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private void ChartType_Checked(object sender, RoutedEventArgs e)
    {
        if (TrendChart == null || CandleChart == null) return;

        if (TrendTabButton.IsChecked == true)
        {
            TrendChart.Visibility = Visibility.Visible;
            CandleChart.Visibility = Visibility.Collapsed;

            // Custom radio style updates
            TrendTabButton.Foreground = new SolidColorBrush(Colors.White);
            TrendTabButton.Background = new SolidColorBrush(Color.FromArgb(255, 124, 92, 255)); // Active purple
            CandleTabButton.Foreground = new SolidColorBrush(Color.FromArgb(150, 255, 255, 255));
            CandleTabButton.Background = new SolidColorBrush(Colors.Transparent);
        }
        else if (CandleTabButton.IsChecked == true)
        {
            TrendChart.Visibility = Visibility.Collapsed;
            CandleChart.Visibility = Visibility.Visible;

            // Custom radio style updates
            CandleTabButton.Foreground = new SolidColorBrush(Colors.White);
            CandleTabButton.Background = new SolidColorBrush(Color.FromArgb(255, 124, 92, 255)); // Active purple
            TrendTabButton.Foreground = new SolidColorBrush(Color.FromArgb(150, 255, 255, 255));
            TrendTabButton.Background = new SolidColorBrush(Colors.Transparent);
        }
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (Investment != null)
        {
            var page = FindParent<InvestmentsPage>(this);
            if (page != null)
            {
                page.EditInvestment(Investment);
            }
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (Investment != null)
        {
            var page = FindParent<InvestmentsPage>(this);
            if (page != null)
            {
                page.DeleteInvestment(Investment.Id);
            }
        }
    }

    private void Buy_Click(object sender, RoutedEventArgs e)
    {
        if (Investment != null)
        {
            var page = FindParent<InvestmentsPage>(this);
            if (page != null)
            {
                page.RecordInvestmentTransaction(Investment, "investment_buy");
            }
        }
    }

    private void Sell_Click(object sender, RoutedEventArgs e)
    {
        if (Investment != null)
        {
            var page = FindParent<InvestmentsPage>(this);
            if (page != null)
            {
                page.RecordInvestmentTransaction(Investment, "investment_sell");
            }
        }
    }

    private void Yield_Click(object sender, RoutedEventArgs e)
    {
        if (Investment != null)
        {
            var page = FindParent<InvestmentsPage>(this);
            if (page != null)
            {
                page.RecordInvestmentTransaction(Investment, "investment_yield");
            }
        }
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        DependencyObject parentObject = VisualTreeHelper.GetParent(child);
        if (parentObject == null) return null;
        if (parentObject is T parent) return parent;
        return FindParent<T>(parentObject);
    }
}
