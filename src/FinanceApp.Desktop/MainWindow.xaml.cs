using FinanceApp.Desktop.Pages;
using FinanceApp.Desktop.Services;
using FinanceApp.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinanceApp.Desktop;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Register InfoBar
        var infoBarService = App.Host.Services.GetRequiredService<InfoBarService>();
        infoBarService.Register(GlobalInfoBar);

        // Restore Window Display Mode
        _ = RestoreWindowModeAsync();

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

    public async Task RestoreWindowModeAsync()
    {
        var sessionStorage = App.Host.Services.GetRequiredService<SessionStorage>();
        var mode = await sessionStorage.LoadWindowStateAsync();
        ApplyWindowMode(mode);
    }

    public void ApplyWindowMode(string mode)
    {
        var appWindow = this.AppWindow;
        if (appWindow == null) return;

        switch (mode.ToLower())
        {
            case "fullscreen":
                appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
                break;
            case "maximized":
                appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
                if (appWindow.Presenter is OverlappedPresenter opMax)
                {
                    opMax.Maximize();
                }
                break;
            case "windowed":
            default:
                appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
                if (appWindow.Presenter is OverlappedPresenter opWin)
                {
                    opWin.Restore();
                }
                break;
        }
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
                "transactions" => typeof(TransactionsPage),
                "accounts" => typeof(AccountsPage),
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
                        "transactions" => typeof(TransactionsPage),
                        "accounts" => typeof(AccountsPage),
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

    private void NavAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        if (!NavView.IsPaneVisible) return;

        var tag = sender.Key switch
        {
            Windows.System.VirtualKey.D => "dashboard",
            Windows.System.VirtualKey.T => "transactions",
            Windows.System.VirtualKey.A => "accounts",
            Windows.System.VirtualKey.R => "recurring",
            Windows.System.VirtualKey.I => "investments",
            Windows.System.VirtualKey.S => "settings",
            _ => null
        };

        if (tag != null)
        {
            ExecutePaletteCommand(tag);
        }
    }

    private void LogoutAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        if (!NavView.IsPaneVisible) return;
        _ = ConfirmLogoutAsync();
    }

    private void FullScreenAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        ToggleFullScreen();
    }

    private void TogglePaneAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        if (!NavView.IsPaneVisible) return;
        TogglePane();
    }

    private async void OpenCommandPaletteAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        if (!NavView.IsPaneVisible) return;
        await ShowCommandPaletteAsync();
    }

    private void ToggleValuesAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        if (!NavView.IsPaneVisible) return;
        if (RootFrame.Content is DashboardPage dashboardPage)
        {
            dashboardPage.ViewModel.ToggleValuesVisibilityCommand.Execute(null);
        }
    }

    private void ToggleFullScreen()
    {
        var appWindow = this.AppWindow;
        if (appWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen)
        {
            appWindow.SetPresenter(AppWindowPresenterKind.Default);
        }
        else
        {
            appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
        }
    }

    private void TogglePane()
    {
        NavView.IsPaneOpen = !NavView.IsPaneOpen;
    }

    private void ExecutePaletteCommand(string tag)
    {
        if (tag == "fullscreen")
        {
            ToggleFullScreen();
        }
        else if (tag == "togglepane")
        {
            TogglePane();
        }
        else if (tag == "logout")
        {
            _ = ConfirmLogoutAsync();
        }
        else
        {
            foreach (var menuItem in NavView.MenuItems)
            {
                if (menuItem is NavigationViewItem navItem && navItem.Tag?.ToString() == tag)
                {
                    NavView.SelectedItem = navItem;
                    break;
                }
            }
        }
    }

    private async Task ShowCommandPaletteAsync()
    {
        var suggestBox = new AutoSuggestBox
        {
            PlaceholderText = "Pesquise por uma tela ou comando (ex: Transações)...",
            Width = 400,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            UpdateTextOnSelect = false
        };

        var items = new List<CommandPaletteItem>
        {
            new("Ir para Dashboard (Ctrl+D)", "dashboard", Symbol.Home),
            new("Ir para Transações (Ctrl+T)", "transactions", Symbol.List),
            new("Ir para Contas (Ctrl+A)", "accounts", Symbol.Contact),
            new("Ir para Recorrências (Ctrl+R)", "recurring", Symbol.Refresh),
            new("Ir para Investimentos (Ctrl+I)", "investments", Symbol.Globe),
            new("Ir para Configurações (Ctrl+S)", "settings", Symbol.Setting),
            new("Alternar Tela Cheia (F11)", "fullscreen", Symbol.FullScreen),
            new("Alternar Painel Lateral (Ctrl+B)", "togglepane", Symbol.View),
            new("Sair da Conta (Ctrl+Q)", "logout", Symbol.Import)
        };

        suggestBox.TextChanged += (sender, args) =>
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var query = sender.Text.ToLowerInvariant();
                var matches = items
                    .Where(i => i.Title.ToLowerInvariant().Contains(query))
                    .ToList();
                sender.ItemsSource = matches;
            }
        };

        suggestBox.SuggestionChosen += (sender, args) =>
        {
            if (args.SelectedItem is CommandPaletteItem chosen)
            {
                sender.Text = chosen.Title;
            }
        };

        var dialog = new ContentDialog
        {
            Title = "Buscar Tela / Comando (Ctrl+P)",
            Content = suggestBox,
            CloseButtonText = "Fechar",
            XamlRoot = this.Content.XamlRoot,
            RequestedTheme = ElementTheme.Dark
        };

        suggestBox.QuerySubmitted += (sender, args) =>
        {
            dialog.Hide();
            CommandPaletteItem selected = null;
            if (args.ChosenSuggestion is CommandPaletteItem item)
            {
                selected = item;
            }
            else if (!string.IsNullOrWhiteSpace(sender.Text))
            {
                var query = sender.Text.ToLowerInvariant();
                selected = items.FirstOrDefault(i => i.Title.ToLowerInvariant().Contains(query));
            }

            if (selected != null)
            {
                ExecutePaletteCommand(selected.Tag);
            }
        };

        suggestBox.ItemsSource = items;

        suggestBox.ItemTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(@"
            <DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                <StackPanel Orientation='Horizontal' Spacing='12' Padding='4,8'>
                    <SymbolIcon Symbol='{Binding Icon}' />
                    <TextBlock Text='{Binding Title}' VerticalAlignment='Center' FontSize='14' />
                </StackPanel>
            </DataTemplate>");

        await dialog.ShowAsync();
    }

    private class CommandPaletteItem
    {
        public string Title { get; }
        public string Tag { get; }
        public Symbol Icon { get; }

        public CommandPaletteItem(string title, string tag, Symbol icon)
        {
            Title = title;
            Tag = tag;
            Icon = icon;
        }
    }
}
