using System.Collections.ObjectModel;
using GalgameManager.Helpers;
using GalgameManager.MultiStreamPage.Lists;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GalgameManager.Views.Dialog
{
    public partial class MultiStreamPageSettingDialog
    {
        public ObservableCollection<object> Result { get; } = new();
        public List<MultiStreamPageSortKeys> GameListFullKeys { get; } = new() { MultiStreamPageSortKeys.LastPlayed };
        public List<MultiStreamPageSortKeys> CategoryListKeys { get; } = new()
        {
            MultiStreamPageSortKeys.LastPlayed, 
            MultiStreamPageSortKeys.LastClicked,
        };

        public MultiStreamPageSettingDialog(ObservableCollection<object> lists)
        {
            InitializeComponent();

            XamlRoot = App.MainWindow!.Content.XamlRoot;
            PrimaryButtonText = "ConfirmLiteral".GetLocalized();
            SecondaryButtonText = "Cancel".GetLocalized();

            foreach (var list in lists)
                Result.Add(list);
        }
    }
}

namespace MultiStreamPageSettingDialog
{
    public class TemplateSelector : DataTemplateSelector
    {
        public DataTemplate GameListFullTemplate { get; set; } = null!;
        public DataTemplate GameListCategoryTemplate { get; set; } = null!;
        public DataTemplate GameListSourceTemplate { get; set; } = null!;
        public DataTemplate CategoryListTemplate { get; set; } = null!;
        public DataTemplate SourceListFullTemplate { get; set; } = null!;
        public DataTemplate SourceListSubTemplate { get; set; } = null!;
        
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is GameList gameList)
            {
                if (gameList.Category is not null)
                    return GameListCategoryTemplate;
                if (gameList.Source is not null)
                    return GameListSourceTemplate;
                return GameListFullTemplate;
            }
            if (item is CategoryList)
                return CategoryListTemplate;
            if (item is SourceList sourceList)
                return sourceList.Root is null ? SourceListFullTemplate : SourceListSubTemplate;
            return base.SelectTemplateCore(item, container);
        }
    }
}