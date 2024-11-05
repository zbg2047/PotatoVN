using GalgameManager.Models;
using GalgameManager.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GalgameManager.Views;

public sealed partial class MultiStreamPage : Page
{
    public MultiStreamViewModel ViewModel
    {
        get;
    }

    public MultiStreamPage()
    {
        ViewModel = App.GetService<MultiStreamViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement container && container.DataContext != null)
            ViewModel.ClickGameCommand.Execute(container.DataContext as Galgame);
    }
}
