using System;
using FinanceApp.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using FinanceApp.Contracts.Auth;

namespace FinanceApp.Desktop.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = App.Host.Services.GetRequiredService<SettingsViewModel>();
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            await ViewModel.LoadCommand.ExecuteAsync(null);
            
            SelectComboBoxTag(ThemeInput, ViewModel.Theme);
            SelectComboBoxTag(WindowModeInput, ViewModel.WindowMode);
            SelectComboBoxTag(PeriodInput, ViewModel.DefaultDashboardPeriod);

            if (this.XamlRoot?.Content is FrameworkElement root)
            {
                root.RequestedTheme = ViewModel.Theme == "light" ? Microsoft.UI.Xaml.ElementTheme.Light : Microsoft.UI.Xaml.ElementTheme.Dark;
            }
        };
    }

    private void SelectComboBoxTag(ComboBox comboBox, string? tagValue)
    {
        if (tagValue == null) return;
        for (int i = 0; i < comboBox.Items.Count; i++)
        {
            if (comboBox.Items[i] is ComboBoxItem item && item.Tag?.ToString() == tagValue)
            {
                comboBox.SelectedIndex = i;
                break;
            }
        }
    }

    private async void SaveSettings_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.Theme = (ThemeInput.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "dark";
        ViewModel.WindowMode = (WindowModeInput.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "windowed";
        ViewModel.DefaultDashboardPeriod = (PeriodInput.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "current_month";

        await ViewModel.SaveCommand.ExecuteAsync(null);

        if (this.XamlRoot?.Content is FrameworkElement root)
        {
            root.RequestedTheme = ViewModel.Theme == "light" ? Microsoft.UI.Xaml.ElementTheme.Light : Microsoft.UI.Xaml.ElementTheme.Dark;
        }
    }

    private async void MfaToggle_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ViewModel.MfaEnabled)
        {
            // Disable MFA Flow
            var panel = new StackPanel { Spacing = 12 };
            panel.Children.Add(new TextBlock 
            { 
                Text = "Para desativar a autenticação de dois fatores, digite o código atual gerado pelo seu aplicativo autenticador:",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13
            });

            var codeBox = new TextBox 
            { 
                MaxLength = 6, 
                PlaceholderText = "123456", 
                FontSize = 16, 
                HorizontalAlignment = HorizontalAlignment.Stretch 
            };
            panel.Children.Add(codeBox);

            var dialog = new ContentDialog
            {
                Title = "Desativar 2FA (MFA)",
                Content = panel,
                PrimaryButtonText = "Desativar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
                RequestedTheme = ElementTheme.Dark
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var code = codeBox.Text.Trim();
                if (string.IsNullOrEmpty(code)) return;
                await ViewModel.DisableMfaAsync(code);
            }
        }
        else
        {
            // Enable MFA Flow
            var setup = await ViewModel.GetMfaSetupAsync();
            if (setup == null) return;

            var panel = new StackPanel { Spacing = 12 };
            panel.Children.Add(new TextBlock 
            { 
                Text = "Use um aplicativo autenticador (como Google Authenticator ou Microsoft Authenticator) para escanear a conta ou adicione a chave abaixo manualmente:",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13
            });

            var secretGrid = new Grid { Margin = new Thickness(0, 8, 0, 8) };
            secretGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            secretGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
            var secretBox = new TextBox 
            { 
                Text = setup.SecretKey, 
                IsReadOnly = true, 
                FontFamily = new FontFamily("Consolas"),
                HorizontalAlignment = HorizontalAlignment.Stretch 
            };
            Grid.SetColumn(secretBox, 0);
            secretGrid.Children.Add(secretBox);

            var copyBtn = new Button 
            { 
                Content = "Copiar", 
                Margin = new Thickness(8, 0, 0, 0)
            };
            copyBtn.Click += (s, ev) => 
            {
                var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
                package.SetText(setup.SecretKey);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
            };
            Grid.SetColumn(copyBtn, 1);
            secretGrid.Children.Add(copyBtn);
            panel.Children.Add(secretGrid);

            var codeLabel = new TextBlock { Text = "Digite o código gerado de 6 dígitos:", Margin = new Thickness(0, 8, 0, 0) };
            panel.Children.Add(codeLabel);

            var codeBox = new TextBox { MaxLength = 6, PlaceholderText = "123456", FontSize = 16, HorizontalAlignment = HorizontalAlignment.Stretch };
            panel.Children.Add(codeBox);

            var dialog = new ContentDialog
            {
                Title = "Configurar 2FA (MFA)",
                Content = panel,
                PrimaryButtonText = "Ativar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
                RequestedTheme = ElementTheme.Dark
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var code = codeBox.Text.Trim();
                if (string.IsNullOrEmpty(code)) return;
                
                await ViewModel.EnableMfaAsync(new MfaEnableRequest 
                { 
                    SecretKey = setup.SecretKey, 
                    Code = code 
                });
            }
        }
    }
}
