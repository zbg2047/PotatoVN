using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.UI;
using GalgameManager.Contracts.Services;
using GalgameManager.Contracts.ViewModels;
using GalgameManager.Models;
using GalgameManager.Models.Filters;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using GalgameManager.MultiStreamPage.Lists;

namespace GalgameManager.ViewModels
{
    public partial class MultiStreamViewModel : ObservableRecipient, INavigationAware
    {
        public ObservableCollection<object> Lists { get; } = new();

        private readonly IGalgameCollectionService _gameService;
        private readonly ICategoryService _categoryService;
        private readonly INavigationService _navigationService;
        private readonly IFilterService _filterService;

        public MultiStreamViewModel(IGalgameCollectionService gameService, ICategoryService categoryService,
            INavigationService navigationService, IFilterService filterService)
        {
            _gameService = gameService;
            _categoryService = categoryService;
            _navigationService = navigationService;
            _filterService = filterService;
        }


        public void OnNavigatedTo(object parameter)
        {
            // test only
            Lists.Add(new GameList(_gameService.Galgames, "最近游玩的游戏",
                GameList.SortKey.LastPlayed));
            Lists.Add(new CategoryList(_categoryService.DeveloperGroup));
            foreach (Category c in _categoryService.StatusGroup.Categories)
                Lists.Add(new GameList(new ObservableCollection<Galgame>(c.GalgamesX), c.Name,
                    GameList.SortKey.LastPlayed));
        }

        public void OnNavigatedFrom()
        {
        }

        [RelayCommand]
        private void ClickGame(Galgame? clickedItem)
        {
            if (clickedItem == null) return;
            _navigationService.SetListDataItemForNextConnectedAnimation(clickedItem);
            _navigationService.NavigateTo(typeof(GalgameViewModel).FullName!,
                new GalgamePageParameter { Galgame = clickedItem });
        }
        
        [RelayCommand]
        private void ClickCategory(Category category)
        {
            _filterService.ClearFilters();
            _filterService.AddFilter(new CategoryFilter(category));
            _navigationService.NavigateTo(typeof(HomeViewModel).FullName!);
        }
    }

    public class MultiStreamPageSelector : DataTemplateSelector
    {
        public DataTemplate GameListTemplate { get; set; } = null!;
        public DataTemplate CategoryListTemplate { get; set; } = null!;

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            Debug.Assert(GameListTemplate is not null && CategoryListTemplate is not null, "Template is not set");
            return item switch
            {
                GameList => GameListTemplate,
                CategoryList => CategoryListTemplate,
                _ => base.SelectTemplateCore(item, container)
            };
        }
    }
}


# region OBSERVABLE_LISTS

namespace GalgameManager.MultiStreamPage.Lists
{
    public class GameList
    {
        public AdvancedCollectionView Games;
        public string Title;
        public SortKey Sort;

        public enum SortKey
        {
            LastPlayed,
        }

        public GameList(ObservableCollection<Galgame> games, string title, SortKey sort)
        {
            Games = new AdvancedCollectionView(games, true);
            Title = title;
            Sort = sort;

            Games.SortDescriptions.Add(new SortDescription(Sort switch
            {
                SortKey.LastPlayed => nameof(Galgame.LastPlayTime),
                _ => throw new ArgumentOutOfRangeException()
            }, SortDirection.Descending));
        }
    }

    public class CategoryList
    {
        public AdvancedCollectionView Categories;
        public string Title;

        public CategoryList(CategoryGroup group)
        {
            Categories = new AdvancedCollectionView(group.Categories, true);
            Title = group.Name;
        }
    }
}

# endregion