using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalgameManager.Contracts.Services;
using GalgameManager.Enums;
using GalgameManager.Helpers;
using GalgameManager.Models.Sources;
using H.NotifyIcon.Core;

namespace GalgameManager.Models.BgTasks;

public class RenameFolderTask : BgTaskBase
{
    private readonly List<Galgame> _games;
    private readonly ILocalSettingsService _localSettings;
    private readonly IGalgameSourceCollectionService _sourceService;
    private readonly IGalgameCollectionService _galService;
    
    public RenameFolderTask(List<Galgame> games)
    {
        _games = games;
        _localSettings = App.GetService<ILocalSettingsService>();
        _sourceService = App.GetService<IGalgameSourceCollectionService>();
        _galService = App.GetService<IGalgameCollectionService>();
    }

    protected override Task RecoverFromJsonInternal() => Task.CompletedTask;

    protected override Task RunInternal()
    {
        if (_games.Count == 0)
            return Task.CompletedTask;

        return Task.Run(async () =>
        {
            var total = _games.Count;
            var log = $"{DateTime.Now}\n\n";

            for (var i = 0; i < _games.Count; i++)
            {
                var game = _games[i];
                if (game.LocalPath is not { } oldPath) continue;
                
                try
                {
                    var dirInfo = new DirectoryInfo(oldPath);
                    var parentPath = dirInfo.Parent?.FullName;
                    if (parentPath == null) continue;

                    var newName = game.Name.Value?.RemoveInvalidChars();
                    if (string.IsNullOrEmpty(newName)) continue;

                    var newPath = Path.Combine(parentPath, newName);
                    
                    ChangeProgress(i, total, "RenameFolderTask_Progress".GetLocalized(game.Name));
                    
                    if (oldPath.Equals(newPath, StringComparison.OrdinalIgnoreCase))
                    {
                        log += $"{game.Name}: 无需重命名\n";
                        continue;
                    }

                    if (Directory.Exists(newPath))
                    {
                        log += $"{game.Name}: 目标路径已存在\n";
                        continue;
                    }

                    Directory.Move(oldPath, newPath);
                    
                    // // 更新游戏在源中的路径
                    // foreach (var source in game.Sources)
                    // {
                    //     if (source is GalgameFolderSource folderSource)
                    //     {
                    //         // 先从库中移除该游戏，再设置路径
                    //         if (source != null)
                    //             _sourceService.MoveOutNoOperate(source, game);
                    //             await _galService.SetLocalPathAsync(game, newPath);
                    //             _sourceService.Save(source);
                    //     }
                    // }

                    var source = _sourceService.GetGalgameSources().FirstOrDefault(s => s.Galgames.Any(g => g.Galgame == game));
                    if (source != null)
                    {
                        UiThreadInvokeHelper.Invoke(() => _sourceService.MoveOutNoOperate(source, game));
                    }
                    UiThreadInvokeHelper.Invoke(() => _galService.SetLocalPathAsync(game, newPath));

                    log += $"{game.Name}: 重命名成功 {oldPath} -> {newPath}\n";
                    
                    
                }
                catch (Exception e)
                {
                    log += $"{game.Name}: 重命名失败 - {e.Message}\n";
                }
            }

            ChangeProgress(total - 1, total, "RenameFolderTask_Saving".GetLocalized());
            FileHelper.SaveWithoutJson("RenameFolderTask.txt", log, "Logs");
            await Task.Delay(1000);

            ChangeProgress(total, total, "RenameFolderTask_Done".GetLocalized());
            
            if (App.MainWindow is null && await _localSettings.ReadSettingAsync<bool>(KeyValues.NotifyWhenGetGalgameInFolder))
            {
                App.SystemTray?.ShowNotification(nameof(NotificationIcon.Info), 
                    "RenameFolderTask_Done".GetLocalized());
            }
        });
    }

    public override bool OnSearch(string key) => false;

    public override string Title { get; } = "RenameFolderTask_Title".GetLocalized();
}

