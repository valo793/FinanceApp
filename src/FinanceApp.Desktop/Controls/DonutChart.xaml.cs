using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace FinanceApp.Desktop.Controls;

public sealed partial class DonutChart : UserControl
{
    private List<ChartDataPoint>? _points;

    private static readonly Color[] PredefinedColors =
    [
        ColorHelper.FromArgb(255, 0, 255, 255),   // Cyan
        ColorHelper.FromArgb(255, 138, 43, 226),  // Violet
        ColorHelper.FromArgb(255, 0, 230, 118),   // Emerald
        ColorHelper.FromArgb(255, 255, 215, 0),   // Amber
        ColorHelper.FromArgb(255, 255, 87, 34),   // Coral
        ColorHelper.FromArgb(255, 41, 121, 255)   // Blue
    ];

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable<ChartDataPoint>),
            typeof(DonutChart),
            new PropertyMetadata(null, OnItemsSourceChanged));

    public IEnumerable<ChartDataPoint>? ItemsSource
    {
        get => (IEnumerable<ChartDataPoint>?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public DonutChart()
    {
        InitializeComponent();
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DonutChart chart)
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
        _points = ItemsSource?.Where(x => x.Value > 0).ToList();
        Redraw();
    }

    private void Redraw()
    {
        DonutCanvas.Children.Clear();
        LegendStack.Children.Clear();

        if (_points == null || _points.Count == 0)
        {
            // Show placeholder empty ring
            var placeholder = new Microsoft.UI.Xaml.Shapes.Ellipse
            {
                Width = 140,
                Height = 140,
                Stroke = new SolidColorBrush(ColorHelper.FromArgb(30, 255, 255, 255)),
                StrokeThickness = 16
            };
            Canvas.SetLeft(placeholder, 20);
            Canvas.SetTop(placeholder, 20);
            DonutCanvas.Children.Add(placeholder);

            var noDataText = new TextBlock
            {
                Text = "Sem dados",
                FontSize = 12,
                Foreground = new SolidColorBrush(ColorHelper.FromArgb(150, 255, 255, 255)),
                Width = 180,
                TextAlignment = TextAlignment.Center
            };
            Canvas.SetTop(noDataText, 82);
            DonutCanvas.Children.Add(noDataText);
            return;
        }

        double total = _points.Sum(p => p.Value);
        if (total <= 0) return;

        double center = 90;
        double radius = 68;
        double currentAngle = -90; // Start at top (12 o'clock)

        for (int i = 0; i < _points.Count; i++)
        {
            var pt = _points[i];
            var color = PredefinedColors[i % PredefinedColors.Length];
            var brush = new SolidColorBrush(color);

            double percentage = pt.Value / total;
            double sweepAngle = percentage * 360;

            if (percentage >= 0.999)
            {
                // Full circle
                var fullRing = new Microsoft.UI.Xaml.Shapes.Ellipse
                {
                    Width = radius * 2,
                    Height = radius * 2,
                    Stroke = brush,
                    StrokeThickness = 16
                };
                Canvas.SetLeft(fullRing, center - radius);
                Canvas.SetTop(fullRing, center - radius);
                DonutCanvas.Children.Add(fullRing);
            }
            else if (sweepAngle > 0)
            {
                // Draw arc segment
                var pathGeometry = new PathGeometry();
                var pathFigure = new PathFigure();

                double startRad = currentAngle * Math.PI / 180;
                double endRad = (currentAngle + sweepAngle) * Math.PI / 180;

                Point startPt = new Point(center + radius * Math.Cos(startRad), center + radius * Math.Sin(startRad));
                Point endPt = new Point(center + radius * Math.Cos(endRad), center + radius * Math.Sin(endRad));

                pathFigure.StartPoint = startPt;
                pathFigure.Segments.Add(new ArcSegment
                {
                    Point = endPt,
                    Size = new Size(radius, radius),
                    SweepDirection = SweepDirection.Clockwise,
                    IsLargeArc = sweepAngle > 180
                });

                var path = new Microsoft.UI.Xaml.Shapes.Path
                {
                    Data = pathGeometry,
                    Stroke = brush,
                    StrokeThickness = 16,
                    StrokeEndLineCap = PenLineCap.Round,
                    StrokeStartLineCap = PenLineCap.Round
                };
                pathGeometry.Figures.Add(pathFigure);
                DonutCanvas.Children.Add(path);
            }

            // Add legend item
            var legendItem = new Grid();
            legendItem.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            legendItem.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            legendItem.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var colorIndicator = new Microsoft.UI.Xaml.Shapes.Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = brush,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(colorIndicator, 0);
            legendItem.Children.Add(colorIndicator);

            var labelText = new TextBlock
            {
                Text = $"{pt.Label} ({percentage:P0})",
                FontSize = 12,
                Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(labelText, 1);
            legendItem.Children.Add(labelText);

            var valueText = new TextBlock
            {
                Text = $"R$ {pt.Value:N2}",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)Application.Current.Resources["TextPrimaryBrush"],
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(valueText, 2);
            legendItem.Children.Add(valueText);

            LegendStack.Children.Add(legendItem);

            currentAngle += sweepAngle;
        }

        // Add total text in the center of the donut
        var totalLabel = new TextBlock
        {
            Text = "TOTAL",
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(ColorHelper.FromArgb(120, 255, 255, 255)),
            Width = 180,
            TextAlignment = TextAlignment.Center
        };
        Canvas.SetTop(totalLabel, 72);
        DonutCanvas.Children.Add(totalLabel);

        var totalValue = new TextBlock
        {
            Text = $"R$ {total:N0}",
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)Application.Current.Resources["TextPrimaryBrush"],
            Width = 180,
            TextAlignment = TextAlignment.Center
        };
        Canvas.SetTop(totalValue, 88);
        DonutCanvas.Children.Add(totalValue);
    }
}
