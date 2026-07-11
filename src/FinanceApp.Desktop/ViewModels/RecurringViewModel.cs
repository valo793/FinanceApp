using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
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

public class CalendarDayCell
{
    public DateOnly Date { get; init; }
    public int DayNumber => Date.Day;
    public bool IsCurrentMonth { get; init; }
    public bool IsToday => Date == DateOnly.FromDateTime(DateTime.Today);
    public List<PlannedOccurrence> Occurrences { get; init; } = [];
    public bool HasOccurrences => Occurrences.Count > 0;

    public Brush CellBackgroundBrush => IsCurrentMonth 
        ? (Brush)Application.Current.Resources["BgSurfaceBrush"] 
        : (Brush)Application.Current.Resources["BgPrimaryBrush"];

    public Brush CellBorderBrush => IsToday 
        ? (Brush)Application.Current.Resources["AccentCyanBrush"] 
        : (Brush)Application.Current.Resources["BorderSubtleBrush"];

    public Thickness CellBorderThickness => IsToday ? new Thickness(2) : new Thickness(1);

    public Brush CellForegroundBrush => IsToday
        ? (Brush)Application.Current.Resources["AccentCyanBrush"]
        : (IsCurrentMonth 
            ? (Brush)Application.Current.Resources["TextPrimaryBrush"] 
            : (Brush)Application.Current.Resources["TextSecondaryBrush"]);
}

public partial class RecurringViewModel(ApiClient apiClient) : ObservableObject
{
    [ObservableProperty] private ObservableCollection<PlannedOccurrence> occurrences = [];
    [ObservableProperty] private ObservableCollection<CalendarDayCell> calendarDays = [];
    [ObservableProperty] private string currentMonthYearText = string.Empty;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;

    private DateTime currentMonthDate = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            var templates = await apiClient.GetRecurringTransactionsAsync();
            
            var list = new List<PlannedOccurrence>();
            // Load occurrences from 1 month before current month to 2 months after to cover the visible grid bounds
            var startRange = DateOnly.FromDateTime(currentMonthDate.AddMonths(-1));
            var limitDate = DateOnly.FromDateTime(currentMonthDate.AddMonths(2));
            
            foreach (var template in templates)
            {
                if (template.IsPaused || !template.IsActive) continue;
                
                var date = template.NextRunDate;
                int safetyLimit = 0;
                while (date <= limitDate && (template.EndDate == null || date <= template.EndDate) && safetyLimit < 150)
                {
                    safetyLimit++;
                    if (date >= startRange)
                    {
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
                    }
                    
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
            GenerateCalendarDays();
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

    public void GenerateCalendarDays()
    {
        CurrentMonthYearText = currentMonthDate.ToString("MMMM yyyy", new System.Globalization.CultureInfo("pt-BR")).ToUpper();

        var daysList = new List<CalendarDayCell>();

        // Find the first day of the month
        var firstDayOfMonth = new DateTime(currentMonthDate.Year, currentMonthDate.Month, 1);
        // Find the day of week of the first day (Sunday = 0, Monday = 1, etc.)
        int startOffset = (int)firstDayOfMonth.DayOfWeek;
        
        // Start date of the grid (may be in the previous month)
        var startDate = firstDayOfMonth.AddDays(-startOffset);
        
        // Always show 6 weeks (42 days) to keep a stable grid size
        for (int i = 0; i < 42; i++)
        {
            var cellDate = DateOnly.FromDateTime(startDate.AddDays(i));
            var isCurrentMonth = cellDate.Month == currentMonthDate.Month;
            
            // Filter occurrences for this specific day
            var dayOccurrences = Occurrences.Where(x => x.PlannedDate == cellDate).ToList();
            
            daysList.Add(new CalendarDayCell
            {
                Date = cellDate,
                IsCurrentMonth = isCurrentMonth,
                Occurrences = dayOccurrences
            });
        }

        CalendarDays = new ObservableCollection<CalendarDayCell>(daysList);
    }

    [RelayCommand]
    public async Task NextMonthAsync()
    {
        currentMonthDate = currentMonthDate.AddMonths(1);
        await LoadAsync();
    }

    [RelayCommand]
    public async Task PreviousMonthAsync()
    {
        currentMonthDate = currentMonthDate.AddMonths(-1);
        await LoadAsync();
    }

    [RelayCommand]
    public async Task TodayAsync()
    {
        currentMonthDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        await LoadAsync();
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
