using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GalgameManager.Contracts.Services;
using GalgameManager.Contracts.ViewModels;
using GalgameManager.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

namespace GalgameManager.ViewModels;

public partial class AnnualReportViewModel : ObservableRecipient, INavigationAware
{

    private int _currentPageIndex = 0;
    private int _previousPageIndex = -1;

    [ObservableProperty]
    private Frame? contentFrame;

    public AnnualReportViewModel()
    {
    }

    public void OnNavigatedTo(object parameter)
    {
    }

    public void OnNavigatedFrom()
    {
    }

    public void SelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (ContentFrame == null) return;
        
        SelectorBarItem selectedItem = sender.SelectedItem;
        _currentPageIndex = sender.Items.IndexOf(selectedItem);

        Type pageType = typeof(Views.AnnualReportSubPage1);

        switch (_currentPageIndex)
        {
            case 0:
                pageType = typeof(Views.AnnualReportSubPage1);
                break;
            case 1:
                pageType = typeof(Views.AnnualReportSubPage2);
                break;
        }

        var slideNavigationTransitionEffect = _currentPageIndex - _previousPageIndex > 0 
            ? SlideNavigationTransitionEffect.FromRight 
            : SlideNavigationTransitionEffect.FromLeft;

        ContentFrame.Navigate(pageType, null, new SlideNavigationTransitionInfo() 
        { 
            Effect = slideNavigationTransitionEffect 
        });

        _previousPageIndex = _currentPageIndex;
    }

    [RelayCommand]
    private void NextPage()
    {
        if (ContentFrame == null) return;

        NavigateToPage(Math.Min(_currentPageIndex + 1, 1));
        UpdateSelectorBarSelection();
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (ContentFrame == null) return;
        NavigateToPage(Math.Max(_currentPageIndex - 1, 0));
        UpdateSelectorBarSelection();
    }

    private void UpdateSelectorBarSelection()
    {
        var selectorBar = ContentFrame?.Parent is Grid grid 
            ? grid.Children.OfType<SelectorBar>().FirstOrDefault() 
            : null;

        if (selectorBar != null && _currentPageIndex >= 0 && _currentPageIndex < selectorBar.Items.Count)
        {
            selectorBar.SelectedItem = selectorBar.Items[_currentPageIndex];
        }
    }

    private void NavigateToPage(int pageIndex)
    {
        if (ContentFrame == null) return;

        Type pageType = pageIndex switch
        {
            0 => typeof(Views.AnnualReportSubPage1),
            1 => typeof(Views.AnnualReportSubPage2),
            _ => typeof(Views.AnnualReportSubPage1)
        };

        _previousPageIndex = _currentPageIndex;

        var slideNavigationTransitionEffect = pageIndex - _previousPageIndex > 0 
            ? SlideNavigationTransitionEffect.FromRight 
            : SlideNavigationTransitionEffect.FromLeft;

        ContentFrame.Navigate(pageType, null, new SlideNavigationTransitionInfo() 
        { 
            Effect = slideNavigationTransitionEffect 
        });

        _currentPageIndex = pageIndex;
    }
}
