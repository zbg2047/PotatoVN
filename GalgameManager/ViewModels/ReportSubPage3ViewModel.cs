using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GalgameManager.Contracts.ViewModels;
using GalgameManager.Models;
using GalgameManager.Services;
using CommunityToolkit.WinUI.Collections;
using GalgameManager.Contracts.Services;
using GalgameManager.Contracts.ViewModels;
using GalgameManager.Enums;
using GalgameManager.Helpers;
using GalgameManager.Models;
using GalgameManager.Models.Filters;
using GalgameManager.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using GalgameManager.Contracts;
using GalgameManager.Contracts.Services;
using GalgameManager.Contracts.ViewModels;

namespace GalgameManager.ViewModels
{
    public partial class ReportSubPage3ViewModel : ObservableObject, INavigationAware
    {


        [ObservableProperty]
        private Category _category;

        [ObservableProperty]
        private int _playedGamesCount;

        private readonly IGalgameCollectionService _galgameCollectionService;
        [ObservableProperty] private Galgame _game = null!;
        [ObservableProperty] private ObservableCollection<Galgame> _games = new();

        private readonly INavigationService _navigationService;

        public ReportSubPage3ViewModel(IGalgameCollectionService galgameCollectionService, 
                                     INavigationService navigationService)
        {
            _category = new Category("游戏会社");
            _category.ImagePath = "ms-appx:///Assets/Pictures/Potato.png";
            _playedGamesCount = 5;

            _galgameCollectionService = galgameCollectionService;
            _navigationService = navigationService;

            Games = new ObservableCollection<Galgame>(_galgameCollectionService.Galgames);
            Game = _galgameCollectionService.Galgames[0];
        }

       
        
        public void OnNavigatedTo(object parameter)
        {
            
        }

        public void OnNavigatedFrom()
        {
        }

        [RelayCommand]
        private void NavigateTo(IDisplayableGameObject? clickedItem)
        {
            if (clickedItem is Galgame galgame)
            {
                _navigationService.NavigateTo(typeof(GalgameViewModel).FullName!,
                    new GalgamePageParameter { Galgame = galgame });
            }
        }
    }
}
