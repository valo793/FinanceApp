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

namespace FinanceApp.Desktop.Controls;

public sealed class ChartDataPoint
{
    public string Label { get; }
    public double Value { get; }

    public ChartDataPoint(string label, double value)
    {
        Label = label;
        Value = value;
    }
}

public sealed partial class LineChart : UserControl
{
    private List<ChartDataPoint>? _points;
    private readonly List<Point> _renderedCoordinates = [];
    private Ellipse? _hoverMarker;

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable<ChartDataPoint>),
            typeof(LineChart),
            new PropertyMetadata(null, OnItemsSourceChanged));

    public IEnumerable<ChartDataPoint>? ItemsSource
    {
        get => (IEnumerable<ChartDataPoint>?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly DependencyProperty LineColorProperty =
        DependencyProperty.Register(
            nameof(LineColor),
            typeof(Windows.UI.Color),
            typeof(LineChart),
            new PropertyMetadata(Windows.UI.Color.FromArgb(255, 59, 130, 246), (d, e) => (d as LineChart)?.UpdateData()));

    public Windows.UI.Color LineColor
    {
        get => (Windows.UI.Color)GetValue(LineColorProperty);
        set => SetValue(LineColorProperty, value);
    }

    public LineChart()
    {
        InitializeComponent();
        ChartCanvas.PointerMoved += ChartCanvas_PointerMoved;
        ChartCanvas.PointerExited += ChartCanvas_PointerExited;
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LineChart chart)
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
        MarkersCanvas.Children.Clear();
        _renderedCoordinates.Clear();
        LinePath.Data = null;
        AreaPath.Data = null;

        if (_points == null || _points.Count == 0) return;

        double width = ChartCanvas.ActualWidth;
        double height = ChartCanvas.ActualHeight;

        if (width <= 0 || height <= 0) return;

        // Reserve padding (generous bottom for X-axis dates)
        double topPadding = 8;
        double bottomPadding = 30;
        double leftPadding = 42;
        double rightPadding = 18;

        double drawWidth = width - leftPadding - rightPadding;
        double drawHeight = height - topPadding - bottomPadding;

        if (drawWidth <= 0 || drawHeight <= 0) return;

        double minY = _points.Min(p => p.Value);
        double maxY = _points.Max(p => p.Value);

        // Avoid division by zero
        if (Math.Abs(maxY - minY) < 0.01)
        {
            minY -= 1.0;
            maxY += 1.0;
        }

        double diffY = maxY - minY;

        // Draw Y-Axis Grid Lines and Labels
        int gridLineCount = 3;
        for (int i = 0; i <= gridLineCount; i++)
        {
            double yRatio = (double)i / gridLineCount;
            double yPos = topPadding + drawHeight - (yRatio * drawHeight);
            double yVal = minY + (yRatio * diffY);

            // Grid line
            var line = new Line
            {
                X1 = leftPadding,
                Y1 = yPos,
                X2 = leftPadding + drawWidth,
                Y2 = yPos,
                Stroke = new SolidColorBrush(ColorHelper.FromArgb(25, 255, 255, 255)),
                StrokeThickness = 1
            };
            GridLinesCanvas.Children.Add(line);

            // Smart Y-Axis Label
            string formattedY;
            if (yVal >= 1_000_000)
                formattedY = $"R$ {yVal / 1_000_000:N1}M";
            else if (yVal >= 10_000)
                formattedY = $"R$ {yVal / 1_000:N0}k";
            else
                formattedY = $"R$ {yVal:N0}";

            var textBlock = new TextBlock
            {
                Text = formattedY,
                FontSize = 9,
                Foreground = new SolidColorBrush(ColorHelper.FromArgb(150, 255, 255, 255)),
                Width = leftPadding - 6,
                TextAlignment = TextAlignment.Right
            };
            Canvas.SetLeft(textBlock, 0);
            Canvas.SetTop(textBlock, yPos - 6);
            GridLinesCanvas.Children.Add(textBlock);
        }

        // Draw X-axis bottom baseline
        var xAxisLine = new Line
        {
            X1 = leftPadding,
            Y1 = topPadding + drawHeight,
            X2 = leftPadding + drawWidth,
            Y2 = topPadding + drawHeight,
            Stroke = new SolidColorBrush(ColorHelper.FromArgb(25, 255, 255, 255)),
            StrokeThickness = 1
        };
        GridLinesCanvas.Children.Add(xAxisLine);

        // Calculate coordinates and draw X-axis date labels
        for (int i = 0; i < _points.Count; i++)
        {
            double xRatio = _points.Count > 1 ? (double)i / (_points.Count - 1) : 0.5;
            double xPos = leftPadding + (xRatio * drawWidth);

            double yRatio = (double)(_points[i].Value - minY) / diffY;
            double yPos = topPadding + drawHeight - (yRatio * drawHeight);

            _renderedCoordinates.Add(new Point(xPos, yPos));

            // Render X-axis date label for selected sample points
            if (_points.Count <= 5 || i % (_points.Count / 4 + 1) == 0 || i == _points.Count - 1)
            {
                var xLabel = new TextBlock
                {
                    Text = _points[i].Label,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(ColorHelper.FromArgb(180, 255, 255, 255)),
                    Width = 40,
                    TextAlignment = TextAlignment.Center
                };
                Canvas.SetLeft(xLabel, xPos - 20);
                Canvas.SetTop(xLabel, topPadding + drawHeight + 6);
                GridLinesCanvas.Children.Add(xLabel);
            }
        }

        // Create Path Geometry for Line
        var lineFigure = new PathFigure { StartPoint = _renderedCoordinates[0], IsClosed = false };
        var lineGeometry = new PathGeometry();
        lineGeometry.Figures.Add(lineFigure);

        // Create Path Geometry for Area
        var areaFigure = new PathFigure { StartPoint = new Point(_renderedCoordinates[0].X, topPadding + drawHeight), IsClosed = true };
        var areaGeometry = new PathGeometry();
        areaGeometry.Figures.Add(areaFigure);

        areaFigure.Segments.Add(new LineSegment { Point = _renderedCoordinates[0] });

        for (int i = 1; i < _renderedCoordinates.Count; i++)
        {
            lineFigure.Segments.Add(new LineSegment { Point = _renderedCoordinates[i] });
            areaFigure.Segments.Add(new LineSegment { Point = _renderedCoordinates[i] });
        }

        areaFigure.Segments.Add(new LineSegment { Point = new Point(_renderedCoordinates.Last().X, topPadding + drawHeight) });

        LinePath.Data = lineGeometry;
        LinePath.Stroke = new SolidColorBrush(LineColor);

        AreaPath.Data = areaGeometry;
        var gradient = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1)
        };
        gradient.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(50, LineColor.R, LineColor.G, LineColor.B), Offset = 0.0 });
        gradient.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(0, LineColor.R, LineColor.G, LineColor.B), Offset = 1.0 });
        AreaPath.Fill = gradient;

        // Draw small circles at data points
        foreach (var coord in _renderedCoordinates)
        {
            var dot = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = new SolidColorBrush(LineColor),
                Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 10, 10, 15)),
                StrokeThickness = 1
            };
            Canvas.SetLeft(dot, coord.X - 3);
            Canvas.SetTop(dot, coord.Y - 3);
            MarkersCanvas.Children.Add(dot);
        }
    }

    private void ChartCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_points == null || _points.Count == 0 || _renderedCoordinates.Count == 0) return;
        var pt = e.GetCurrentPoint(ChartCanvas).Position;

        int closestIdx = 0;
        double minDistance = double.MaxValue;
        for (int i = 0; i < _renderedCoordinates.Count; i++)
        {
            var coord = _renderedCoordinates[i];
            double dist = Math.Abs(coord.X - pt.X);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestIdx = i;
            }
        }

        if (closestIdx >= 0 && closestIdx < _renderedCoordinates.Count)
        {
            var selectedCoord = _renderedCoordinates[closestIdx];
            var dataPt = _points[closestIdx];

            UpdateHoverMarker(selectedCoord);

            TooltipBorder.Visibility = Visibility.Visible;
            TooltipLabel.Text = dataPt.Label;
            TooltipValue.Text = $"R$ {dataPt.Value:N2}";

            // Position tooltip
            double tooltipX = selectedCoord.X + 12;
            double tooltipY = selectedCoord.Y - 50;

            if (tooltipX + TooltipBorder.ActualWidth > ChartCanvas.ActualWidth)
                tooltipX = selectedCoord.X - TooltipBorder.ActualWidth - 12;
            if (tooltipY < 0)
                tooltipY = selectedCoord.Y + 12;

            Canvas.SetLeft(TooltipBorder, tooltipX);
            Canvas.SetTop(TooltipBorder, tooltipY);
        }
    }

    private void ChartCanvas_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        TooltipBorder.Visibility = Visibility.Collapsed;
        HideHoverMarker();
    }

    private void UpdateHoverMarker(Point pos)
    {
        if (_hoverMarker == null)
        {
            _hoverMarker = new Ellipse
            {
                Width = 14,
                Height = 14,
                Fill = new SolidColorBrush(ColorHelper.FromArgb(80, 0, 255, 255)),
                Stroke = new SolidColorBrush(Colors.Cyan),
                StrokeThickness = 2
            };
            ChartCanvas.Children.Add(_hoverMarker);
        }

        _hoverMarker.Visibility = Visibility.Visible;
        Canvas.SetLeft(_hoverMarker, pos.X - 7);
        Canvas.SetTop(_hoverMarker, pos.Y - 7);
    }

    private void HideHoverMarker()
    {
        if (_hoverMarker != null)
        {
            _hoverMarker.Visibility = Visibility.Collapsed;
        }
    }
}
