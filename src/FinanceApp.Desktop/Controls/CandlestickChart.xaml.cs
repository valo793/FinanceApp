using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace FinanceApp.Desktop.Controls;

public sealed class CandlestickDataPoint
{
    public DateOnly Date { get; }
    public double Open { get; }
    public double High { get; }
    public double Low { get; }
    public double Close { get; }
    public long Volume { get; }

    public CandlestickDataPoint(DateOnly date, double open, double high, double low, double close, long volume)
    {
        Date = date;
        Open = open;
        High = high;
        Low = low;
        Close = close;
        Volume = volume;
    }
}

public sealed partial class CandlestickChart : UserControl
{
    private List<CandlestickDataPoint>? _points;
    private readonly List<(Point Coordinate, CandlestickDataPoint Data)> _renderedCandles = [];

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable<CandlestickDataPoint>),
            typeof(CandlestickChart),
            new PropertyMetadata(null, OnItemsSourceChanged));

    public IEnumerable<CandlestickDataPoint>? ItemsSource
    {
        get => (IEnumerable<CandlestickDataPoint>?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public CandlestickChart()
    {
        InitializeComponent();
        ChartCanvas.PointerMoved += ChartCanvas_PointerMoved;
        ChartCanvas.PointerExited += ChartCanvas_PointerExited;
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CandlestickChart chart)
        {
            chart.UpdateData();
        }
    }

    private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        Redraw();
    }

    private void UpdateData()
    {
        _points = ItemsSource?.OrderBy(p => p.Date).ToList();
        Redraw();
    }

    private void Redraw()
    {
        GridLinesCanvas.Children.Clear();
        CandlesCanvas.Children.Clear();
        _renderedCandles.Clear();

        if (_points == null || _points.Count == 0) return;

        double width = ChartCanvas.ActualWidth;
        double height = ChartCanvas.ActualHeight;

        if (width <= 0 || height <= 0) return;

        double topPadding = 20;
        double bottomPadding = 25;
        double leftPadding = 50;
        double rightPadding = 20;

        double drawWidth = width - leftPadding - rightPadding;
        double drawHeight = height - topPadding - bottomPadding;

        if (drawWidth <= 0 || drawHeight <= 0) return;

        double minPrice = _points.Min(p => p.Low);
        double maxPrice = _points.Max(p => p.High);
        double priceRange = maxPrice - minPrice;

        // Give some margin
        if (priceRange == 0)
        {
            minPrice -= 1.0;
            maxPrice += 1.0;
            priceRange = 2.0;
        }
        else
        {
            minPrice -= priceRange * 0.05;
            maxPrice += priceRange * 0.05;
            priceRange = maxPrice - minPrice;
        }

        // Draw Y-axis grid lines and labels (3 lines)
        var gridStroke = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
        var labelBrush = new SolidColorBrush(Color.FromArgb(150, 255, 255, 255));

        for (int i = 0; i <= 3; i++)
        {
            double ratio = i / 3.0;
            double y = topPadding + (1 - ratio) * drawHeight;
            double val = minPrice + ratio * priceRange;

            // Line
            var line = new Line
            {
                X1 = leftPadding,
                Y1 = y,
                X2 = leftPadding + drawWidth,
                Y2 = y,
                Stroke = gridStroke,
                StrokeThickness = 1
            };
            GridLinesCanvas.Children.Add(line);

            // Label
            var text = new TextBlock
            {
                Text = val.ToString("F2"),
                FontSize = 10,
                Foreground = labelBrush
            };
            Canvas.SetLeft(text, 5);
            Canvas.SetTop(text, y - 8);
            GridLinesCanvas.Children.Add(text);
        }

        // Draw X-axis line
        var xAxis = new Line
        {
            X1 = leftPadding,
            Y1 = topPadding + drawHeight,
            X2 = leftPadding + drawWidth,
            Y2 = topPadding + drawHeight,
            Stroke = gridStroke,
            StrokeThickness = 1
        };
        GridLinesCanvas.Children.Add(xAxis);

        // Draw candles
        double candleWidth = Math.Clamp((drawWidth / _points.Count) * 0.65, 3.0, 20.0);
        double spacing = drawWidth / (_points.Count > 1 ? _points.Count - 1 : 1);

        var greenBrush = new SolidColorBrush(Color.FromArgb(255, 34, 197, 94));  // Success/Green
        var redBrush = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));    // Error/Red

        for (int i = 0; i < _points.Count; i++)
        {
            var p = _points[i];
            double x = leftPadding + (i * spacing);

            double yOpen = topPadding + drawHeight * (1.0 - (p.Open - minPrice) / priceRange);
            double yClose = topPadding + drawHeight * (1.0 - (p.Close - minPrice) / priceRange);
            double yHigh = topPadding + drawHeight * (1.0 - (p.High - minPrice) / priceRange);
            double yLow = topPadding + drawHeight * (1.0 - (p.Low - minPrice) / priceRange);

            bool isBullish = p.Close >= p.Open;
            var candleBrush = isBullish ? greenBrush : redBrush;

            // 1. Wick (Shadow line)
            var wick = new Line
            {
                X1 = x,
                Y1 = yHigh,
                X2 = x,
                Y2 = yLow,
                Stroke = candleBrush,
                StrokeThickness = 1.5
            };
            CandlesCanvas.Children.Add(wick);

            // 2. Real Body (Rectangle)
            double bodyHeight = Math.Max(2.0, Math.Abs(yClose - yOpen));
            double bodyY = Math.Min(yOpen, yClose);

            var body = new Rectangle
            {
                Width = candleWidth,
                Height = bodyHeight,
                Fill = candleBrush,
                RadiusX = 1,
                RadiusY = 1
            };
            Canvas.SetLeft(body, x - (candleWidth / 2));
            Canvas.SetTop(body, bodyY);
            CandlesCanvas.Children.Add(body);

            _renderedCandles.Add((new Point(x, bodyY + bodyHeight / 2), p));

            // Draw date labels on X-axis (only for some points to prevent crowding)
            if (_points.Count < 10 || i % (_points.Count / 5 + 1) == 0 || i == _points.Count - 1)
            {
                var xLabel = new TextBlock
                {
                    Text = p.Date.ToString("dd/MM"),
                    FontSize = 10,
                    Foreground = labelBrush,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Canvas.SetLeft(xLabel, x - 15);
                Canvas.SetTop(xLabel, topPadding + drawHeight + 5);
                GridLinesCanvas.Children.Add(xLabel);
            }
        }
    }

    private void ChartCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_renderedCandles.Count == 0) return;

        var pt = e.GetCurrentPoint(ChartCanvas).Position;

        // Find closest candle
        var closest = _renderedCandles
            .OrderBy(c => Math.Abs(c.Coordinate.X - pt.X))
            .First();

        double distanceX = Math.Abs(closest.Coordinate.X - pt.X);

        if (distanceX < 20)
        {
            var p = closest.Data;
            TooltipDate.Text = p.Date.ToString("dd/MM/yyyy");
            TooltipOHLC.Text = $"ABER (O): R$ {p.Open:F2}\nMÁX (H): R$ {p.High:F2}\nMÍN (L): R$ {p.Low:F2}\nFECH (C): R$ {p.Close:F2}\nVOL: {p.Volume:N0}";

            TooltipBorder.Visibility = Visibility.Visible;

            double tooltipX = closest.Coordinate.X + 15;
            double tooltipY = Math.Clamp(pt.Y - 50, 10, ChartCanvas.ActualHeight - TooltipBorder.ActualHeight - 10);

            if (tooltipX + TooltipBorder.ActualWidth > ChartCanvas.ActualWidth)
            {
                tooltipX = closest.Coordinate.X - TooltipBorder.ActualWidth - 15;
            }

            Canvas.SetLeft(TooltipBorder, tooltipX);
            Canvas.SetTop(TooltipBorder, tooltipY);
        }
        else
        {
            TooltipBorder.Visibility = Visibility.Collapsed;
        }
    }

    private void ChartCanvas_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        TooltipBorder.Visibility = Visibility.Collapsed;
    }
}
