using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using FinanceApp.Desktop.ViewModels;

namespace FinanceApp.Desktop.Pages;

public sealed partial class RecurringPage : Page
{
    public RecurringViewModel ViewModel { get; }

    public RecurringPage()
    {
        ViewModel = App.Host.Services.GetRequiredService<RecurringViewModel>();
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            await ViewModel.LoadCommand.ExecuteAsync(null);
        };
    }

    private async void PauseRecurring_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Guid id)
        {
            await ViewModel.PauseCommand.ExecuteAsync(id);
        }
    }

    private void OccurrenceButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is PlannedOccurrence occurrence)
        {
            var flyout = (Flyout)this.Resources["OccurrenceDetailsFlyout"];
            if (flyout.Content is StackPanel panel)
            {
                var desc = (TextBlock)panel.FindName("FlyoutDescription");
                var freq = (TextBlock)panel.FindName("FlyoutFrequency");
                var amt = (TextBlock)panel.FindName("FlyoutAmount");
                var pauseBtn = (Button)panel.FindName("FlyoutPauseButton");

                if (desc != null) desc.Text = occurrence.Description;
                if (freq != null) freq.Text = occurrence.FrequencyText;
                if (amt != null) amt.Text = occurrence.AmountFormatted;
                if (pauseBtn != null) pauseBtn.Tag = occurrence.TemplateId;
            }
            flyout.ShowAt(btn);
        }
    }

    private void OccurrencesPanel_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is StackPanel panel)
        {
            PopulateOccurrences(panel);
        }
    }

    private void OccurrencesPanel_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (sender is StackPanel panel)
        {
            PopulateOccurrences(panel);
        }
    }

    private void PopulateOccurrences(StackPanel panel)
    {
        if (panel.DataContext is CalendarDayCell cell)
        {
            panel.Children.Clear();
            foreach (var occurrence in cell.Occurrences)
            {
                var btn = new Button
                {
                    Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources[occurrence.TransactionKind == "income" ? "AccentCyanBrush" : "AccentVioletBrush"],
                    Padding = new Thickness(6, 3, 6, 3),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    BorderThickness = new Thickness(0),
                    CornerRadius = new CornerRadius(4),
                    Tag = occurrence
                };
                btn.Click += OccurrenceButton_Click;

                var tb = new TextBlock
                {
                    Text = occurrence.Description,
                    FontSize = 9,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextPrimaryBrush"],
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                btn.Content = tb;

                panel.Children.Add(btn);
            }
        }
    }
}
