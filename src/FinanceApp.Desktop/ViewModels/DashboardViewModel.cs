using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceApp.Contracts.Dashboards;
using FinanceApp.Desktop.Services;

namespace FinanceApp.Desktop.ViewModels;

public partial class DashboardViewModel(ApiClient apiClient) : ObservableObject
{
    [ObservableProperty] private DashboardOverviewDto? overview;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;

    public string CurrentBalanceText => Overview is null ? "R$ 0,00" : $"R$ {Overview.CurrentBalance:N2}";
    public string ProjectedBalanceText => Overview is null ? "R$ 0,00" : $"R$ {Overview.ProjectedBalance:N2}";
    public string MonthIncomeText => Overview is null ? "R$ 0,00" : $"R$ {Overview.MonthIncome:N2}";
    public string MonthExpensesText => Overview is null ? "R$ 0,00" : $"R$ {Overview.MonthExpenses:N2}";
    public string NetResultText => Overview is null ? "Resultado: R$ 0,00" : $"Resultado líquido: R$ {Overview.NetResult:N2}";
    public string PendingRecurrencesText => Overview is null ? "0 pendentes" : $"{Overview.PendingRecurrences} pendentes";

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            Overview = await apiClient.GetDashboardOverviewAsync();
            OnPropertyChanged(nameof(CurrentBalanceText));
            OnPropertyChanged(nameof(ProjectedBalanceText));
            OnPropertyChanged(nameof(MonthIncomeText));
            OnPropertyChanged(nameof(MonthExpensesText));
            OnPropertyChanged(nameof(NetResultText));
            OnPropertyChanged(nameof(PendingRecurrencesText));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
