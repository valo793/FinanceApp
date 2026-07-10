using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;

namespace FinanceApp.Desktop;

public partial class App : Application
{
    public static IHost Host { get; } = Microsoft.Extensions.Hosting.Host
        .CreateDefaultBuilder()
        .ConfigureServices((_, services) =>
        {
            services.AddSingleton<Services.ApiClient>();
            services.AddSingleton<ViewModels.LoginViewModel>();
            services.AddSingleton<ViewModels.DashboardViewModel>();
            services.AddSingleton<ViewModels.ExpensesViewModel>();
            services.AddSingleton<ViewModels.IncomesViewModel>();
            services.AddSingleton<ViewModels.AccountsViewModel>();
            services.AddSingleton<ViewModels.CategoriesViewModel>();
            services.AddSingleton<ViewModels.RecurringViewModel>();
        })
        .Build();

    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        await Host.StartAsync();
        _window = new MainWindow();
        _window.Activate();
    }
}
