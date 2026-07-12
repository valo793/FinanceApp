using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceApp.Contracts.Dashboards;
using FinanceApp.Desktop.Services;

using System.Linq;
using FinanceApp.Desktop.Controls;

namespace FinanceApp.Desktop.ViewModels;

public partial class DashboardViewModel(ApiClient apiClient, CacheService cacheService) : ObservableObject
{
    [ObservableProperty] private DashboardOverviewDto? overview;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private bool hideValues = true;

    public IEnumerable<ChartDataPoint> NetWorthPoints =>
        Overview?.NetWorthSeries?.Select(x => new ChartDataPoint(x.Date.ToString("dd/MM"), (double)x.Balance)) 
        ?? Enumerable.Empty<ChartDataPoint>();

    public IEnumerable<ChartDataPoint> CashflowPoints =>
        Overview?.CashflowSeries?.Select(x => new ChartDataPoint(x.Date.ToString("dd/MM"), (double)x.Balance)) 
        ?? Enumerable.Empty<ChartDataPoint>();

    public IEnumerable<ChartDataPoint> ExpenseByCategoryPoints =>
        Overview?.ExpenseByCategory?.Select(x => new ChartDataPoint(x.Name, (double)x.Amount)) 
        ?? Enumerable.Empty<ChartDataPoint>();

    public IEnumerable<WaterfallDataPoint> WaterfallPoints =>
        Overview?.WaterfallSeries?.Select(x => new WaterfallDataPoint(x.Label, (double)x.Value, x.Type)) 
        ?? Enumerable.Empty<WaterfallDataPoint>();

    public string CurrentBalanceText => HideValues ? "R$ •••••" : (Overview is null ? "R$ 0,00" : $"R$ {Overview.CurrentBalance:N2}");
    public string ProjectedBalanceText => HideValues ? "R$ •••••" : (Overview is null ? "R$ 0,00" : $"R$ {Overview.ProjectedBalance:N2}");
    public string MonthIncomeText => HideValues ? "R$ •••••" : (Overview is null ? "R$ 0,00" : $"R$ {Overview.MonthIncome:N2}");
    public string MonthExpensesText => HideValues ? "R$ •••••" : (Overview is null ? "R$ 0,00" : $"R$ {Overview.MonthExpenses:N2}");
    public string NetResultText => HideValues ? "Resultado: R$ •••••" : (Overview is null ? "Resultado líquido: R$ 0,00" : $"Resultado líquido: R$ {Overview.NetResult:N2}");
    public string PendingRecurrencesText => Overview is null ? "0 pendentes" : $"{Overview.PendingRecurrences} pendentes";

    public string VisibilityGlyph => HideValues ? "\uE7B3" : "\uE890";

    [RelayCommand]
    public void ToggleValuesVisibility()
    {
        HideValues = !HideValues;
        NotifyBindingsChanged();
    }

    private void NotifyBindingsChanged()
    {
        OnPropertyChanged(nameof(CurrentBalanceText));
        OnPropertyChanged(nameof(ProjectedBalanceText));
        OnPropertyChanged(nameof(MonthIncomeText));
        OnPropertyChanged(nameof(MonthExpensesText));
        OnPropertyChanged(nameof(NetResultText));
        OnPropertyChanged(nameof(VisibilityGlyph));
        OnPropertyChanged(nameof(NetWorthPoints));
        OnPropertyChanged(nameof(CashflowPoints));
        OnPropertyChanged(nameof(ExpenseByCategoryPoints));
        OnPropertyChanged(nameof(WaterfallPoints));
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            // Load user preferences first
            try
            {
                var prefs = await apiClient.GetPreferencesAsync();
                if (prefs != null)
                {
                    HideValues = !prefs.ShowValuesOnStart;
                }
            }
            catch
            {
                // Fallback if settings endpoint fails
                HideValues = false;
            }

            // Try loading from local cache first
            try
            {
                var cached = await cacheService.GetAsync<DashboardOverviewDto>("dashboard_overview", TimeSpan.FromMinutes(5));
                if (cached is not null)
                {
                    Overview = cached;
                    NotifyBindingsChanged();
                    OnPropertyChanged(nameof(PendingRecurrencesText));
                }
            }
            catch
            {
                // Ignore cache failures
            }

            Overview = await apiClient.GetDashboardOverviewAsync();
            NotifyBindingsChanged();
            OnPropertyChanged(nameof(PendingRecurrencesText));

            // Save to cache
            if (Overview is not null)
            {
                await cacheService.SetAsync("dashboard_overview", Overview);
            }
        }
        catch (Exception ex)
        {
            if (Overview is null)
            {
                ErrorMessage = ex.Message;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
