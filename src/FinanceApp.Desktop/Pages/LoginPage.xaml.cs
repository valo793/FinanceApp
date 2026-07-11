using FinanceApp.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using FinanceApp.Contracts.Auth;

namespace FinanceApp.Desktop.Pages;

public sealed partial class LoginPage : Page
{
    public LoginViewModel ViewModel { get; }

    public LoginPage()
    {
        ViewModel = App.Host.Services.GetRequiredService<LoginViewModel>();
        InitializeComponent();

        ViewModel.MfaChallengeRequested += async (challengeToken) =>
        {
            var panel = new StackPanel { Spacing = 12 };
            panel.Children.Add(new TextBlock 
            { 
                Text = "Sua conta possui a autenticação de dois fatores ativa. Digite o código de 6 dígitos gerado no seu aplicativo autenticador:",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13
            });

            var codeBox = new TextBox 
            { 
                MaxLength = 6, 
                PlaceholderText = "123456", 
                FontSize = 18,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                TextAlignment = TextAlignment.Center
            };
            panel.Children.Add(codeBox);

            var dialog = new ContentDialog
            {
                Title = "Autenticação de Dois Fatores (MFA)",
                Content = panel,
                PrimaryButtonText = "Verificar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
                RequestedTheme = ElementTheme.Dark
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var code = codeBox.Text.Trim();
                if (string.IsNullOrEmpty(code)) return;

                var success = await ViewModel.VerifyMfaCodeAsync(new MfaVerifyRequest 
                { 
                    ChallengeToken = challengeToken, 
                    Code = code 
                });
            }
        };
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox pb)
            ViewModel.Password = pb.Password;
    }
}
