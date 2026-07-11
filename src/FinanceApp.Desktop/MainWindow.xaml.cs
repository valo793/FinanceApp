using FinanceApp.Desktop.Pages;
using FinanceApp.Desktop.Services;
using FinanceApp.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FinanceApp.Desktop;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Register InfoBar
        var infoBarService = App.Host.Services.GetRequiredService<InfoBarService>();
        infoBarService.Register(GlobalInfoBar);

        // Start with login page, hide nav
        NavView.IsPaneVisible = false;
        RootFrame.Navigate(typeof(LoginPage));

        // Subscribe to login success
        var loginVm = App.Host.Services.GetRequiredService<LoginViewModel>();
        loginVm.LoginSucceeded += () =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                NavView.IsPaneVisible = true;
                RootFrame.Navigate(typeof(DashboardPage));
            });
        };

        // Try to restore a saved session (auto-login)
        _ = TryRestoreSessionAsync();
    }

    private async Task TryRestoreSessionAsync()
    {
        var apiClient = App.Host.Services.GetRequiredService<ApiClient>();
        var restored = await apiClient.TryRestoreSessionAsync();

        if (restored)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                NavView.IsPaneVisible = true;
                RootFrame.Navigate(typeof(DashboardPage));
            });
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();
            if (tag == "logout")
            {
                _ = ConfirmLogoutAsync();
                return;
            }

            var pageType = tag switch
            {
                "dashboard" => typeof(DashboardPage),
                "expenses" => typeof(ExpensesPage),
                "incomes" => typeof(IncomesPage),
                "accounts" => typeof(AccountsPage),
                "categories" => typeof(CategoriesPage),
                "recurring" => typeof(RecurringPage),
                "investments" => typeof(InvestmentsPage),
                "settings" => typeof(SettingsPage),
                _ => typeof(DashboardPage)
            };
            RootFrame.Navigate(pageType);
        }
    }

    private async Task ConfirmLogoutAsync()
    {
        var dialog = new ContentDialog
        {
            Title = "Sair da Conta",
            Content = "Tem certeza que deseja sair de sua sessão atual?",
            PrimaryButtonText = "Sair",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.RootFrame.XamlRoot,
            RequestedTheme = ElementTheme.Dark
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await LogoutAsync();
        }
        else
        {
            var currentType = RootFrame.CurrentSourcePageType;
            foreach (var menuItem in NavView.MenuItems)
            {
                if (menuItem is NavigationViewItem navItem)
                {
                    var tag = navItem.Tag?.ToString();
                    var pageType = tag switch
                    {
                        "dashboard" => typeof(DashboardPage),
                        "expenses" => typeof(ExpensesPage),
                        "incomes" => typeof(IncomesPage),
                        "accounts" => typeof(AccountsPage),
                        "categories" => typeof(CategoriesPage),
                        "recurring" => typeof(RecurringPage),
                        "investments" => typeof(InvestmentsPage),
                        "settings" => typeof(SettingsPage),
                        _ => null
                    };
                    if (pageType == currentType)
                    {
                        NavView.SelectedItem = navItem;
                        break;
                    }
                }
            }
        }
    }

    private async Task LogoutAsync()
    {
        var apiClient = App.Host.Services.GetRequiredService<ApiClient>();
        await apiClient.LogoutAsync();
        
        DispatcherQueue.TryEnqueue(() =>
        {
            NavView.IsPaneVisible = false;
            RootFrame.Navigate(typeof(LoginPage));
            NavView.SelectedItem = null;
        });
    }
}
