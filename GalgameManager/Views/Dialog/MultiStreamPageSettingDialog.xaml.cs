using System.Collections.ObjectModel;
using GalgameManager.Contracts.Services;
using GalgameManager.Helpers;
using GalgameManager.Models;
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
        public List<Category> Categories { get; } = new();
        public List<CategoryGroup> CategoryGroups { get; } = new();

        private readonly ICategoryService _categoryService = App.GetService<ICategoryService>();

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
        public DataTemplate SourceListFullTemplate { get; set; } = null!;
        public DataTemplate SourceListSubTemplate { get; set; } = null!;
        
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is GameList)
                return GameListTemplate;
            if (item is CategoryList)
                return CategoryListTemplate;
            if (item is SourceList sourceList)
                return sourceList.Root is null ? SourceListFullTemplate : SourceListSubTemplate;
            return base.SelectTemplateCore(item, container);
        }
    }
}