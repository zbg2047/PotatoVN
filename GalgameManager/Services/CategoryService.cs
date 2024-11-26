using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using CommunityToolkit.WinUI;
using GalgameManager.Contracts.Services;
using GalgameManager.Enums;
using GalgameManager.Helpers;
using GalgameManager.Helpers.Phrase;
using GalgameManager.Models;
using Microsoft.UI.Dispatching;

namespace GalgameManager.Services;

public class CategoryService : ICategoryService
{
    private ObservableCollection<CategoryGroup> _categoryGroups = new();
    private readonly GalgameCollectionService _galgameService;
    private CategoryGroup? _developerGroup, _statusGroup;
    private readonly Category[] _statusCategory = new Category[5];
    private bool _isInit;
    private readonly ILocalSettingsService _localSettings;
    private readonly BlockingCollection<Category> _queue = new();
    private readonly BgmPhraser _bgmPhraser;
    private readonly DispatcherQueue? _dispatcher;

    public CategoryGroup? GetGroup(Guid id) => _categoryGroups.FirstOrDefault(group => group.Id == id);

    public CategoryGroup StatusGroup => _statusGroup!;
    public CategoryGroup DeveloperGroup => _developerGroup!;

    public CategoryService(ILocalSettingsService localSettings, IGalgameCollectionService galgameService)
    {
        _localSettings = localSettings;
        _galgameService = (galgameService as GalgameCollectionService)!;
        _galgameService.GalgameAddedEvent += UpdateCategory;
        _galgameService.GalgameDeletedEvent += galgame =>
        {
            List<Category> toRemove = galgame.Categories.ToList();
            toRemove.ForEach(c => c.Remove(galgame));
        };
        _bgmPhraser = (BgmPhraser)_galgameService.PhraserList[(int)RssType.Bangumi];

        async void OnAppClosing() => await SaveAsync();

        App.OnAppClosing += OnAppClosing;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        Thread worker = new(Worker)
        {
            IsBackground = true
        };
        worker.Start();
    }

    public async Task Init()
    {
        if (_isInit) return;
        
        _categoryGroups = await _localSettings.ReadSettingAsync<ObservableCollection<CategoryGroup>>
                              (KeyValues.CategoryGroups, true, converters: new() { new GalgameAndUidConverter() }) 
                          ?? new ObservableCollection<CategoryGroup>();
        foreach(CategoryGroup group in _categoryGroups)
            group.Categories.ForEach(c => c.GalgamesX.RemoveNull());
        
        await Upgrade();
        
        InitStatusGroup();
        InitDeveloperGroup();
        
        foreach (Galgame g in _galgameService.Galgames) 
            g.GalPropertyChanged += HandleGalPropertyChanged;
        _galgameService.GalgameAddedEvent += galgame =>
        {
            if (_isInit == false) return;
            HandleGalPropertyChanged(galgame, nameof(Galgame.Developer), galgame.Developer.Value);
            HandleGalPropertyChanged(galgame, nameof(Galgame.PlayType), galgame.PlayType);
            HandleGalPropertyChanged(galgame, nameof(Galgame.LastPlayTime), galgame.LastPlayTime);
        };
        _galgameService.GalgameDeletedEvent += galgame =>
        {
            galgame.GalPropertyChanged -= HandleGalPropertyChanged;
            foreach (Category category in galgame.Categories)
                category.Remove(galgame);
        }; // 避免内存泄漏

        // 给Galgame注入Category
        foreach (Category category in _categoryGroups.SelectMany(group => group.Categories))
            category.GalgamesX.ForEach(g =>
            {
                if (g.Categories.Contains(category)) return;
                g.Categories.Add(category);
            });

        _isInit = true;
        return;
        
        void HandleGalPropertyChanged(Galgame gal, string name, object? _)
        {
            switch (name)
            {
                case nameof(Galgame.Developer):
                    UpdateCategory(gal);
                    break;
                case nameof(Galgame.PlayType):
                    GetStatusCategory(gal)?.Remove(gal);
                    _statusCategory[(int)gal.PlayType].Add(gal);
                    break;
                case nameof(Galgame.LastPlayTime):
                    foreach (Category category in gal.Categories)
                        category.UpdateLastPlayed();
                    break;
            }
        }
    }

    public async Task<ObservableCollection<CategoryGroup>> GetCategoryGroupsAsync()
    {
        if (_isInit == false)
            await Init();
        return _categoryGroups;
    }

    /// <summary>
    /// 新增分类组
    /// </summary>
    /// <param name="name">分类组名</param>
    /// <returns>创建的分类组</returns>
    public CategoryGroup AddCategoryGroup(string name)
    {
        CategoryGroup newGroup = new(name, CategoryGroupType.Custom);
        _categoryGroups.Add(newGroup);
        return newGroup;
    }
    
    /// <summary>
    /// 删除分类组
    /// </summary>
    /// <param name="categoryGroup">分类组</param>
    public void DeleteCategoryGroup(CategoryGroup categoryGroup)
    {
        foreach (Category category in categoryGroup.Categories) // 删除分类组里的分类（如果没有其他分类组在用的话）
        {
            if (_categoryGroups.Count(group => group.Categories.Contains(category)) == 1)
                category.Delete();
        }
        _categoryGroups.Remove(categoryGroup);
    }
    
    /// <summary>
    /// 将源分类合并到目标分类，然后删除源分类 <br/>
    /// 如果目标分类和源分类相同，则不进行任何操作
    /// </summary>
    /// <param name="target">目标分类</param>
    /// <param name="source">源分类</param>
    public void Merge(Category target, Category source)
    {
        if (target == source) return;
        target.Add(source);
        DeleteCategory(source);
    }

    public Category? GetCategory(Guid id)
    {
        return _categoryGroups.SelectMany(group => group.Categories).FirstOrDefault(category => category.Id == id);
    }

    public Category? GetCategory(string name)
    {
        return _categoryGroups.SelectMany(group => group.Categories).FirstOrDefault(category => category.Name == name);
    }

    /// <summary>
    /// 删除分类
    /// </summary>
    /// <param name="category">分类</param>
    public void DeleteCategory(Category category)
    {
        category.Delete();
        foreach (CategoryGroup categoryGroup in _categoryGroups)
            categoryGroup.Categories.Remove(category);
    }

    /// <summary>
    /// 更新某个分类的信息（目前只有开发商的图片）
    /// </summary>
    /// <param name="category"></param>
    public void UpdateCategory(Category category)
    {
        _queue.Add(category);
    }

    /// <summary>
    /// 更新所有游戏的分类（开发商及游玩状态）
    /// </summary>
    public async Task UpdateAllGames()
    {
        IList<Galgame> games = _galgameService.Galgames;
        foreach (Galgame game in games)
            UpdateCategory(game);
        await SaveAsync();
        //todo:空Category删除
    }

    private async void UpdateCategory(Galgame galgame)
    {
        if (_isInit == false) await Init();
        // 更新开发商分类组
        if (await _localSettings.ReadSettingAsync<bool>(KeyValues.AutoCategory) 
            && galgame.Developer.Value != Galgame.DefaultString && galgame.Developer.Value != string.Empty)
        {
            //移除旧的开发商分类
            Category? old = GetDeveloperCategory(galgame);
            old?.Remove(galgame);
            
            var developerStrings = galgame.Developer.Value!.Split(',');
            foreach (var developerStr in developerStrings)
            {
                Producer producer = ProducerDataHelper.Producers.FirstOrDefault(p =>
                    p.Names.Any(name => string.Equals(name, developerStr, StringComparison.CurrentCultureIgnoreCase))) ?? new Producer(developerStr);
                Category? developer = _developerGroup!.Categories.FirstOrDefault(c => 
                        producer.Names.Any(name => string.Equals(name, c.Name, StringComparison.CurrentCultureIgnoreCase)));
                if (developer is null)
                {
                    developer = new Category(producer.Name);
                    _queue.Add(developer);
                    _developerGroup!.Categories.Add(developer);
                }
                developer.Add(galgame);
            }
        }
        
    }

    private async void Worker()
    {
        foreach (Category category in _queue.GetConsumingEnumerable())
        {
            var imgUrl = await _bgmPhraser.GetDeveloperImageUrlAsync(category.Name);
            if (imgUrl is null) continue;
            var imagPath = await DownloadHelper.DownloadAndSaveImageAsync(imgUrl);
            if(imagPath is not null && _dispatcher is not null)
                await _dispatcher.EnqueueAsync(() =>
                {
                    category.ImagePath = imagPath;
                });
        }
    }

    private async Task SaveAsync()
    {
        await _localSettings.SaveSettingAsync(KeyValues.CategoryGroups, _categoryGroups, true,
            converters: new() { new GalgameAndUidConverter() });
        if (_statusGroup != null)
            _categoryGroups.Add(_statusGroup);
    }

    /// <summary>
    /// 获取开发商分类，如果没有则返回null
    /// </summary>
    public Category? GetDeveloperCategory(Galgame galgame)
    {
        foreach(Category category in galgame.Categories)
            if(_developerGroup!.Categories.Contains(category))
                return category;
        return null;
    }

    /// <summary>
    /// 获取状态分类，如果没有则返回null
    /// </summary>
    private Category? GetStatusCategory(IEnumerable<Category> categories)
    {
        return categories.FirstOrDefault(category => _statusGroup!.Categories.Contains(category));
    }

    /// <summary>
    /// 获取状态分类，如果没有则返回null
    /// </summary>
    private Category? GetStatusCategory(Galgame galgame)
    {
        return GetStatusCategory(galgame.Categories);
    }

    private void InitStatusGroup()
    {
        _statusGroup = _categoryGroups.FirstOrDefault(group => group.Type == CategoryGroupType.Status);
        if (_statusGroup is null)
        {
            _statusGroup = new CategoryGroup(ResourceExtensions.GetLocalized("CategoryService_Status"),
                CategoryGroupType.Status);
            _categoryGroups.Add(_statusGroup);
            _statusGroup.Categories.Add(new Category(PlayType.None.GetLocalized())
                { Id = new Guid("00000000-0000-0000-0000-000000000001") });
            _statusGroup.Categories.Add(new Category(PlayType.Played.GetLocalized())
                { Id = new Guid("00000000-0000-0000-0000-000000000002") });
            _statusGroup.Categories.Add(new Category(PlayType.Playing.GetLocalized())
                { Id = new Guid("00000000-0000-0000-0000-000000000003") });
            _statusGroup.Categories.Add(new Category(PlayType.Shelved.GetLocalized())
                { Id = new Guid("00000000-0000-0000-0000-000000000004") });
            _statusGroup.Categories.Add(new Category(PlayType.Abandoned.GetLocalized())
                { Id = new Guid("00000000-0000-0000-0000-000000000005") });
            SetStatusCategory();
            foreach(Galgame game in _galgameService.Galgames.Where(g => GetStatusCategory(g) is null)) 
                _statusCategory[(int)game.PlayType].Add(game);
        }
        else
            SetStatusCategory();
        return;

        void SetStatusCategory()
        {
            _statusCategory[(int)PlayType.None] = _statusGroup.Categories[0];
            _statusCategory[(int)PlayType.Played] = _statusGroup.Categories[1];
            _statusCategory[(int)PlayType.Playing] = _statusGroup.Categories[2];
            _statusCategory[(int)PlayType.Shelved] = _statusGroup.Categories[3];
            _statusCategory[(int)PlayType.Abandoned] = _statusGroup.Categories[4];
        }
    }

    private void InitDeveloperGroup()
    {
        try
        {
            _developerGroup = _categoryGroups.First(cg => cg.Type == CategoryGroupType.Developer);
            _developerGroup.Name = ResourceExtensions.GetLocalized("CategoryService_Developer");
        }
        catch
        {
            _developerGroup = new CategoryGroup(ResourceExtensions.GetLocalized("CategoryService_Developer"),
                CategoryGroupType.Developer);
            _categoryGroups.Add(_developerGroup);
        }
    }

    /// <summary>
    /// 旧的存储格式与新的存储格式不兼容，需要升级
    /// </summary>
    private async Task Upgrade()
    {
        // 给各分类添加id字段, since v1.8.0
        if (!await _localSettings.ReadSettingAsync<bool>(KeyValues.CategoryIdUpgraded))
        {
            foreach (CategoryGroup group in _categoryGroups)
            foreach (Category category in group.Categories)
                category.Id = Guid.NewGuid();
            await _localSettings.SaveSettingAsync(KeyValues.CategoryIdUpgraded, true);
            await SaveAsync();
        }
        // 给各分类添加LastPlayed字段, since v1.8.0
        if (!await _localSettings.ReadSettingAsync<bool>(KeyValues.CategoryLastPlayedUpgraded))
        {
            foreach (CategoryGroup group in _categoryGroups)
            foreach (Category category in group.Categories)
                category.UpdateLastPlayed();
            await _localSettings.SaveSettingAsync(KeyValues.CategoryLastPlayedUpgraded, true);
            await SaveAsync();
        }
        await Task.CompletedTask; //todo：待完成多Source化后添加
    }

    /// <summary>
    /// 是否在某个type的分类组中
    /// </summary>
    /// <param name="category">分类</param>
    /// <param name="type">type</param>
    public bool IsInCategoryGroup(Category category, CategoryGroupType type)
    {
        return _categoryGroups.Any(g => g.Type == type && g.Categories.Contains(category));
    }
}