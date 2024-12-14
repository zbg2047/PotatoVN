using GalgameManager.Contracts.Services;
using GalgameManager.Core.Contracts.Services;
using GalgameManager.Helpers;
using GalgameManager.Models;
using GalgameManager.Models.BgTasks;
using GalgameManager.Models.Sources;

namespace GalgameManager.Services;

public class LocalFolderSourceService : IGalgameSourceService
{
    private readonly IInfoService _infoService;
    private readonly IFileService _fileService;

    public LocalFolderSourceService(IInfoService infoService, IFileService fileService)
    {
        _infoService = infoService;
        _fileService = fileService;
    }

    public BgTaskBase MoveInAsync(GalgameSourceBase target, Galgame game, string? targetPath = null)
    {
        if (targetPath is null) throw new PvnException("targetPath is null");
        if (target is not GalgameFolderSource) throw new ArgumentException("target is not GalgameFolderSource");
        return new LocalFolderSourceMoveInTask(game, targetPath);
    }

    public BgTaskBase MoveOutAsync(GalgameSourceBase target, Galgame game)
    {
        return new LocalFolderSourceMoveOutTask(game, target);
    }

    public async Task SaveMetaAsync(Galgame game)
    {
        foreach (GalgameFolderSource source in game.Sources.OfType<GalgameFolderSource>())
        {
            var folderPath = source.GetPath(game)!;
            var metaPath = Path.Combine(folderPath, ".PotatoVN");
            if (!Directory.Exists(metaPath)) Directory.CreateDirectory(metaPath);
            Galgame meta = game.DeepClone();
            // 备份图片
            if (Utils.IsImageValid(meta.ImagePath.Value))
            {
                CopyImg(meta.ImagePath.Value, metaPath);
                meta.ImagePath.ForceSet(Path.Combine(".", Path.GetFileName(meta.ImagePath.Value!)));
            }
            foreach (GalgameCharacter character in meta.Characters)
            {
                if (Utils.IsImageValid(character.ImagePath))
                {
                    CopyImg(character.ImagePath, metaPath);
                    character.ImagePath = Path.Combine(".", Path.GetFileName(character.ImagePath));
                }
                if (Utils.IsImageValid(character.PreviewImagePath))
                {
                    CopyImg(character.PreviewImagePath, metaPath);
                    character.PreviewImagePath = Path.Combine(".", Path.GetFileName(character.PreviewImagePath));
                }
            }
            _fileService.Save(metaPath, "meta.json", meta);
        }

        await Task.CompletedTask;
    }

    public async Task<Galgame?> LoadMetaAsync(string path)
    {
        await Task.CompletedTask;
        var metaFolderPath = Path.Combine(path, ".PotatoVN");
        if (!Directory.Exists(metaFolderPath)) return null; // 不存在备份文件夹
        Galgame meta = _fileService.Read<Galgame>(metaFolderPath, "meta.json")!;
        if (meta is null) throw new PvnException("meta.json not exist");
        meta.ImagePath.ForceSet(LoadImg(meta.ImagePath.Value, metaFolderPath));
        foreach (GalgameCharacter character in meta.Characters)
        {
            character.ImagePath = LoadImg(character.ImagePath, metaFolderPath)!;
            character.PreviewImagePath = LoadImg(character.PreviewImagePath, metaFolderPath)!;
        }
        meta.ExePath = LoadImg(meta.ExePath, metaFolderPath, defaultReturn: null);
        meta.SavePath = Directory.Exists(meta.SavePath) ? meta.SavePath : null; //检查存档路径是否存在并设置SavePosition字段
        meta.FindSaveInPath();
        return meta;
    }

    public async Task<(long total, long used)> GetSpaceAsync(GalgameSourceBase source)
    {
        await Task.CompletedTask;
        try
        {
            DriveInfo? info = GetDriveInfo(source.Path);
            if (info is null) return (-1, -1);
            return (info.TotalSize, info.TotalSize - info.AvailableFreeSpace);
        }
        catch (Exception e)
        {
            _infoService.DeveloperEvent(msg: $"failed to get drive info with exception: {e}");
            return (-1, -1);
        }
    }

    public string GetMoveInDescription(GalgameSourceBase target, string targetPath)
    {
        return "LocalFolderSourceService_MoveInDescription".GetLocalized(targetPath);
    }

    public string GetMoveOutDescription(GalgameSourceBase target, Galgame galgame)
    {
        var path = target.GetPath(galgame) ?? string.Empty;
        return "LocalFolderSourceService_MoveOutDescription".GetLocalized(path);
    }

    public string? CheckMoveOperateValid(GalgameSourceBase? moveIn, GalgameSourceBase? moveOut, Galgame galgame)
    {
        if (moveIn?.SourceType == GalgameSourceType.LocalFolder)
            return moveOut?.SourceType == GalgameSourceType.LocalFolder
                ? null
                : "LocalFolderSourceService_MoveOutError".GetLocalized();
        return null;
    }

    private static void CopyImg(string? src, string metaPath)
    {
        if (!Utils.IsImageValid(src) || src is null) return;
        var target = Path.Combine(metaPath, Path.GetFileName(src));
        if (File.Exists(target) && new FileInfo(target).Length == new FileInfo(src).Length) return; //文件已存在且大小相同就不复制
        File.Copy(src, target, true);
    }

    private static string? LoadImg(string? target, string path, string defaultTarget = Galgame.DefaultImagePath, 
        string? defaultReturn = Galgame.DefaultImagePath)
    {
        if (string.IsNullOrEmpty(target) || target == defaultTarget) return defaultReturn;
        var targetPath = Path.GetFullPath(Path.Combine(path, target));
        return File.Exists(targetPath) ? targetPath : defaultReturn;
    }
    
    private static DriveInfo? GetDriveInfo(string path)
    {
        var root = Path.GetPathRoot(path);
        return root is null ? null : new DriveInfo(root);
    }
}