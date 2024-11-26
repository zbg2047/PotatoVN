using System.Collections.ObjectModel;
using GalgameManager.Contracts.Services;
using GalgameManager.Helpers;
using GalgameManager.Models;
using GalgameManager.Models.Sources;
using GalgameManager.MultiStreamPage.Lists;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GalgameManager.Views.Dialog
{   
    public partial class MultiStreamPageSettingDialog
    {
        public ObservableCollection<IList> Result { get; } = new();
        public List<MultiStreamPageSortKeys> GameListKeys { get; } = new()
        {
            MultiStreamPageSortKeys.LastPlayed, 
            MultiStreamPageSortKeys.ReleaseDate,
        };
        public List<MultiStreamPageSortKeys> CategoryListKeys { get; } = new()
        {
            MultiStreamPageSortKeys.LastPlayed, 
            MultiStreamPageSortKeys.LastClicked,
        };
        public List<MultiStreamPageSortKeys> SourceListKeys { get; } = new()
        {
            MultiStreamPageSortKeys.LastPlayed, 
            MultiStreamPageSortKeys.LastClicked,
        };
        
        public List<Category> Categories { get; } = new();
        public List<CategoryGroup> CategoryGroups { get; } = new();
        public List<GalgameSourceBase> Sources { get; } = new();
            
        private readonly ICategoryService _categoryService = App.GetService<ICategoryService>();
        private readonly IGalgameSourceCollectionService _sourceCollectionService =
            App.GetService<IGalgameSourceCollectionService>();

        public MultiStreamPageSettingDialog(ObservableCollection<IList> lists)
        {
            InitializeComponent();

            XamlRoot = App.MainWindow!.Content.XamlRoot;
            PrimaryButtonText = "ConfirmLiteral".GetLocalized();
            SecondaryButtonText = "Cancel".GetLocalized();
            Loaded += async (_, _) =>
            {
                foreach (CategoryGroup group in await _categoryService.GetCategoryGroupsAsync())
                    Categories.AddRange(group.Categories);
                CategoryGroups.AddRange(await _categoryService.GetCategoryGroupsAsync());
                Sources.AddRange(_sourceCollectionService.GetGalgameSources());
            };
            
            foreach (var list in lists)
                Result.Add(list);
        }

        private void Delete(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: IList item })
                Result.Remove(item);
        }
    }
}

namespace MultiStreamPageSettingDialog
{
    public class TemplateSelector : DataTemplateSelector
    {
        public DataTemplate GameListTemplate { get; set; } = null!;
        public DataTemplate CategoryListTemplate { get; set; } = null!;
        public DataTemplate SourceListTemplate { get; set; } = null!;
        
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is GameList)
                return GameListTemplate;
            if (item is CategoryList)
                return CategoryListTemplate;
            if (item is SourceList)
                return SourceListTemplate;
            return base.SelectTemplateCore(item, container);
        }
    }
}