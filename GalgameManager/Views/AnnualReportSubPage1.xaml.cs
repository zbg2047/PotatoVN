
using GalgameManager.ViewModels;

using Microsoft.UI.Xaml.Controls;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace GalgameManager.Views;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AnnualReportSubPage1 : Page
{
    public ReportSubPage1ViewModel ViewModel
    {
        get;
    }

    public AnnualReportSubPage1()
    {
        ViewModel = App.GetService<ReportSubPage1ViewModel>();
        this.InitializeComponent();
    }
}
