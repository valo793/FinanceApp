using System;
using FinanceApp.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace FinanceApp.Desktop.Pages;

public sealed partial class RecurringPage : Page
{
    public RecurringViewModel ViewModel { get; }

    public RecurringPage()
    {
        ViewModel = App.Host.Services.GetRequiredService<RecurringViewModel>();
        InitializeComponent();
        Loaded += async (_, _) => await ViewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void WeekFilter_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ViewModel.SetFilterModeCommand.ExecuteAsync("week");
    }

    private async void MonthFilter_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ViewModel.SetFilterModeCommand.ExecuteAsync("month");
    }

    private async void PauseTemplate_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Guid id)
        {
            await ViewModel.PauseCommand.ExecuteAsync(id);
        }
    }
}
