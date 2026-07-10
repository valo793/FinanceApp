using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceApp.Contracts.Recurring;
using FinanceApp.Desktop.Services;

namespace FinanceApp.Desktop.ViewModels;

public class PlannedOccurrence
{
    public Guid TemplateId { get; init; }
    public string Description { get; init; } = string.Empty;
    public string TransactionKind { get; init; } = "expense";
    public DateOnly PlannedDate { get; init; }
    public decimal Amount { get; init; }
    public string FrequencyText { get; init; } = string.Empty;
    public string StatusText { get; init; } = "Previsto";

    public string AmountFormatted => $"R$ {Amount:N2}";
    public string PlannedDateFormatted => PlannedDate.ToString("dd/MM/yyyy");
}

public partial class RecurringViewModel(ApiClient apiClient) : ObservableObject
{
    [ObservableProperty] private ObservableCollection<PlannedOccurrence> occurrences = [];
    [ObservableProperty] private string filterMode = "month"; // "week" or "month"
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            var templates = await apiClient.GetRecurringTransactionsAsync();
            
            var list = new List<PlannedOccurrence>();
            var limitDate = FilterMode == "week" 
                ? DateOnly.FromDateTime(DateTime.Today.AddDays(7)) 
                : DateOnly.FromDateTime(DateTime.Today.AddDays(30));
            
            foreach (var template in templates)
            {
                if (template.IsPaused || !template.IsActive) continue;
                
                var date = template.NextRunDate;
                int safetyLimit = 0;
                while (date <= limitDate && (template.EndDate == null || date <= template.EndDate) && safetyLimit < 100)
                {
                    safetyLimit++;
                    list.Add(new PlannedOccurrence
                    {
                        TemplateId = template.Id,
                        Description = template.Description,
                        TransactionKind = template.TransactionKind,
                        PlannedDate = date,
                        Amount = template.DefaultAmount,
                        FrequencyText = template.Frequency switch
                        {
                            "daily" => "Diário",
                            "weekly" => "Semanal",
                            "monthly" => "Mensal",
                            "yearly" => "Anual",
                            _ => template.Frequency
                        }
                    });
                    
                    date = template.Frequency switch
                    {
                        "daily" => date.AddDays(template.IntervalValue > 0 ? template.IntervalValue : 1),
                        "weekly" => date.AddDays(7 * (template.IntervalValue > 0 ? template.IntervalValue : 1)),
                        "monthly" => date.AddMonths(template.IntervalValue > 0 ? template.IntervalValue : 1),
                        "yearly" => date.AddYears(template.IntervalValue > 0 ? template.IntervalValue : 1),
                        _ => date.AddMonths(1)
                    };
                }
            }
            
            Occurrences = new ObservableCollection<PlannedOccurrence>(list.OrderBy(x => x.PlannedDate));
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

    [RelayCommand]
    public async Task SetFilterModeAsync(string mode)
    {
        FilterMode = mode;
        await LoadAsync();
    }

    [RelayCommand]
    public async Task CreateAsync(CreateRecurringRequest request)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await apiClient.CreateRecurringTransactionAsync(request);
            await LoadAsync();
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

    [RelayCommand]
    public async Task PauseAsync(Guid id)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await apiClient.PauseRecurringTransactionAsync(id);
            await LoadAsync();
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

    [RelayCommand]
    public async Task ResumeAsync(Guid id)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await apiClient.ResumeRecurringTransactionAsync(id);
            await LoadAsync();
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
