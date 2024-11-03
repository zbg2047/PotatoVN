using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.UI;
using GalgameManager.Contracts.Services;
using GalgameManager.Contracts.ViewModels;
using GalgameManager.Models;
using GalgameManager.Models.Sources;
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

        public MultiStreamViewModel(IGalgameCollectionService gameService, ICategoryService categoryService)
        {
            _gameService = gameService;
            _categoryService = categoryService;
        }

        public void OnNavigatedTo(object parameter)
        {
            // test only
            Lists.Add(new GameList(_gameService.Galgames, "最近游玩的游戏",
                GameList.SortKey.LastPlayed));
            foreach (Category c in _categoryService.StatusGroup.Categories)
                Lists.Add(new GameList(new ObservableCollection<Galgame>(c.GalgamesX), c.Name,
                    GameList.SortKey.LastPlayed));
        }

        public void OnNavigatedFrom()
        {
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
                GalgameSourceBase => CategoryListTemplate,
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
}

# endregion