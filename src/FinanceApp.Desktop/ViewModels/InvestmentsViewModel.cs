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
    [ObservableProperty] private Windows.UI.Color trendColor = Windows.UI.Color.FromArgb(255, 5, 150, 105); // Emerald green default
    [ObservableProperty] private Microsoft.UI.Xaml.Media.Brush trendBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 5, 150, 105));

    [ObservableProperty] private bool isWatchlistMode;
    [ObservableProperty] private bool showKpiCards = true;
    [ObservableProperty] private string? selectedCategory = "Todas";
    [ObservableProperty] private InvestmentDto? selectedFilterInvestment;
    [ObservableProperty] private ObservableCollection<InvestmentDto> filterInvestments = [];
    
    [ObservableProperty] private string selectedRange = "1M";
    [ObservableProperty] private string selectedDrill = "Dia";
    [ObservableProperty] private bool preferCandlestick;
    [ObservableProperty] private bool hasSelectedAsset;
    [ObservableProperty] private string selectedBenchmark = "Nenhum";
    [ObservableProperty] private ObservableCollection<ChartDataPoint> benchmarkHistoryPoints = [];
    
    [ObservableProperty] private bool isRebalanceMode;
    [ObservableProperty] private double aporteValue = 5000;
    
    public ObservableCollection<RebalanceItem> RebalanceItems { get; } = [];
    public ObservableCollection<ActionPlanItem> ActionPlanItems { get; } = [];
    
    [ObservableProperty] private string patrimonioAtualText = "R$ 0,00";
    [ObservableProperty] private string patrimonioProjetadoText = "R$ 0,00";
    [ObservableProperty] private double rendaFixaPercentage;
    [ObservableProperty] private double acoesBrPercentage;
    [ObservableProperty] private double acoesEuaPercentage;
    [ObservableProperty] private double fiisPercentage;

    public Microsoft.UI.Xaml.GridLength RendaFixaWidth => new Microsoft.UI.Xaml.GridLength(RendaFixaPercentage > 0 ? RendaFixaPercentage : 0.01, Microsoft.UI.Xaml.GridUnitType.Star);
    public Microsoft.UI.Xaml.GridLength AcoesBrWidth => new Microsoft.UI.Xaml.GridLength(AcoesBrPercentage > 0 ? AcoesBrPercentage : 0.01, Microsoft.UI.Xaml.GridUnitType.Star);
    public Microsoft.UI.Xaml.GridLength AcoesEuaWidth => new Microsoft.UI.Xaml.GridLength(AcoesEuaPercentage > 0 ? AcoesEuaPercentage : 0.01, Microsoft.UI.Xaml.GridUnitType.Star);
    public Microsoft.UI.Xaml.GridLength FiisWidth => new Microsoft.UI.Xaml.GridLength(FiisPercentage > 0 ? FiisPercentage : 0.01, Microsoft.UI.Xaml.GridUnitType.Star);
    
    private List<CandlestickDataPoint> _rawCandlestickPoints = [];
    private bool _ignoreDrillChange;

    public ObservableCollection<string> Categories { get; } = ["Todas", "Ações", "FIIs", "CDB", "Tesouro Direto", "Cripto", "Outros Fundos"];
    public ObservableCollection<string> Benchmarks { get; } = ["Nenhum", "Inflação (IPCA)", "CDI", "Ibovespa", "Dólar", "Poupança"];

    public InvestmentDto? PinnedInvestment => Investments?.FirstOrDefault(x => x.IsPinned);
    public bool HasPinnedInvestment => PinnedInvestment != null;

    partial void OnIsWatchlistModeChanged(bool value)
    {
        ShowKpiCards = !value;
        
        // Reset selections to avoid cross-contamination between tabs
        SelectedInvestment = null;
        SelectedFilterInvestment = null;
        SelectedCategory = "Todas";
        SelectedBenchmark = "Nenhum";
        HasSelectedAsset = false;

        _ = LoadAsync();
    }

    partial void OnIsRebalanceModeChanged(bool value)
    {
        if (value)
        {
            LoadRebalanceData();
        }
    }

    partial void OnAporteValueChanged(double value)
    {
        RecalculateRebalance();
    }

    partial void OnSelectedCategoryChanged(string? value)
    {
        _ = LoadHistoryAsync();
    }

    partial void OnSelectedFilterInvestmentChanged(InvestmentDto? value)
    {
        _ = LoadHistoryAsync();
    }

    partial void OnSelectedRangeChanged(string value)
    {
        // Auto default drill level based on selected range
        _ignoreDrillChange = true;
        SelectedDrill = value switch
        {
            "6M" => "Semana",
            "1Y" => "Mês",
            "5Y" => "Trimestre",
            "1M" or _ => "Dia"
        };
        _ignoreDrillChange = false;

        AssetBentoCard.SetGlobalPeriod(value, SelectedDrill);

        if (SelectedInvestment != null)
        {
            _ = LoadCandlesticksAsync(SelectedInvestment.Id);
        }
        else
        {
            _ = LoadHistoryAsync();
        }
    }

    partial void OnSelectedDrillChanged(string value)
    {
        if (!_ignoreDrillChange)
        {
            ApplyDrillAggregation();
            AssetBentoCard.SetGlobalPeriod(SelectedRange, SelectedDrill);
        }
    }

    partial void OnPreferCandlestickChanged(bool value)
    {
        ShowCandlestickChart = value;
        if (!value && CandlestickPoints.Count > 0)
        {
            // Switch to line view: populate HistoryPoints from candle close prices
            HistoryPoints = new ObservableCollection<ChartDataPoint>(
                CandlestickPoints.Select(x => new ChartDataPoint(x.Date.ToString("dd/MM"), x.Close))
            );
        }

        // Notify all bento cards of the global chart mode change
        AssetBentoCard.SetGlobalChartMode(value);
    }

    partial void OnSelectedInvestmentChanged(InvestmentDto? value)
    {
        HasSelectedAsset = value != null;
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
            _ = LoadHistoryAsync(); // Reload consolidated history when clearing selection
        }
    }

    private async Task LoadCandlesticksAsync(Guid id)
    {
        try
        {
            IsBusy = true;
            DateOnly to = DateOnly.FromDateTime(DateTime.Today);
            DateOnly from = SelectedRange switch
            {
                "6M" => to.AddMonths(-6),
                "1Y" => to.AddYears(-1),
                "5Y" => to.AddYears(-5),
                "1M" or _ => to.AddMonths(-1)
            };

            var points = await apiClient.GetInvestmentCandlesticksAsync(id, from, to);
            _rawCandlestickPoints = points.Select(x => new CandlestickDataPoint(
                x.Date, 
                (double)x.Open, 
                (double)x.High, 
                (double)x.Low, 
                (double)x.Close, 
                x.Volume
            )).ToList();
            ApplyDrillAggregation();
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

    private async Task LoadConsolidatedCandlesticksAsync(bool isWatchlist)
    {
        IsBusy = true;
        try
        {
            DateOnly to = DateOnly.FromDateTime(DateTime.Today);
            DateOnly from = SelectedRange switch
            {
                "6M" => to.AddMonths(-6),
                "1Y" => to.AddYears(-1),
                "5Y" => to.AddYears(-5),
                "1M" or _ => to.AddMonths(-1)
            };

            var assets = await apiClient.GetInvestmentsAsync(isWatchlist: isWatchlist);

            // Filter by category
            if (SelectedCategory != "Todas" && !string.IsNullOrWhiteSpace(SelectedCategory))
            {
                var tag = MapCategoryTag(SelectedCategory);
                assets = assets.Where(x => x.AssetType.Equals(tag, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Filter by investment
            if (SelectedFilterInvestment != null && SelectedFilterInvestment.Id != Guid.Empty)
            {
                assets = assets.Where(x => x.Id == SelectedFilterInvestment.Id).ToList();
            }

            var dateMap = new Dictionary<DateOnly, List<CandlestickPointDto>>();

            foreach (var asset in assets)
            {
                try
                {
                    var candles = await apiClient.GetInvestmentCandlesticksAsync(asset.Id, from, to);
                    foreach (var c in candles)
                    {
                        if (!dateMap.TryGetValue(c.Date, out var list))
                        {
                            list = [];
                            dateMap[c.Date] = list;
                        }
                        list.Add(c);
                    }
                }
                catch
                {
                    // Skip asset if it fails to load
                }
            }

            if (dateMap.Count == 0)
            {
                _rawCandlestickPoints.Clear();
                ApplyDrillAggregation();
                return;
            }

            var aggregatedCandles = new List<CandlestickDataPoint>();

            foreach (var kvp in dateMap.OrderBy(x => x.Key))
            {
                var date = kvp.Key;
                var list = kvp.Value;

                double open = (double)list.Sum(x => x.Open);
                double high = (double)list.Sum(x => x.High);
                double low = (double)list.Sum(x => x.Low);
                double close = (double)list.Sum(x => x.Close);
                long volume = list.Sum(x => x.Volume);

                aggregatedCandles.Add(new CandlestickDataPoint(date, open, high, low, close, volume));
            }

            _rawCandlestickPoints = aggregatedCandles;
            ApplyDrillAggregation();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyDrillAggregation()
    {
        if (_rawCandlestickPoints.Count == 0)
        {
            CandlestickPoints.Clear();
            ShowCandlestickChart = false;
            return;
        }

        List<CandlestickDataPoint> aggregated;

        switch (SelectedDrill)
        {
            case "Semana":
                aggregated = AggregateByWeek(_rawCandlestickPoints);
                break;
            case "Mês":
                aggregated = AggregateByMonth(_rawCandlestickPoints);
                break;
            case "Trimestre":
                aggregated = AggregateByQuarter(_rawCandlestickPoints);
                break;
            case "Ano":
                aggregated = AggregateByYear(_rawCandlestickPoints);
                break;
            case "Dia":
            default:
                aggregated = _rawCandlestickPoints;
                break;
        }

        CandlestickPoints = new ObservableCollection<CandlestickDataPoint>(aggregated);

        // Populate history points from candle close prices
        HistoryPoints = new ObservableCollection<ChartDataPoint>(
            aggregated.Select(x => new ChartDataPoint(x.Date.ToString("dd/MM"), x.Close))
        );

        ShowCandlestickChart = PreferCandlestick;
        UpdateTrendColor();
        _ = UpdateBenchmarkLineAsync();
    }

    private void UpdateTrendColor()
    {
        if (HistoryPoints.Count >= 2)
        {
            double first = HistoryPoints.First().Value;
            double last = HistoryPoints.Last().Value;
            if (last >= first)
            {
                TrendColor = Windows.UI.Color.FromArgb(255, 5, 150, 105);   // Emerald green
                TrendBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(TrendColor);
            }
            else
            {
                TrendColor = Windows.UI.Color.FromArgb(255, 225, 29, 72);  // Rose red
                TrendBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(TrendColor);
            }
        }
        else
        {
            TrendColor = Windows.UI.Color.FromArgb(255, 5, 150, 105); // Default green
            TrendBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(TrendColor);
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
            var sortedItems = items.OrderByDescending(x => x.IsPinned).ToList();
            Investments = new ObservableCollection<InvestmentDto>(sortedItems);

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
            OnPropertyChanged(nameof(PinnedInvestment));
            OnPropertyChanged(nameof(HasPinnedInvestment));

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
            if (SelectedInvestment == null)
            {
                // Consolidated view (no selected asset):
                // Aggregate candlesticks for either real portfolio or watchlist
                await LoadConsolidatedCandlesticksAsync(isWatchlist: IsWatchlistMode);
            }
            else
            {
                // Selected asset view:
                await LoadCandlesticksAsync(SelectedInvestment.Id);
            }
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

    [RelayCommand]
    public async Task TogglePinAsync(Guid id)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await apiClient.TogglePinInvestmentAsync(id);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Erro ao alternar o destaque do ativo: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedBenchmarkChanged(string value)
    {
        _ = UpdateBenchmarkLineAsync();
    }

    private async Task UpdateBenchmarkLineAsync()
    {
        if (SelectedBenchmark == "Nenhum" || HistoryPoints.Count == 0 || CandlestickPoints.Count == 0)
        {
            BenchmarkHistoryPoints = [];
            return;
        }
 
        double startValue = HistoryPoints.First().Value;
        var points = new List<ChartDataPoint>();
 
        if (SelectedBenchmark == "CDI" || SelectedBenchmark == "Inflação (IPCA)" || SelectedBenchmark == "Poupança")
        {
            double annualRate = SelectedBenchmark switch
            {
                "CDI" => 0.105,
                "Inflação (IPCA)" => 0.045,
                "Poupança" or _ => 0.065
            };
 
            double dailyFactor = Math.Pow(1.0 + annualRate, 1.0 / 365.0);
            var startDate = CandlestickPoints[0].Date;
 
            for (int i = 0; i < CandlestickPoints.Count; i++)
            {
                var pt = CandlestickPoints[i];
                int days = pt.Date.DayNumber - startDate.DayNumber;
                double value = startValue * Math.Pow(dailyFactor, days);
                points.Add(new ChartDataPoint(pt.Date.ToString("dd/MM"), value));
            }
        }
        else if (SelectedBenchmark == "Ibovespa" || SelectedBenchmark == "Dólar")
        {
            try
            {
                var ticker = SelectedBenchmark == "Ibovespa" ? "^BVSP" : "USDBRL=X";
                var startDate = CandlestickPoints[0].Date;
                var endDate = CandlestickPoints.Last().Date;
 
                var benchmarkCandles = await apiClient.GetBenchmarkCandlesticksAsync(ticker, startDate, endDate);
                if (benchmarkCandles.Count == 0)
                {
                    BenchmarkHistoryPoints = [];
                    return;
                }
 
                var priceMap = benchmarkCandles.ToDictionary(c => c.Date, c => (double)c.Close);
 
                double GetLastKnownPrice(DateOnly d)
                {
                    var availableDates = priceMap.Keys.Where(x => x <= d).OrderByDescending(x => x).ToList();
                    if (availableDates.Count > 0)
                    {
                        return priceMap[availableDates[0]];
                    }
                    return priceMap.OrderBy(x => x.Key).First().Value;
                }
 
                double p0 = GetLastKnownPrice(startDate);
                if (p0 <= 0) p0 = 1.0;
 
                for (int i = 0; i < CandlestickPoints.Count; i++)
                {
                    var pt = CandlestickPoints[i];
                    double pi = GetLastKnownPrice(pt.Date);
                    double value = startValue * (pi / p0);
                    points.Add(new ChartDataPoint(pt.Date.ToString("dd/MM"), value));
                }
            }
            catch (Exception ex)
            {
                infoBarService.Error($"Erro ao carregar benchmark {SelectedBenchmark}: {ex.Message}");
                BenchmarkHistoryPoints = [];
                return;
            }
        }
 
        BenchmarkHistoryPoints = new ObservableCollection<ChartDataPoint>(points);
    }

    public static List<CandlestickDataPoint> AggregateByWeek(List<CandlestickDataPoint> raw)
    {
        var systemCalendar = System.Globalization.CultureInfo.InvariantCulture.Calendar;
        return raw
            .GroupBy(p => {
                var dt = p.Date.ToDateTime(TimeOnly.MinValue);
                int week = systemCalendar.GetWeekOfYear(dt, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
                return (Year: dt.Year, Week: week);
            })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Week)
            .Select(g => {
                var list = g.OrderBy(p => p.Date).ToList();
                double open = list.First().Open;
                double close = list.Last().Close;
                double high = list.Max(p => p.High);
                double low = list.Min(p => p.Low);
                long volume = list.Sum(p => p.Volume);
                DateOnly date = list.First().Date;
                return new CandlestickDataPoint(date, open, high, low, close, volume);
            })
            .ToList();
    }

    public static List<CandlestickDataPoint> AggregateByMonth(List<CandlestickDataPoint> raw)
    {
        return raw
            .GroupBy(p => (p.Date.Year, p.Date.Month))
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => {
                var list = g.OrderBy(p => p.Date).ToList();
                double open = list.First().Open;
                double close = list.Last().Close;
                double high = list.Max(p => p.High);
                double low = list.Min(p => p.Low);
                long volume = list.Sum(p => p.Volume);
                DateOnly date = new DateOnly(g.Key.Year, g.Key.Month, 1);
                return new CandlestickDataPoint(date, open, high, low, close, volume);
            })
            .ToList();
    }

    public static List<CandlestickDataPoint> AggregateByQuarter(List<CandlestickDataPoint> raw)
    {
        return raw
            .GroupBy(p => (p.Date.Year, Quarter: (p.Date.Month - 1) / 3 + 1))
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Quarter)
            .Select(g => {
                var list = g.OrderBy(p => p.Date).ToList();
                double open = list.First().Open;
                double close = list.Last().Close;
                double high = list.Max(p2 => p2.High);
                double low = list.Min(p2 => p2.Low);
                long volume = list.Sum(p => p.Volume);
                DateOnly date = new DateOnly(g.Key.Year, (g.Key.Quarter - 1) * 3 + 1, 1);
                return new CandlestickDataPoint(date, open, high, low, close, volume);
            })
            .ToList();
    }

    public static List<CandlestickDataPoint> AggregateByYear(List<CandlestickDataPoint> raw)
    {
        return raw
            .GroupBy(p => p.Date.Year)
            .OrderBy(g => g.Key)
            .Select(g => {
                var list = g.OrderBy(p => p.Date).ToList();
                double open = list.First().Open;
                double close = list.Last().Close;
                double high = list.Max(p => p.High);
                double low = list.Min(p => p.Low);
                long volume = list.Sum(p => p.Volume);
                DateOnly date = new DateOnly(g.Key, 1, 1);
                return new CandlestickDataPoint(date, open, high, low, close, volume);
            })
            .ToList();
     }

    public void LoadRebalanceData()
    {
        // Fetch user investments and group them
        decimal rfVal = Investments.Where(x => x.AssetType == "cdb" || x.AssetType == "tesouro" || x.AssetType == "Renda Fixa").Sum(x => x.CurrentValue);
        decimal acoesBrVal = Investments.Where(x => x.AssetType == "stock" || x.AssetType == "Ações BR" || x.AssetType == "Ações").Sum(x => x.CurrentValue);
        decimal acoesEuaVal = Investments.Where(x => x.AssetType == "fund" || x.AssetType == "Ações EUA" || x.AssetType == "Internacional").Sum(x => x.CurrentValue);
        decimal fiisVal = Investments.Where(x => x.AssetType == "fii" || x.AssetType == "FIIs" || x.AssetType == "FII").Sum(x => x.CurrentValue);

        // Fallback demo values if portfolio is empty or not yet seeded
        if (rfVal == 0 && acoesBrVal == 0 && acoesEuaVal == 0 && fiisVal == 0)
        {
            rfVal = 35000m;
            acoesBrVal = 42000m;
            acoesEuaVal = 8000m;
            fiisVal = 5000m;
        }

        RebalanceItems.Clear();
        RebalanceItems.Add(new RebalanceItem { CategoryName = "Renda Fixa", CurrentValue = rfVal, TargetPercentage = 30, OnTargetChanged = RecalculateRebalance });
        RebalanceItems.Add(new RebalanceItem { CategoryName = "Ações BR", CurrentValue = acoesBrVal, TargetPercentage = 40, OnTargetChanged = RecalculateRebalance });
        RebalanceItems.Add(new RebalanceItem { CategoryName = "Ações EUA", CurrentValue = acoesEuaVal, TargetPercentage = 20, OnTargetChanged = RecalculateRebalance });
        RebalanceItems.Add(new RebalanceItem { CategoryName = "FIIs", CurrentValue = fiisVal, TargetPercentage = 10, OnTargetChanged = RecalculateRebalance });

        RecalculateRebalance();
    }

    public void RecalculateRebalance()
    {
        decimal totalCurrent = RebalanceItems.Sum(x => x.CurrentValue);
        PatrimonioAtualText = $"R$ {totalCurrent:N2}";
        
        decimal projected = totalCurrent + (decimal)AporteValue;
        PatrimonioProjetadoText = $"R$ {projected:N2}";

        // Calculate current percentages
        foreach (var item in RebalanceItems)
        {
            item.CurrentPercentage = totalCurrent > 0 ? (double)((item.CurrentValue / totalCurrent) * 100) : 0;
            item.Refresh();
        }

        // Greedy allocation of Aporte
        var tempAllocations = RebalanceItems.ToDictionary(x => x.ClassName, x => 0m);
        decimal remainingAporte = (decimal)AporteValue;

        if (remainingAporte > 0)
        {
            while (remainingAporte > 0.01m)
            {
                var targetItem = RebalanceItems
                    .Select(x => new {
                        Item = x,
                        Deficit = (projected * (decimal)(x.TargetPercentage / 100.0)) - (x.CurrentValue + tempAllocations[x.ClassName])
                    })
                    .Where(x => x.Deficit > 0)
                    .OrderByDescending(x => x.Deficit)
                    .FirstOrDefault();

                if (targetItem == null)
                {
                    // Satisfied or total targets < 100%
                    var fallback = RebalanceItems.OrderByDescending(x => x.TargetPercentage).FirstOrDefault();
                    if (fallback != null)
                    {
                        tempAllocations[fallback.ClassName] += remainingAporte;
                    }
                    break;
                }

                decimal toAllocate = Math.Min(remainingAporte, Math.Max(0.01m, targetItem.Deficit));
                tempAllocations[targetItem.Item.ClassName] += toAllocate;
                remainingAporte -= toAllocate;
            }
        }

        // Build ActionPlanItems
        ActionPlanItems.Clear();
        
        var greenBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 185, 129));
        var roseBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 244, 63, 94));
        var greyBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 156, 163, 175));
        
        var greenBg = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(25, 16, 185, 129));
        var roseBg = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(25, 244, 63, 94));
        var greyBg = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(25, 156, 163, 175));

        // Sort items by deficit distance (furthest first) to show in ActionPlan
        var sortedDeficits = RebalanceItems
            .Select(x => new {
                Item = x,
                Deficit = (projected * (decimal)(x.TargetPercentage / 100.0)) - x.CurrentValue,
                Allocated = tempAllocations[x.ClassName]
            })
            .OrderByDescending(x => x.Deficit)
            .ToList();

        // 1. Aportar
        foreach (var d in sortedDeficits)
        {
            if (d.Allocated > 0)
            {
                ActionPlanItems.Add(new ActionPlanItem
                {
                    ClassName = d.Item.ClassName,
                    ActionType = "APORTAR",
                    Value = d.Allocated,
                    ActionColor = greenBrush,
                    BackgroundBrush = greenBg
                });
            }
        }

        // 2. Falta Investir
        foreach (var d in sortedDeficits)
        {
            decimal remainingDeficit = d.Deficit - d.Allocated;
            if (remainingDeficit > 0.01m)
            {
                ActionPlanItems.Add(new ActionPlanItem
                {
                    ClassName = d.Item.ClassName,
                    ActionType = "FALTA INVESTIR",
                    Value = remainingDeficit,
                    ActionColor = greyBrush,
                    BackgroundBrush = greyBg
                });
            }
        }

        // 3. Excesso
        foreach (var d in sortedDeficits)
        {
            if (d.Deficit < -0.01m)
            {
                ActionPlanItems.Add(new ActionPlanItem
                {
                    ClassName = d.Item.ClassName,
                    ActionType = "EXCESSO",
                    Value = -d.Deficit,
                    ActionColor = roseBrush,
                    BackgroundBrush = roseBg
                });
            }
        }
        
        // Update percentages for UI
        RendaFixaPercentage = RebalanceItems.FirstOrDefault(x => x.ClassName == "Renda Fixa")?.CurrentPercentage ?? 0;
        AcoesBrPercentage = RebalanceItems.FirstOrDefault(x => x.ClassName == "Ações BR")?.CurrentPercentage ?? 0;
        AcoesEuaPercentage = RebalanceItems.FirstOrDefault(x => x.ClassName == "Ações EUA")?.CurrentPercentage ?? 0;
        RendaFixaPercentage = RebalanceItems.FirstOrDefault(x => x.CategoryName == "Renda Fixa")?.CurrentPercentage ?? 0;
        AcoesBrPercentage = RebalanceItems.FirstOrDefault(x => x.CategoryName == "Ações BR")?.CurrentPercentage ?? 0;
        AcoesEuaPercentage = RebalanceItems.FirstOrDefault(x => x.CategoryName == "Ações EUA")?.CurrentPercentage ?? 0;
        FiisPercentage = RebalanceItems.FirstOrDefault(x => x.CategoryName == "FIIs")?.CurrentPercentage ?? 0;

        OnPropertyChanged(nameof(RendaFixaWidth));
        OnPropertyChanged(nameof(AcoesBrWidth));
        OnPropertyChanged(nameof(AcoesEuaWidth));
        OnPropertyChanged(nameof(FiisWidth));
    }
}

public class RebalanceItem : ObservableObject
{
    public string CategoryName { get; set; } = string.Empty;
    public string ClassName { get => CategoryName; set => CategoryName = value; }
    public decimal CurrentValue { get; set; }
    public string CurrentValueText => $"R$ {CurrentValue:N2}";
    public double CurrentPercentage { get; set; }
    public string CurrentPercentageText => $"{CurrentPercentage:N1}%";
    public string TargetPercentageText => $"{TargetPercentage:N1}%";
    public decimal Difference { get; set; }
    public string DifferenceText => Difference >= 0 ? $"+R$ {Difference:N2}" : $"-R$ {Math.Abs(Difference):N2}";
    public Microsoft.UI.Xaml.Media.Brush DifferenceBrush => Difference >= 0 
        ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 5, 150, 105))
        : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 225, 29, 72));

    public string ActionText => Difference > 50 ? "APORTAR" : (Difference < -50 ? "VENDER" : "OK");
    public Microsoft.UI.Xaml.Media.Brush ActionBadgeBrush => Difference > 50
        ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 5, 150, 105))
        : (Difference < -50 
            ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 225, 29, 72))
            : new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(150, 100, 100, 100)));

    private double _targetPercentage;
    public double TargetPercentage
    {
        get => _targetPercentage;
        set
        {
            if (SetProperty(ref _targetPercentage, value))
            {
                OnTargetChanged?.Invoke();
            }
        }
    }
    
    public Action? OnTargetChanged { get; set; }

    public void Refresh()
    {
        OnPropertyChanged(nameof(CurrentValueText));
        OnPropertyChanged(nameof(CurrentPercentageText));
    }
}

public class ActionPlanItem
{
    public string ClassName { get; set; } = string.Empty;
    public string ActionType { get; set; } = "APORTAR";
    public decimal Value { get; set; }
    public string ValueText => $"R$ {Value:N2}";
    public Microsoft.UI.Xaml.Media.Brush? ActionColor { get; set; }
    public Microsoft.UI.Xaml.Media.Brush? BackgroundBrush { get; set; }
}
