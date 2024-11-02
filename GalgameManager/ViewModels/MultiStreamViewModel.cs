using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GalgameManager.Contracts.Services;
using GalgameManager.Models;

namespace GalgameManager.ViewModels;

public partial class MultiStreamViewModel : ObservableRecipient
{
    public ObservableCollection<Galgame> Games;

    private readonly IGalgameCollectionService _gameService;

    public MultiStreamViewModel(IGalgameCollectionService gameService)
    {
        _gameService = gameService;
        Games = gameService.Galgames;
    }
}