using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceApp.Contracts.UserPreferences;
using FinanceApp.Contracts.Auth;
using FinanceApp.Desktop.Exceptions;
using FinanceApp.Desktop.Services;

namespace FinanceApp.Desktop.ViewModels;

public partial class SettingsViewModel(ApiClient apiClient, InfoBarService infoBarService) : ObservableObject
{
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;

    [ObservableProperty] private string theme = "dark";
    [ObservableProperty] private string? accentColor;
    [ObservableProperty] private string density = "comfortable";
    [ObservableProperty] private bool showValuesOnStart = true;
    [ObservableProperty] private string defaultDashboardPeriod = "current_month";
    [ObservableProperty] private bool mfaEnabled;

    public string MfaButtonText => MfaEnabled ? "Desativar 2FA" : "Ativar 2FA";

    partial void OnMfaEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(MfaButtonText));
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            var prefs = await apiClient.GetPreferencesAsync();
            if (prefs != null)
            {
                Theme = prefs.Theme;
                AccentColor = prefs.AccentColor;
                Density = prefs.Density;
                ShowValuesOnStart = prefs.ShowValuesOnStart;
                DefaultDashboardPeriod = prefs.DefaultDashboardPeriod;
            }
            MfaEnabled = apiClient.MfaEnabled;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            infoBarService.Error($"Erro ao carregar preferências: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var request = new UpdatePreferenceRequest
            {
                Theme = Theme,
                AccentColor = AccentColor,
                Density = Density,
                ShowValuesOnStart = ShowValuesOnStart,
                DefaultDashboardPeriod = DefaultDashboardPeriod
            };

            await apiClient.UpdatePreferencesAsync(request);
            infoBarService.Success("Configurações salvas com sucesso!");
        }
        catch (ConcurrencyConflictException ex)
        {
            ErrorMessage = ex.Message;
            infoBarService.Warning(ex.Message);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            infoBarService.Error($"Erro ao salvar configurações: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task<MfaSetupResponse?> GetMfaSetupAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            return await apiClient.SetupMfaAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            infoBarService.Error($"Erro ao iniciar setup MFA: {ex.Message}");
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task<bool> EnableMfaAsync(MfaEnableRequest request)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await apiClient.EnableMfaAsync(request);
            MfaEnabled = true;
            infoBarService.Success("Autenticação de Dois Fatores (MFA) ativada com sucesso!");
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            infoBarService.Error($"Erro ao ativar MFA: {ex.Message}");
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task<bool> DisableMfaAsync(string code)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            var request = new MfaEnableRequest { SecretKey = string.Empty, Code = code };
            await apiClient.DisableMfaAsync(request);
            MfaEnabled = false;
            infoBarService.Success("Autenticação de Dois Fatores (MFA) desativada com sucesso.");
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            infoBarService.Error($"Erro ao desativar MFA: {ex.Message}");
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
