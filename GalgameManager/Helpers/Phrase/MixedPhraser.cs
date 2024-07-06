﻿using System.Collections.ObjectModel;
using GalgameManager.Contracts.Phrase;
using GalgameManager.Enums;
using GalgameManager.Models;

namespace GalgameManager.Helpers.Phrase;

public class MixedPhraser : IGalInfoPhraser, IGalCharacterPhraser
{
    private readonly BgmPhraser _bgmPhraser;
    private readonly VndbPhraser _vndbPhraser;
    private IEnumerable<string> _developerList;
    private bool _init;
    private static string[] _sourcesNames = { "vndb", "bgm", "ymgal" };

    
    private void Init()
    {
        _init = true;
        _developerList = ProducerDataHelper.Producers.SelectMany(p => p.Names);
    }
    
    private string? GetDeveloperFromTags(Galgame galgame)
    {
        if (_init == false)
            Init();
        string? result = null;
        foreach (var tag in galgame.Tags.Value!)
        {
            double maxSimilarity = 0;
            foreach(var dev in _developerList)
            {
                if (IGalInfoPhraser.Similarity(dev, tag) > maxSimilarity)
                {
                    maxSimilarity = IGalInfoPhraser.Similarity(dev, tag);
                    result = dev;
                }
            }

            if (result != null && maxSimilarity > 0.75) // magic number: 一个tag和开发商的相似度大于0.75就认为是开发商
                break;
        }
        return result;
    }
    
    public MixedPhraser(BgmPhraser bgmPhraser, VndbPhraser vndbPhraser)
    {
        _bgmPhraser = bgmPhraser;
        _vndbPhraser = vndbPhraser;
        _developerList = new List<string>();
    }
    
    public async Task<Galgame?> GetGalgameInfo(Galgame galgame)
    {
        if (_init == false)
            Init();
        Galgame? bgm = new(), vndb = new();
        bgm.Name = galgame.Name;
        vndb.Name = galgame.Name;
        // 试图从Id中获取bgmId和vndbId
        try
        {
            Dictionary<string, string> tmp = Id2IdDict(galgame.Ids[(int)RssType.Mixed] ?? "");
            string ? bgmId;string? vndbId;
            tmp.TryGetValue("bgm", out bgmId);
            tmp.TryGetValue("vndb", out vndbId);
            
            if (string.IsNullOrEmpty(bgmId) == false)
            {
                bgm.RssType = RssType.Bangumi;
                bgm.Id = bgmId;
            }
            if (string.IsNullOrEmpty(vndbId) == false)
            {
                vndb.RssType = RssType.Vndb;
                vndb.Id = vndbId;
            }
        }
        catch (Exception)
        {
            // ignored
        }
        // 从bgm和vndb中获取信息
        bgm = await _bgmPhraser.GetGalgameInfo(bgm);
        vndb = await _vndbPhraser.GetGalgameInfo(vndb);
        if(bgm == null && vndb == null)
            return null;
        
        // 合并信息
        Galgame result = new()
        {
            RssType = RssType.Mixed,
            Id = $"bgm:{(bgm == null ? "null" : bgm.Id)},vndb:{(vndb == null ? "null" : vndb.Id)}",
            // name
            Name = bgm != null ? bgm.Name : vndb!.Name,
            // description
            Description = bgm != null ? bgm.Description : vndb!.Description,
            // expectedPlayTime
            ExpectedPlayTime = vndb != null ? vndb.ExpectedPlayTime: Galgame.DefaultString,
            // rating
            Rating = bgm != null ? bgm.Rating : vndb!.Rating,
            // imageUrl
            ImageUrl = vndb != null ? vndb.ImageUrl : bgm!.ImageUrl,
            // release date
            ReleaseDate = bgm?.ReleaseDate ?? vndb!.ReleaseDate,
            Characters =  (bgm?.Characters.Count > 0 ? bgm?.Characters : vndb?.Characters) ?? new ObservableCollection<GalgameCharacter>()
        };

        // Chinese name
        if (bgm != null && !string.IsNullOrEmpty(bgm.CnName))result.CnName =  bgm.CnName;
        else if (vndb != null && !string.IsNullOrEmpty(vndb.CnName)) result.CnName = vndb.CnName;
        else result.CnName = "";
        
        // developer
        if (bgm != null && bgm.Developer != Galgame.DefaultString)result.Developer = bgm.Developer;
        else if (vndb != null && vndb.Developer != Galgame.DefaultString)result.Developer = vndb.Developer;
        // tags
        result.Tags = bgm != null ? bgm.Tags : vndb!.Tags;
        
        // developer from tag
        if (result.Developer == Galgame.DefaultString)
        {
            var tmp = GetDeveloperFromTags(result);
            if (tmp != null)
                result.Developer = tmp;
        }
        return result;
    }

    public static Dictionary<string, string> Id2IdDict(string ids)
    {
        Dictionary<string, string> idDict = new();
        ids = ids.Replace("，", ",").Replace(" ", "");
        foreach (var id in ids.Split(","))
        {
            if (id.Contains(':'))
            {
                var parts = id.Split(":");
                if (parts.Length == 2 && _sourcesNames.Contains(parts[0]))
                {
                    idDict.Add(parts[0], parts[1]);
                }
            }
        }

        return idDict;
    }
    
    public static string IdDict2Id(Dictionary<string, string?> ids)
    {
        List<string> idParts = new();
        foreach (var (name, id) in ids)
        {
            if (_sourcesNames.Contains(name) && !id.IsNullOrEmpty())
            {
                idParts.Add($"{name}:{id}");
            }
        }
        return string.Join(",", idParts);
    }
    
    public static string IdList2Id(string?[] ids)
    {
        Dictionary<string, string?> idDict = new()
        {
            ["bgm"] = ids[(int)RssType.Bangumi],
            ["vndb"] = ids[(int)RssType.Vndb],
            ["ymgal"] = ids[(int)RssType.Ymgal]
        };
        return IdDict2Id(idDict);
    }

    public RssType GetPhraseType() => RssType.Mixed;

    public async Task<GalgameCharacter?> GetGalgameCharacter(GalgameCharacter galgameCharacter)
    {
        return await _bgmPhraser.GetGalgameCharacter(galgameCharacter);
    }
}

public class MixedPhraserData : IGalInfoPhraserData
{
}