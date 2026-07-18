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
using Microsoft.UI.Xaml.Input;
using FinanceApp.Contracts.Investments;
using FinanceApp.Desktop.Services;
using FinanceApp.Desktop.Pages;
using FinanceApp.Desktop.ViewModels;

namespace FinanceApp.Desktop.Controls;

public sealed partial class AssetBentoCard : UserControl
{
    private readonly ApiClient _apiClient;
    private bool _dataLoaded = false;

    // Global chart mode (shared across all card instances)
    private static bool _globalCandlestickMode = false;
    private static event Action? GlobalChartModeChanged;

    public static void SetGlobalChartMode(bool candlestick)
    {
        _globalCandlestickMode = candlestick;
        GlobalChartModeChanged?.Invoke();
    }

    // Global period and grouping (shared across all card instances)
    private static string _globalRange = "1M";
    private static string _globalDrill = "Dia";
    private static event Action? GlobalPeriodChanged;

    public static void SetGlobalPeriod(string range, string drill)
    {
        _globalRange = range;
        _globalDrill = drill;
        GlobalPeriodChanged?.Invoke();
    }

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
            if (card.Investment != null)
            {
                _ = card.LoadChartsAsync();
            }
        }
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        GlobalChartModeChanged += OnGlobalChartModeChanged;
        GlobalPeriodChanged += OnGlobalPeriodChanged;
        UpdateVisuals();
        ApplyChartMode();
        if (!_dataLoaded && Investment != null)
        {
            _ = LoadChartsAsync();
        }
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        GlobalChartModeChanged -= OnGlobalChartModeChanged;
        GlobalPeriodChanged -= OnGlobalPeriodChanged;
    }

    private void OnGlobalPeriodChanged()
    {
        if (Investment != null)
        {
            DispatcherQueue.TryEnqueue(async () =>
            {
                await LoadChartsAsync();
            });
        }
    }

    private void OnGlobalChartModeChanged()
    {
        DispatcherQueue.TryEnqueue(ApplyChartMode);
    }

    private void ApplyChartMode()
    {
        if (TrendChart == null || CandleChart == null) return;

        if (_globalCandlestickMode)
        {
            TrendChart.Visibility = Visibility.Collapsed;
            CandleChart.Visibility = Visibility.Visible;
        }
        else
        {
            TrendChart.Visibility = Visibility.Visible;
            CandleChart.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateVisuals()
    {
        if (Investment == null) return;

        NameText.Text = Investment.Name;
        TickerText.Text = Investment.Ticker ?? "SEM TICKER";
        
        // Show quantity details
        QuantityText.Text = $"Qtd: {Investment.Quantity:G}";
        
        // Show current unit price
        PriceDetailsText.Text = $"Unit: R$ {Investment.CurrentPrice:N2}";
        
        // Position value (CurrentValue = Qty * CurrentPrice)
        PositionValueText.Text = $"R$ {Investment.CurrentValue:N2}";
        
        // Gain/Loss percentage
        decimal gainPercent = Investment.GainLossPercent;
        if (gainPercent >= 0)
        {
            GainLossText.Text = $"+{gainPercent:N2}%";
            GainLossText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129)); // Emerald Green
        }
        else
        {
            GainLossText.Text = $"{gainPercent:N2}%";
            GainLossText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)); // Red
        }

        if (PinIcon != null && PinButton != null)
        {
            if (Investment.IsPinned)
            {
                PinIcon.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 6, 182, 212)); // Cyan
                PinIcon.Glyph = "\uE840"; // Unpin
                ToolTipService.SetToolTip(PinButton, "Desafixar gráfico");
            }
            else
            {
                PinIcon.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 156, 163, 175)); // Grey
                PinIcon.Glyph = "\uE718"; // Pin
                ToolTipService.SetToolTip(PinButton, "Fixar/Destacar gráfico");
            }
        }
    }

    private async Task LoadChartsAsync()
    {
        if (Investment == null) return;

        LoadingOverlay.Visibility = Visibility.Visible;
        try
        {
            DateOnly to = DateOnly.FromDateTime(DateTime.Today);
            DateOnly from = _globalRange switch
            {
                "6M" => to.AddMonths(-6),
                "1Y" => to.AddYears(-1),
                "5Y" => to.AddYears(-5),
                "1M" or _ => to.AddMonths(-1)
            };

            var candlesticks = await _apiClient.GetInvestmentCandlesticksAsync(Investment.Id, from, to);
            var rawPoints = candlesticks.Select(x => new CandlestickDataPoint(
                x.Date,
                (double)x.Open,
                (double)x.High,
                (double)x.Low,
                (double)x.Close,
                x.Volume
            )).ToList();

            List<CandlestickDataPoint> aggregated;
            switch (_globalDrill)
            {
                case "Semana":
                    aggregated = InvestmentsViewModel.AggregateByWeek(rawPoints);
                    break;
                case "Mês":
                    aggregated = InvestmentsViewModel.AggregateByMonth(rawPoints);
                    break;
                case "Trimestre":
                    aggregated = InvestmentsViewModel.AggregateByQuarter(rawPoints);
                    break;
                case "Ano":
                    aggregated = InvestmentsViewModel.AggregateByYear(rawPoints);
                    break;
                case "Dia":
                default:
                    aggregated = rawPoints;
                    break;
            }

            var closePoints = aggregated.Select(x => new ChartDataPoint(x.Date.ToString("dd/MM"), x.Close)).ToList();
            TrendChart.ItemsSource = closePoints;

            // Bind candlestick chart
            CandleChart.ItemsSource = aggregated;

            // Apply card-specific trend color
            if (closePoints.Count >= 2)
            {
                var first = closePoints.First().Value;
                var last = closePoints.Last().Value;
                TrendChart.LineColor = last >= first 
                    ? Windows.UI.Color.FromArgb(255, 16, 185, 129)  // Green (emerald-500)
                    : Windows.UI.Color.FromArgb(255, 239, 68, 68);  // Red (rose-500)
            }

            _dataLoaded = true;
        }
        catch
        {
            // Fail silently, charts remain empty
        }
        finally
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
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

    private void Card_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (Investment != null)
        {
            var page = FindParent<InvestmentsPage>(this);
            if (page != null)
            {
                page.ViewModel.SelectedInvestment = Investment;
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

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        if (Investment != null)
        {
            var page = FindParent<InvestmentsPage>(this);
            if (page != null)
            {
                _ = page.ViewModel.TogglePinCommand.ExecuteAsync(Investment.Id);
            }
        }
    }

    private void Card_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (Application.Current.Resources.TryGetValue("AccentCyanBrush", out var brush))
        {
            CardBorder.BorderBrush = (Brush)brush;
        }
    }

    private void Card_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (Application.Current.Resources.TryGetValue("BorderMutedBrush", out var brush))
        {
            CardBorder.BorderBrush = (Brush)brush;
        }
    }
}
