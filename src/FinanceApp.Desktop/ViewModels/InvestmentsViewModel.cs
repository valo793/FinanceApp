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
    [ObservableProperty] private ObservableCollection<CandlestickDataPoint> candlestickPoints = [];
    [ObservableProperty] private InvestmentDto? selectedInvestment;
    [ObservableProperty] private bool showCandlestickChart;
    [ObservableProperty] private string chartTitle = "Evolução do Patrimônio Investido";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;

    [ObservableProperty] private decimal totalInvested;
    [ObservableProperty] private decimal currentValue;
    [ObservableProperty] private decimal gainLossPercent;

    [ObservableProperty] private bool isWatchlistMode;
    [ObservableProperty] private string? selectedCategory = "Todas";
    [ObservableProperty] private InvestmentDto? selectedFilterInvestment;
    [ObservableProperty] private ObservableCollection<InvestmentDto> filterInvestments = [];
    public ObservableCollection<string> Categories { get; } = ["Todas", "Ações", "FIIs", "CDB", "Tesouro Direto", "Cripto", "Outros Fundos"];

    partial void OnIsWatchlistModeChanged(bool value)
    {
        _ = LoadAsync();
    }

    partial void OnSelectedCategoryChanged(string? value)
    {
        _ = LoadHistoryAsync();
    }

    partial void OnSelectedFilterInvestmentChanged(InvestmentDto? value)
    {
        _ = LoadHistoryAsync();
    }

    partial void OnSelectedInvestmentChanged(InvestmentDto? value)
    {
        if (value != null && !string.IsNullOrWhiteSpace(value.Ticker))
        {
            ChartTitle = $"Variação de Preço — {value.Ticker}";
            _ = LoadCandlesticksAsync(value.Id);
        }
        else
        {
            ChartTitle = "Evolução do Patrimônio Investido";
            ShowCandlestickChart = false;
            CandlestickPoints.Clear();
        }
    }

    private async Task LoadCandlesticksAsync(Guid id)
    {
        try
        {
            IsBusy = true;
            var points = await apiClient.GetInvestmentCandlesticksAsync(id);
            CandlestickPoints = new ObservableCollection<CandlestickDataPoint>(
                points.Select(x => new CandlestickDataPoint(
                    x.Date, 
                    (double)x.Open, 
                    (double)x.High, 
                    (double)x.Low, 
                    (double)x.Close, 
                    x.Volume))
            );
            ShowCandlestickChart = true;
        }
        catch (Exception ex)
        {
            infoBarService.Error($"Erro ao carregar dados de velas: {ex.Message}");
            ShowCandlestickChart = false;
        }
        finally
        {
            IsBusy = false;
        }
    }

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

            var items = await apiClient.GetInvestmentsAsync(isWatchlist: IsWatchlistMode);
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

            // Load filter investments list (only real assets for the history filter)
            var realAssets = await apiClient.GetInvestmentsAsync(isWatchlist: false);
            var filterList = new List<InvestmentDto> { new() { Id = Guid.Empty, Name = "Todos os ativos" } };
            filterList.AddRange(realAssets);
            FilterInvestments = new ObservableCollection<InvestmentDto>(filterList);
            if (SelectedFilterInvestment == null)
            {
                SelectedFilterInvestment = filterList.First();
            }

            await LoadHistoryAsync();
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
    public async Task LoadHistoryAsync()
    {
        try
        {
            string? catFilter = SelectedCategory == "Todas" || string.IsNullOrWhiteSpace(SelectedCategory) ? null : MapCategoryTag(SelectedCategory);
            Guid? invFilter = SelectedFilterInvestment?.Id == Guid.Empty ? null : SelectedFilterInvestment?.Id;

            var history = await apiClient.GetInvestmentHistoryAsync(category: catFilter, investmentId: invFilter);
            HistoryPoints = new ObservableCollection<ChartDataPoint>(
                history.Select(x => new ChartDataPoint(x.Date.ToString("dd/MM"), (double)x.Value))
            );
        }
        catch (Exception ex)
        {
            infoBarService.Error($"Erro ao carregar histórico: {ex.Message}");
        }
    }

    private string MapCategoryTag(string display) => display switch
    {
        "Ações" => "stock",
        "FIIs" => "fii",
        "CDB" => "cdb",
        "Tesouro Direto" => "tesouro",
        "Cripto" => "crypto",
        "Outros Fundos" => "fund",
        _ => display.ToLower()
    };

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
