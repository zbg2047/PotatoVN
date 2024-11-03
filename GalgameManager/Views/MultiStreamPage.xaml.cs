using GalgameManager.ViewModels;
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
        InitializeComponent();
    }
}
