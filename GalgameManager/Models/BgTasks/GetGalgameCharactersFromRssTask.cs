using GalgameManager.Contracts.Services;
using GalgameManager.Enums;
using GalgameManager.Helpers;
using GalgameManager.Services;
using H.NotifyIcon.Core;

namespace GalgameManager.Models.BgTasks;

public class GetGalgameCharactersFromRssTask : BgTaskBase
{
    public GalgameUid GalgameUid
    {
        get => _galgame?.Uid ?? new();
        set =>
            _galgame =
                (App.GetService<IGalgameCollectionService>() as GalgameCollectionService)
                ?.GetGalgameFromUid(value);
    }

    private Galgame? _galgame;
    
    private GetGalgameCharactersFromRssTask() { } //For Json

    public GetGalgameCharactersFromRssTask(Galgame galgame)
    {
        _galgame = galgame;
    }
    
    protected override Task RecoverFromJsonInternal() => Task.CompletedTask;

    protected override Task RunInternal()
    {
        if (_galgame is null)
            return Task.CompletedTask;
        ILocalSettingsService localSettings = App.GetService<ILocalSettingsService>();
        GalgameCollectionService galgameService = (App.GetService<IGalgameCollectionService>() as GalgameCollectionService)!;
        var log = string.Empty;
        
        return Task.Run((async Task () =>
        {
            log += $"{DateTime.Now}\n{_galgame.Name.Value}\n\n";
            var total = _galgame.Characters.Count;
            for (var i = 0; i < _galgame.Characters.Count; i++)
            {
                GalgameCharacter character = _galgame.Characters[i];
                ChangeProgress(i, total, 
                    "Galgame_GetCharacterInfo_GettingInfo".GetLocalized(character.Name, _galgame.Name.Value??""));
                await UiThreadInvokeHelper.InvokeAsync(async Task() =>
                {
                    character = await galgameService.PhraseGalCharacterAsync(character, _galgame.RssType);
                });
                log += $"{_galgame.Name.Value}->{character.Name} Done\n";
                ChangeProgress(i+1, total, 
                    "Galgame_GetCharacterInfo_GottenInfo".GetLocalized(character.Name, _galgame.Name.Value??""));

            }
            
            await galgameService.SaveGalgamesAsync();
            
            ChangeProgress(0, 1, "Galgame_GetCharacterInfo_Saving".GetLocalized());
            FileHelper.SaveWithoutJson(_galgame.GetLogName(), log, "Logs");
            await Task.Delay(1000); //等待文件保存

            ChangeProgress(1, 1, "Galgame_GetCharacterInfo_Done".GetLocalized(_galgame.Name.Value ?? string.Empty));
            if (App.MainWindow is null && await localSettings.ReadSettingAsync<bool>(KeyValues.NotifyWhenGetGalgameInFolder))
            {
                App.SystemTray?.ShowNotification(nameof(NotificationIcon.Info),
                    "Galgame_GetCharacterInfo_Done".GetLocalized(_galgame.Name.Value ?? string.Empty));
            }
        })!);
    }

    public override bool OnSearch(string key)
    {
        if (_galgame is null) return false;
        return _galgame.Uid.ToString() == key;
    }

    public override string Title { get; } = "GetCharacterInfoTask_Title".GetLocalized();
}