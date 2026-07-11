using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceApp.Contracts.Investments;
using FinanceApp.Desktop.Controls;
using FinanceApp.Desktop.Exceptions;
using FinanceApp.Desktop.Services;

namespace FinanceApp.Desktop.ViewModels;

public partial class InvestmentsViewModel(ApiClient apiClient, InfoBarService infoBarService) : ObservableObject
{
    [ObservableProperty] private ObservableCollection<InvestmentDto> investments = [];
    [ObservableProperty] private ObservableCollection<ChartDataPoint> historyPoints = [];
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;

    [ObservableProperty] private decimal totalInvested;
    [ObservableProperty] private decimal currentValue;
    [ObservableProperty] private decimal gainLossPercent;

    public string TotalInvestedText => $"R$ {TotalInvested:N2}";
    public string CurrentValueText => $"R$ {CurrentValue:N2}";
    public string GainLossPercentText => $"{GainLossPercent:+0.00;-0.00;0.00}%";

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var items = await apiClient.GetInvestmentsAsync();
            Investments = new ObservableCollection<InvestmentDto>(items);

            var summary = await apiClient.GetInvestmentSummaryAsync();
            if (summary != null)
            {
                TotalInvested = summary.TotalInvested;
                CurrentValue = summary.CurrentValue;
                GainLossPercent = summary.GainLossPercent;
            }
            else
            {
                TotalInvested = 0;
                CurrentValue = 0;
                GainLossPercent = 0;
            }

            OnPropertyChanged(nameof(TotalInvestedText));
            OnPropertyChanged(nameof(CurrentValueText));
            OnPropertyChanged(nameof(GainLossPercentText));

            var history = await apiClient.GetInvestmentHistoryAsync();
            HistoryPoints = new ObservableCollection<ChartDataPoint>(
                history.Select(x => new ChartDataPoint(x.Date.ToString("dd/MM"), (double)x.Value))
            );
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            infoBarService.Error($"Falha ao carregar investimentos: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SyncPricesAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await apiClient.SyncInvestmentsAsync();
            infoBarService.Success("Cotações de mercado sincronizadas com sucesso!");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            infoBarService.Error($"Erro ao sincronizar cotações: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task CreateInvestmentAsync(CreateInvestmentRequest request)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await apiClient.CreateInvestmentAsync(request);
            infoBarService.Success("Investimento adicionado com sucesso!");
            await LoadAsync();
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
            infoBarService.Error($"Erro ao adicionar investimento: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task UpdateInvestmentAsync((Guid Id, UpdateInvestmentRequest Request) tuple)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await apiClient.UpdateInvestmentAsync(tuple.Id, tuple.Request);
            infoBarService.Success("Investimento atualizado com sucesso!");
            await LoadAsync();
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
            infoBarService.Error($"Erro ao atualizar investimento: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task DeleteInvestmentAsync(Guid id)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await apiClient.DeleteInvestmentAsync(id);
            infoBarService.Success("Investimento excluído com sucesso!");
            await LoadAsync();
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
            infoBarService.Error($"Erro ao excluir investimento: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
