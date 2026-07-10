using System;
using FinanceApp.Desktop.ViewModels;
using FinanceApp.Contracts.Accounts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace FinanceApp.Desktop.Pages;

public sealed partial class AccountsPage : Page
{
    public AccountsViewModel ViewModel { get; }

    public AccountsPage()
    {
        ViewModel = App.Host.Services.GetRequiredService<AccountsViewModel>();
        InitializeComponent();
        Loaded += async (_, _) => await ViewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void AddAccount_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var dialog = new AddAccountDialog
        {
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            if (string.IsNullOrWhiteSpace(dialog.AccountName))
            {
                return;
            }

            var request = new CreateAccountRequest
            {
                Name = dialog.AccountName,
                AccountType = dialog.AccountType,
                OpeningBalance = dialog.OpeningBalance,
                CurrencyCode = dialog.CurrencyCode,
                IncludeInNetWorth = dialog.IncludeInNetWorth,
                IsManual = true
            };
            
            await ViewModel.CreateAccountCommand.ExecuteAsync(request);
        }
    }

    private async void AccountActive_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggle && toggle.Tag is AccountDto dto)
        {
            if (dto.IsActive == toggle.IsOn)
            {
                return;
            }

            var request = new UpdateAccountRequest
            {
                Name = dto.Name,
                AccountType = dto.AccountType,
                CurrencyCode = dto.CurrencyCode,
                OpeningBalance = dto.OpeningBalance,
                IncludeInNetWorth = dto.IncludeInNetWorth,
                IsManual = dto.IsManual,
                IsActive = toggle.IsOn,
                LockVersion = dto.LockVersion
            };

            await ViewModel.UpdateAccountCommand.ExecuteAsync((dto.Id, request));
        }
    }
}
