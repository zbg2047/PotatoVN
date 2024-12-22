using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GalgameManager.Contracts.Services;
using GalgameManager.Contracts.ViewModels;
using GalgameManager.Models;
using System.Collections.ObjectModel;
using SkiaSharp;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.Kernel.Sketches;

namespace GalgameManager.ViewModels;
public partial class ReportSubPage1ViewModel : ObservableObject, INavigationAware
{
    private readonly IGalgameCollectionService _galgameCollectionService;
    [ObservableProperty] private Galgame _game = null!;

    [ObservableProperty] private double _playedTime = 0;
    [ObservableProperty] private int _totalPlayHours = 0;
    [ObservableProperty] private int _totalGamesPlayed = 0;
    [ObservableProperty] private int _mostActiveMonth = 0;
    [ObservableProperty] private double _averagePlayHours = 0;
    [ObservableProperty] private bool _isPlayHoursDetailVisible;
    [ObservableProperty] private bool _isGamesPlayedDetailVisible;
    [ObservableProperty] private bool _isActiveMonthDetailVisible;
    [ObservableProperty] private bool _isAverageHoursDetailVisible;
    [ObservableProperty] private ObservableCollection<MonthlyData>? _monthlyPlayHours;

    [ObservableProperty]
    private ISeries[]? _playHoursSeries;
    [ObservableProperty]
    private ISeries[]? _gamesPlayedSeries;
    [ObservableProperty]
    private ISeries[]? _activeMonthSeries;
    [ObservableProperty]
    private ISeries[]? _averageHoursSeries;
    
    [ObservableProperty]
    private IEnumerable<ICartesianAxis>? _playHoursXAxes;
    [ObservableProperty]
    private IEnumerable<ICartesianAxis>? _monthXAxes;
    [ObservableProperty]
    private IEnumerable<ICartesianAxis>? _averageHoursXAxes;

    public ReportSubPage1ViewModel(IGalgameCollectionService galgameCollectionService)
    {
        _galgameCollectionService = galgameCollectionService;
        
        // 初始化基础数据
        Game = _galgameCollectionService.Galgames.FirstOrDefault() ?? new Galgame();

        PlayedTime = 20.2;
        TotalPlayHours = 100;
        TotalGamesPlayed = 10;
        MostActiveMonth = 1;
        AveragePlayHours = 10;
        
        // 初始化图表数据
        InitializeCharts();
    }

    private void InitializeCharts()
    {
        var months = new string[] { "1月", "2月", "3月", "4月", "5月", "6月", 
                                  "7月", "8月", "9月", "10月", "11月", "12月" };
        
        // 设置默认的图表样式
        var defaultLabelStyle = new SolidColorPaint
        {
            Color = SKColor.Parse("#303030"),
        
        };

        // 游玩时长柱状图
        PlayHoursSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Values = new double[] { 20, 35, 15, 25, 30, 20, 35, 15, 25, 30, 20, 35 },
                Name = "月度游玩时长",
                Fill = new SolidColorPaint(SKColor.Parse("#266489")),
                YToolTipLabelFormatter = point => $"{point.Model} 小时",
            }
        };
        
        
        PlayHoursXAxes = new[]
        {
            new Axis
            {
                Labels = months,
                LabelsRotation = 0,
                LabelsPaint = defaultLabelStyle
            }
        };

        // 游戏完成度饼图
        GamesPlayedSeries = new ISeries[]
        {
            new PieSeries<double>
            {
                Values = new double[] { 5 },
                Name = "已完成",
                Fill = new SolidColorPaint(SKColor.Parse("#2c3e50")),
#pragma warning disable CS0618 // 类型或成员已过时 假警报，PipeSeries根本没有XToolTipLabelFormatter或YToolTipLabelFormatter
                //see https://livecharts.dev/api/2.0.0-rc1/LiveChartsCore.PieSeries-5
                TooltipLabelFormatter = (point) => $"{point.Model:F0} 部",
#pragma warning restore CS0618 // 类型或成员已过时
            },
            new PieSeries<double>
            {
                Values = new double[] { 3 },
                Name = "进行中",
                Fill = new SolidColorPaint(SKColor.Parse("#77d065"))
            },
            new PieSeries<double>
            {
                Values = new double[] { 2 },
                Name = "未开始",
                Fill = new SolidColorPaint(SKColor.Parse("#b455b6"))
            }
        };

        // 月度活跃度折线图
        ActiveMonthSeries = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = new double[] { 5, 8, 10, 12, 15, 18, 20, 22, 25, 28, 30, 32 },
                Name = "月度活跃度",
                Fill = null,
                GeometrySize = 10,
                Stroke = new SolidColorPaint(SKColor.Parse("#68B9C0"), 3),
                GeometryStroke = new SolidColorPaint(SKColor.Parse("#68B9C0"), 3)
            }
        };
        
        MonthXAxes = new[]
        {
            new Axis
            {
                Labels = months,
                LabelsRotation = 0
            }
        };

        // 游戏时长分布条形图
        AverageHoursSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Values = new double[] { 3, 4, 2, 1 },
                Name = "游戏数量",
                Fill = new SolidColorPaint(SKColor.Parse("#266489"))
            }
        };

        AverageHoursXAxes = new[]
        {
            new Axis
            {
                Labels = new[] { "0-10h", "10-20h", "20-30h", "30h+" },
                LabelsRotation = 0
            }
        };
    }

    public void OnNavigatedTo(object parameter)
    {
        Game = _galgameCollectionService.Galgames[0];
    }

    public void OnNavigatedFrom()
    {

    }

    [RelayCommand]
    private void TogglePlayHoursDetail()
    {
        IsPlayHoursDetailVisible = !IsPlayHoursDetailVisible;

        IsGamesPlayedDetailVisible = false;
        IsActiveMonthDetailVisible = false;
        IsAverageHoursDetailVisible = false;
    }

    [RelayCommand]
    private void ToggleGamesPlayedDetail()
    {
        IsGamesPlayedDetailVisible = !IsGamesPlayedDetailVisible;

        IsPlayHoursDetailVisible = false;
        IsActiveMonthDetailVisible = false;
        IsAverageHoursDetailVisible = false;
    }

    [RelayCommand]
    private void ToggleActiveMonthDetail()
    {
        IsActiveMonthDetailVisible = !IsActiveMonthDetailVisible;

        IsPlayHoursDetailVisible = false;
        IsGamesPlayedDetailVisible = false;
        IsAverageHoursDetailVisible = false;
    }

    [RelayCommand]
    private void ToggleAverageHoursDetail()
    {
        IsAverageHoursDetailVisible = !IsAverageHoursDetailVisible;
        
        IsPlayHoursDetailVisible = false;
        IsGamesPlayedDetailVisible = false;
        IsActiveMonthDetailVisible = false;
    }
}

public class MonthlyData
{
    public string? Month { get; set; }
    public double Hours { get; set; }
}