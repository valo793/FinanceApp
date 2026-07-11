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

        // Reserve padding
        double topPadding = 20;
        double bottomPadding = 20;
        double leftPadding = 50;
        double rightPadding = 20;

        double drawWidth = width - leftPadding - rightPadding;
        double drawHeight = height - topPadding - bottomPadding;

        if (drawWidth <= 0 || drawHeight <= 0) return;

        double minY = _points.Min(p => p.Value);
        double maxY = _points.Max(p => p.Value);

        // Avoid division by zero
        if (Math.Abs(maxY - minY) < 0.01)
        {
            minY -= 100;
            maxY += 100;
        }

        // Add 10% breathing room to top and bottom
        double diffY = maxY - minY;
        minY -= diffY * 0.1;
        maxY += diffY * 0.1;
        diffY = maxY - minY;

        // Draw horizontal grid lines (3 steps)
        int gridSteps = 3;
        for (int i = 0; i <= gridSteps; i++)
        {
            double ratio = (double)i / gridSteps;
            double yVal = minY + (ratio * diffY);
            double yPos = topPadding + drawHeight - (ratio * drawHeight);

            // Grid Line
            var line = new Line
            {
                X1 = leftPadding,
                Y1 = yPos,
                X2 = width - rightPadding,
                Y2 = yPos,
                Stroke = new SolidColorBrush(ColorHelper.FromArgb(25, 255, 255, 255)),
                StrokeThickness = 1
            };
            GridLinesCanvas.Children.Add(line);

            // Y-Axis Label
            var textBlock = new TextBlock
            {
                Text = $"R$ {yVal:N0}",
                FontSize = 9,
                Foreground = new SolidColorBrush(ColorHelper.FromArgb(150, 255, 255, 255)),
                Width = leftPadding - 5,
                TextAlignment = TextAlignment.Right
            };
            Canvas.SetLeft(textBlock, 0);
            Canvas.SetTop(textBlock, yPos - 6);
            GridLinesCanvas.Children.Add(textBlock);
        }

        // Calculate coordinates
        for (int i = 0; i < _points.Count; i++)
        {
            double xRatio = _points.Count > 1 ? (double)i / (_points.Count - 1) : 0.5;
            double xPos = leftPadding + (xRatio * drawWidth);

            double yRatio = (double)(_points[i].Value - minY) / diffY;
            double yPos = topPadding + drawHeight - (yRatio * drawHeight);

            _renderedCoordinates.Add(new Point(xPos, yPos));
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
        AreaPath.Data = areaGeometry;

        // Draw small circles at data points
        foreach (var coord in _renderedCoordinates)
        {
            var dot = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = new SolidColorBrush(Colors.Cyan),
                Stroke = new SolidColorBrush(Colors.Black),
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
