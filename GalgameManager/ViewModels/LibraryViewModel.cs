using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Collections;
using GalgameManager.Contracts;
using GalgameManager.Contracts.Services;
using GalgameManager.Contracts.ViewModels;
using GalgameManager.Helpers;
using GalgameManager.Models;
using GalgameManager.Models.BgTasks;
using GalgameManager.Models.Sources;
using GalgameManager.Services;
using GalgameManager.Views.Dialog;
using Microsoft.UI.Xaml.Controls;

namespace GalgameManager.ViewModels;

public partial class LibraryViewModel(
    INavigationService navigationService,
    IGalgameSourceCollectionService galSourceService,
    IInfoService infoService,
    IBgTaskService bgTaskService,
    IGalgameCollectionService galgameService
    )
    : ObservableObject, INavigationAware
{
    private readonly GalgameSourceCollectionService _galSourceCollectionService = (GalgameSourceCollectionService)galSourceService;
    private readonly IGalgameCollectionService _galgameService = galgameService;
    private readonly IBgTaskService _bgTaskService = bgTaskService;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsBackEnabled))]
    private GalgameSourceBase? _currentSource;
    private GalgameSourceBase? _lastBackSource;
    private static GalgameSourceBase? _beforeNavigateFromSource; //用于从该页跳转到Galgame详情界面后返回时直接回到某个库的界面

    [ObservableProperty]
    private AdvancedCollectionView _source = null!;
    public AdvancedCollectionView Galgames = new(new ObservableCollection<Galgame>());

    #region UI

    public readonly string UiSearch = "Search".GetLocalized();
    public bool IsBackEnabled => CurrentSource != null;
    [ObservableProperty] private bool _sourceVisible;
    [ObservableProperty] private bool _galgamesVisible;
    [ObservableProperty] private bool _isPhrasing;
    [ObservableProperty]
    private ObservableCollection<GalgameSourceBase> _pathNodes = new();

    #endregion

    #region SERACH

    [ObservableProperty] private string _searchTitle = "Search".GetLocalized();
    [ObservableProperty] private string _searchKey = "";
    [ObservableProperty] private ObservableCollection<string> _searchSuggestions = new();
    [ObservableProperty] private bool _updateGridSpacing;

    [RelayCommand]
    private void Search(string searchKey)
    {
        SearchTitle = searchKey == string.Empty ? UiSearch : UiSearch + " ●";
        Source.RefreshFilter();
    }

    #endregion

    [ObservableProperty]
    private string _statisticsText = string.Empty;

    private void UpdateStatistics()
    {
        var sourceCount = Source.Count;
        var galgameCount = Galgames.Count;
        StatisticsText = string.Format("LibraryPage_Statistics".GetLocalized(), sourceCount, galgameCount);
    }

    public void OnNavigatedTo(object parameter)
    {
        Source = new AdvancedCollectionView(new ObservableCollection<IDisplayableGameObject>(), true);
        Source.Filter = s =>
        {
            if (s is GalgameSourceBase source)
                return SearchKey.IsNullOrEmpty() || source.ApplySearchKey(SearchKey);
            if (s is Galgame game)
                return SearchKey.IsNullOrEmpty() || game.ApplySearchKey(SearchKey);
            return false;
        };
        if (_beforeNavigateFromSource is not null) parameter = _beforeNavigateFromSource;
        NavigateTo(parameter as GalgameSourceBase); //显示根库 / 指定库
        _beforeNavigateFromSource = null;
        _galSourceCollectionService.OnSourceChanged += HandleSourceCollectionChanged;
    }

    public void OnNavigatedFrom()
    {
        _galSourceCollectionService.OnSourceChanged -= HandleSourceCollectionChanged;
        _lastBackSource = CurrentSource = null;
    }

    private void HandleSourceCollectionChanged()
    {
        CurrentSource = _lastBackSource = null;
        NavigateTo(null);
    }

    /// <summary>
    /// 点击了某个库（若clickItem为null则显示所有根库）<br/>
    /// 若这个库有子库，保持在LibraryViewModel界面，否则以库为Filter进入主界面
    /// </summary>
    [RelayCommand]
    private void NavigateTo(IDisplayableGameObject? clickedItem)
    {
        UpdateGridSpacing = false;
        Source.Clear();
        Galgames.Clear();
        if (clickedItem == null)
        {
            foreach (GalgameSourceBase src in _galSourceCollectionService.GetGalgameSources()
                         .Where(s => s.ParentSource is null))
                Source.Add(src);
        }

        if (clickedItem is Galgame galgame)
        {
            _beforeNavigateFromSource = CurrentSource;
            navigationService.NavigateTo(typeof(GalgameViewModel).FullName!,
                new GalgamePageParameter { Galgame = galgame });
        }
        else if (clickedItem is GalgameSourceBase source)
        {
            if (source.SubSources.Count > 0)
            {
                foreach (GalgameSourceBase src in _galSourceCollectionService.GetGalgameSources()
                             .Where(s => s.ParentSource == clickedItem))
                    Source.Add(src);
                foreach (GalgameAndPath game in source.Galgames)
                    Galgames.Add(game.Galgame);
            }
            else
            {
                // _filterService.ClearFilters();
                // _filterService.AddFilter(new SourceFilter(source));
                // _navigationService.NavigateTo(typeof(HomeViewModel).FullName!);
                foreach (GalgameAndPath game in source.Galgames)
                    Galgames.Add(game.Galgame);
            }

            CurrentSource = source;
        }
        else if (clickedItem is null)
            CurrentSource = null;
        UpdateGridSpacing = true;
        SourceVisible = Source.Count > 0;
        GalgamesVisible = Galgames.Count > 0;
        UpdateStatistics();

        // 更新路径节点
        PathNodes.Clear();
        if (clickedItem is GalgameSourceBase newSource)
        {
            var currentSource = newSource;
            var nodes = new List<GalgameSourceBase>();
            while (currentSource != null)
            {
                nodes.Insert(0, currentSource);
                currentSource = currentSource.ParentSource;
            }
            foreach (var node in nodes)
            {
                PathNodes.Add(node);
            }
        }
    }
    

    [RelayCommand]
    private void Back()
    {
        if (CurrentSource is null) return;
        _lastBackSource = CurrentSource;
        NavigateTo(CurrentSource.ParentSource);
    }

    [RelayCommand]
    private void Forward()
    {
        if (_lastBackSource is null || _lastBackSource == CurrentSource) return;
        NavigateTo(_lastBackSource);
    }

    [RelayCommand]
    private void GetInfoFromRss()
    {
        List<GalgameSourceBase> sources = new();
        if (CurrentSource is null)
        {
            // 获取所有根库
            sources.AddRange(_galSourceCollectionService.GetGalgameSources());
        }
        else
        {
            // 获取当前库及其所有子库
            sources.Add(CurrentSource);
            var allSources = _galSourceCollectionService.GetGalgameSources();
            void AddSubSources(GalgameSourceBase parent)
            {
                foreach (var source in allSources.Where(s => s.ParentSource == parent))
                {
                    sources.Add(source);
                    AddSubSources(source);
                }
            }
            AddSubSources(CurrentSource);
        }

        // 对于这个列表，每个库都创建一个GetGalgameInfoFromRssTask，并加入到BgTaskService中
        foreach (GalgameSourceBase source in sources)
        {
            var getGalgameInfoFromRss = new GetGalgameInfoFromRssTask(source);
            getGalgameInfoFromRss.OnProgress += progress =>
            {
                infoService.Info(progress.ToSeverity(), msg: progress.Message,
                    displayTimeMs: progress.ToSeverity() switch
                    {
                        InfoBarSeverity.Informational => 300000,
                        _ => 3000
                    });
            };
            _ = _bgTaskService.AddBgTask(getGalgameInfoFromRss);
        }
    }

    [RelayCommand]
    private async Task AddLibrary()
    {
        try
        {
            AddSourceDialog dialog = new()
            {
                XamlRoot = App.MainWindow!.Content.XamlRoot,
            };
            await dialog.ShowAsync();
            if (dialog.Canceled) return;
            switch (dialog.SelectItem)
            {
                case 0:
                    await _galSourceCollectionService.AddGalgameSourceAsync(GalgameSourceType.LocalFolder, dialog.Path);
                    break;
                case 1:
                    await _galSourceCollectionService.AddGalgameSourceAsync(GalgameSourceType.LocalZip, dialog.Path);
                    break;
            }

        }
        catch (Exception e)
        {
            infoService.Info(InfoBarSeverity.Error, msg: e.Message);
        }
    }

    [RelayCommand]
    private void EditLibrary(GalgameSourceBase? source)
    {
        if (source is null) return;
        _beforeNavigateFromSource = CurrentSource;
        navigationService.NavigateTo(typeof(GalgameSourceViewModel).FullName!, source.Url);
    }

    [RelayCommand]
    private async Task DeleteFolder(GalgameSourceBase? galgameFolder)
    {
        if (galgameFolder is null) return;
        await _galSourceCollectionService.DeleteGalgameFolderAsync(galgameFolder);
    }

    [RelayCommand]
    private void ScanAll()
    {
        _galSourceCollectionService.ScanAll();
        infoService.Info(InfoBarSeverity.Success, msg: "LibraryPage_ScanAll_Success".GetLocalized(Source.Count));
    }

    [RelayCommand]
    private void EditCurrentFolder()
    {
        if (CurrentSource is null) return;
        _beforeNavigateFromSource = CurrentSource;
        navigationService.NavigateTo(typeof(GalgameSourceViewModel).FullName!, CurrentSource.Url);
    }

    [RelayCommand]
    private async Task GalFlyOutDelete(Galgame? galgame)
    {
        if(galgame == null) return;
        ContentDialog dialog = new()
        {
            XamlRoot = App.MainWindow!.Content.XamlRoot,
            Title = "HomePage_Remove_Title".GetLocalized(),
            Content = "HomePage_Remove_Message".GetLocalized(),
            PrimaryButtonText = "Yes".GetLocalized(),
            SecondaryButtonText = "Cancel".GetLocalized()
        };
        dialog.PrimaryButtonClick += async (_, _) =>
        {
            await _galgameService.RemoveGalgame(galgame);
        };
        
        await dialog.ShowAsync();

        // 删除游戏后，刷新当前库
        NavigateTo(CurrentSource);
    }

    [RelayCommand]
    private void GalFlyOutEdit(Galgame? galgame)
    {
        if(galgame == null) return;
        _beforeNavigateFromSource = CurrentSource;
        navigationService.NavigateTo(typeof(GalgameSettingViewModel).FullName!, galgame);
    }

    [RelayCommand]
    private async Task GalFlyOutGetInfoFromRss(Galgame? galgame)
    {
        if(galgame == null) return;
        IsPhrasing = true;
        await _galgameService.PhraseGalInfoAsync(galgame);
        IsPhrasing = false;
    }

    public void OnBreadcrumbBarItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        if (args.Item is GalgameSourceBase source)
        {
            NavigateTo(source);
        }
    }
}
