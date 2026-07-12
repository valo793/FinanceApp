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

public sealed class WaterfallDataPoint
{
    public string Label { get; }
    public double Value { get; }
    public string Type { get; } // "start", "increase", "decrease", "end"

    public WaterfallDataPoint(string label, double value, string type)
    {
        Label = label;
        Value = value;
        Type = type;
    }
}

public sealed partial class WaterfallChart : UserControl
{
    private List<WaterfallDataPoint>? _points;
    private readonly List<(Rect BarBounds, WaterfallDataPoint Data, double StartValue, double EndValue)> _renderedBars = [];

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable<WaterfallDataPoint>),
            typeof(WaterfallChart),
            new PropertyMetadata(null, OnItemsSourceChanged));

    public IEnumerable<WaterfallDataPoint>? ItemsSource
    {
        get => (IEnumerable<WaterfallDataPoint>?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public WaterfallChart()
    {
        InitializeComponent();
        ChartCanvas.PointerMoved += ChartCanvas_PointerMoved;
        ChartCanvas.PointerExited += ChartCanvas_PointerExited;
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WaterfallChart chart)
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
        _points = ItemsSource?.ToList();
        Redraw();
    }

    private void Redraw()
    {
        GridLinesCanvas.Children.Clear();
        BarsCanvas.Children.Clear();
        _renderedBars.Clear();

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

        // Calculate intermediate and cumulative values
        double running = 0;
        var barValues = new List<(double Start, double End, WaterfallDataPoint Point)>();

        foreach (var p in _points)
        {
            double startVal = 0;
            double endVal = 0;

            if (p.Type == "start")
            {
                startVal = 0;
                endVal = p.Value;
                running = p.Value;
            }
            else if (p.Type == "increase")
            {
                startVal = running;
                endVal = running + p.Value;
                running = endVal;
            }
            else if (p.Type == "decrease")
            {
                startVal = running;
                endVal = running - p.Value;
                running = endVal;
            }
            else if (p.Type == "end")
            {
                startVal = 0;
                endVal = running;
            }

            barValues.Add((startVal, endVal, p));
        }

        double maxVal = barValues.Max(x => Math.Max(x.Start, x.End));
        double minVal = 0; // standard starting point
        double valueRange = maxVal - minVal;

        if (valueRange == 0)
        {
            valueRange = 100.0;
            maxVal = 100.0;
        }
        else
        {
            maxVal += valueRange * 0.10; // 10% margin on top
            valueRange = maxVal - minVal;
        }

        // Draw Y-axis grid lines and labels
        var gridStroke = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
        var labelBrush = new SolidColorBrush(Color.FromArgb(150, 255, 255, 255));

        for (int i = 0; i <= 3; i++)
        {
            double ratio = i / 3.0;
            double y = topPadding + (1 - ratio) * drawHeight;
            double val = minVal + ratio * valueRange;

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

            var text = new TextBlock
            {
                Text = $"R$ {val:N0}",
                FontSize = 10,
                Foreground = labelBrush
            };
            Canvas.SetLeft(text, 5);
            Canvas.SetTop(text, y - 8);
            GridLinesCanvas.Children.Add(text);
        }

        // Draw X-axis
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

        // Brushes
        var neutralBrush = new SolidColorBrush(Color.FromArgb(255, 124, 92, 255)); // Violet
        var greenBrush = new SolidColorBrush(Color.FromArgb(255, 34, 197, 94));    // Success/Green
        var redBrush = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));      // Error/Red

        double colSpacing = drawWidth / barValues.Count;
        double colWidth = colSpacing * 0.7;

        double lastConnectorY = 0;
        double lastConnectorX = 0;

        for (int i = 0; i < barValues.Count; i++)
        {
            var item = barValues[i];
            double x = leftPadding + (i * colSpacing) + (colSpacing - colWidth) / 2;

            double yStart = topPadding + drawHeight * (1.0 - (item.Start - minVal) / valueRange);
            double yEnd = topPadding + drawHeight * (1.0 - (item.End - minVal) / valueRange);

            double topY = Math.Min(yStart, yEnd);
            double bottomY = Math.Max(yStart, yEnd);
            double barHeight = Math.Max(2.0, bottomY - topY);

            SolidColorBrush fillBrush = item.Point.Type switch
            {
                "start" => neutralBrush,
                "end" => neutralBrush,
                "increase" => greenBrush,
                "decrease" => redBrush,
                _ => neutralBrush
            };

            // Rectangle body
            var rect = new Rectangle
            {
                Width = colWidth,
                Height = barHeight,
                Fill = fillBrush,
                RadiusX = 4,
                RadiusY = 4
            };
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, topY);
            BarsCanvas.Children.Add(rect);

            var bounds = new Rect(x, topY, colWidth, barHeight);
            _renderedBars.Add((bounds, item.Point, item.Start, item.End));

            // Connector line from last bar to this bar
            if (i > 0)
            {
                var connector = new Line
                {
                    X1 = lastConnectorX,
                    Y1 = lastConnectorY,
                    X2 = x,
                    Y2 = lastConnectorY,
                    Stroke = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)),
                    StrokeThickness = 1
                };
                connector.StrokeDashArray = new DoubleCollection { 3, 3 };
                BarsCanvas.Children.Add(connector);
            }

            // Set up last connector coordinates
            // Connectors link the end level of the previous bar to the start level of the next bar
            lastConnectorX = x + colWidth;
            lastConnectorY = yEnd;

            // X-axis label
            var text = new TextBlock
            {
                Text = item.Point.Label,
                FontSize = 10,
                Foreground = labelBrush,
                Width = colSpacing,
                HorizontalTextAlignment = TextAlignment.Center
            };
            Canvas.SetLeft(text, leftPadding + (i * colSpacing));
            Canvas.SetTop(text, topPadding + drawHeight + 5);
            GridLinesCanvas.Children.Add(text);
        }
    }

    private void ChartCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_renderedBars.Count == 0) return;

        var pt = e.GetCurrentPoint(ChartCanvas).Position;

        // Find if pointer is over any bar
        var hoverItem = _renderedBars
            .FirstOrDefault(bar => pt.X >= bar.BarBounds.Left && pt.X <= bar.BarBounds.Right);

        if (hoverItem.Data != null)
        {
            var p = hoverItem.Data;
            TooltipLabel.Text = p.Label;

            if (p.Type == "start" || p.Type == "end")
            {
                TooltipValue.Text = $"R$ {p.Value:N2}";
                TooltipValue.Foreground = new SolidColorBrush(Colors.White);
            }
            else
            {
                var sign = p.Type == "increase" ? "+" : "-";
                TooltipValue.Text = $"{sign} R$ {p.Value:N2}";
                TooltipValue.Foreground = new SolidColorBrush(p.Type == "increase" ? Color.FromArgb(255, 34, 197, 94) : Color.FromArgb(255, 239, 68, 68));
            }

            TooltipBorder.Visibility = Visibility.Visible;

            double tooltipX = hoverItem.BarBounds.Left + (hoverItem.BarBounds.Width / 2) - (TooltipBorder.ActualWidth / 2);
            double tooltipY = hoverItem.BarBounds.Top - TooltipBorder.ActualHeight - 10;

            if (tooltipY < 5)
            {
                tooltipY = hoverItem.BarBounds.Bottom + 10;
            }

            tooltipX = Math.Clamp(tooltipX, 10, ChartCanvas.ActualWidth - TooltipBorder.ActualWidth - 10);

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
