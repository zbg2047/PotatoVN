using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.UI;
using GalgameManager.Contracts.Services;
using GalgameManager.Contracts.ViewModels;
using GalgameManager.Helpers;
using GalgameManager.Models;
using GalgameManager.Models.Filters;
using GalgameManager.Models.Sources;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using GalgameManager.MultiStreamPage.Lists;
using GalgameManager.ViewModels;
using SourceFilter = GalgameManager.Models.Filters.SourceFilter;

namespace GalgameManager.ViewModels
{
    public partial class MultiStreamViewModel : ObservableRecipient, INavigationAware
    {
        public ObservableCollection<object> Lists { get; } = new();

        private readonly IGalgameCollectionService _gameService;
        private readonly IGalgameSourceCollectionService _sourceService;
        private readonly ICategoryService _categoryService;
        private readonly INavigationService _navigationService;
        private readonly IFilterService _filterService;
        private readonly IInfoService _infoService;

        public MultiStreamViewModel(IGalgameCollectionService gameService, ICategoryService categoryService,
            INavigationService navigationService, IFilterService filterService, IInfoService infoService,
            IGalgameSourceCollectionService sourceService)
        {
            _gameService = gameService;
            _categoryService = categoryService;
            _navigationService = navigationService;
            _filterService = filterService;
            _sourceService = sourceService;
            _infoService = infoService;
        }


        public void OnNavigatedTo(object parameter)
        {
            // test only
            Lists.Add(new GameList(_gameService.Galgames, "最近游玩的游戏",
                MultiStreamPageSortKeys.LastPlayed));
            Lists.Add(new CategoryList(_categoryService.DeveloperGroup));
            foreach (Category c in _categoryService.StatusGroup.Categories)
                Lists.Add(new GameList(new ObservableCollection<Galgame>(c.GalgamesX), c.Name,
                    MultiStreamPageSortKeys.LastPlayed) { Category = c });
            Lists.Add(new SourceList(null));
            foreach(GalgameSourceBase source in _sourceService.GetGalgameSources())
                if (source.SubSources.Count > 0)
                {
                    Lists.Add(new SourceList(source));
                    break;
                }
        }

        public void OnNavigatedFrom()
        {
        }

        [RelayCommand]
        private void ClickGame(Galgame? clickedItem)
        {
            if (clickedItem == null) return;
            NavigationHelper.NavigateToGalgamePage(_navigationService,
                new GalgamePageParameter { Galgame = clickedItem });
        }

        [RelayCommand]
        private void ClickCategory(Category category)
        {
            NavigationHelper.NavigateToHomePage(_navigationService, _filterService,
                new[] { new CategoryFilter(category) });
        }

        [RelayCommand]
        private void ClickSource(GalgameSourceBase source)
        {
            NavigationHelper.NavigateToHomePage(_navigationService, _filterService, new[] { new SourceFilter(source) });
        }

        [RelayCommand]
        private async Task Setting()
        {
            Views.Dialog.MultiStreamPageSettingDialog dialog = new(Lists);
            ContentDialogResult status = await dialog.ShowAsync();
            if (status != ContentDialogResult.Primary) return;
            Lists.SyncCollection(dialog.Result);
        }

        #region SEARCH

        [ObservableProperty]
        private GalgameSearchSuggestionsProvider _galgameSearchSuggestionsProvider = new(true, true, false);
        [ObservableProperty] private string _searchKey = string.Empty;

        [RelayCommand]
        private void Search()
        {
            Galgame? game = _gameService.GetGalgameFromName(_searchKey);
            if (game is not null)
            {
                NavigationHelper.NavigateToGalgamePage(_navigationService, new GalgamePageParameter { Galgame = game });
                return;
            }
            Category? category = _categoryService.GetCategory(_searchKey);
            if (category is not null)
            {
                NavigationHelper.NavigateToHomePage(_navigationService, _filterService, new[] { new CategoryFilter(category) });
                return;
            }
            _infoService.Info(InfoBarSeverity.Error, msg:"MultiStreamPage_SearchFailed".GetLocalized());
        }

        #endregion
    }

    public class MultiStreamPageSelector : DataTemplateSelector
    {
        public DataTemplate GameListTemplate { get; set; } = null!;
        public DataTemplate CategoryListTemplate { get; set; } = null!;
        public DataTemplate SourceListTemplate { get; set; } = null!;

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            Debug.Assert(
                GameListTemplate is not null && CategoryListTemplate is not null && SourceListTemplate is not null,
                "Template is not set");
            return item switch
            {
                GameList => GameListTemplate,
                CategoryList => CategoryListTemplate,
                SourceList => SourceListTemplate,
                _ => base.SelectTemplateCore(item, container)
            };
        }
    }
}


# region OBSERVABLE_LISTS

namespace GalgameManager.MultiStreamPage.Lists
{
    public enum MultiStreamPageSortKeys
    {
        LastPlayed,
        LastClicked,
    }
    
    public partial class GameList : ObservableRecipient
    {
        public AdvancedCollectionView Games;
        public string Title;
        public MultiStreamPageSortKeys Sort;
        public Category? Category { get; init; } // 如果设置了则为某分类下的游戏列表
        public GalgameSourceBase? Source { get; init; } // 如果设置了则为某源下的游戏列表

        public GameList(ObservableCollection<Galgame> games, string title, MultiStreamPageSortKeys sort)
        {
            Games = new AdvancedCollectionView(games, true);
            Title = title;
            Sort = sort;

            Games.SortDescriptions.Add(new SortDescription(Sort switch
            {
                MultiStreamPageSortKeys.LastPlayed => nameof(Galgame.LastPlayTime),
                _ => throw new ArgumentOutOfRangeException()
            }, SortDirection.Descending));
        }

        [RelayCommand]
        private void ClickTitle()
        {
            INavigationService service = App.GetService<INavigationService>();
            if (Category is not null)
                NavigationHelper.NavigateToHomePage(service, App.GetService<IFilterService>(),
                    new[] { new CategoryFilter(Category) });
            else if (Source is not null)
                NavigationHelper.NavigateToHomePage(service, App.GetService<IFilterService>(),
                    new[] { new SourceFilter(Source) });
            else
                NavigationHelper.NavigateToHomePage(service);
        }
    }

    public partial class CategoryList : ObservableRecipient
    {
        public AdvancedCollectionView Categories;
        public string Title;
        public MultiStreamPageSortKeys Sort;
        private readonly CategoryGroup _group;

        public CategoryList(CategoryGroup group)
        {
            _group = group;
            Categories = new AdvancedCollectionView(group.Categories, true);
            Title = group.Name;
        }

        [RelayCommand]
        private void ClickTitle()
        {
            INavigationService service = App.GetService<INavigationService>();
            service.NavigateTo(typeof(CategoryViewModel).FullName!, _group);
        }
    }

    public partial class SourceList : ObservableRecipient
    {
        public AdvancedCollectionView Sources = new();
        public string Title;
        public MultiStreamPageSortKeys Sort;
        public GalgameSourceBase? Root;

        private readonly IGalgameSourceCollectionService _sourceService =
            App.GetService<IGalgameSourceCollectionService>();

        /// <param name="root">显示root内的游戏库，若设置为null则显示整个所有库</param>
        public SourceList(GalgameSourceBase? root)
        {
            Root = root;
            Title = Root?.Name ?? "MultiStreamPage_AllSources".GetLocalized();
            Sources.Source = new ObservableCollection<GalgameSourceBase>();
            if (root is null)
            {
                foreach (GalgameSourceBase source in _sourceService.GetGalgameSources())
                    Sources.Add(source);
            }
            else
            {
                foreach (GalgameSourceBase source in root.SubSources)
                    Sources.Add(source);
            }
        }

        [RelayCommand]
        private void ClickTitle()
        {
            INavigationService service = App.GetService<INavigationService>();
            service.NavigateTo(typeof(LibraryViewModel).FullName!, Root);
        }
    }
}

# endregion