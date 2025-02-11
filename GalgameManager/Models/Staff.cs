using GalgameManager.Enums;
using LiteDB;

namespace GalgameManager.Models;

public class Staff
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string?[] Ids { get; set; } = new string[Galgame.PhraserNumber];
    public string? JapaneseName { get; set; }
    public string? EnglishName { get; set; }
    public string? ChineseName { get; set; }
    public Gender Gender { get; set; }
    public List<Career> Career { get; set; } = [];
    public string? ImagePath { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public DateTime? BirthDate { get; set; }
    public List<StaffGame> Games { get; set; } = [];

    public string? Name => JapaneseName ?? ChineseName ?? EnglishName;

    public override string ToString() => Name ?? "Unknown";

    public StaffIdentifier GetIdentifier() => new()
    {
        Ids = Ids,
        JapaneseName = JapaneseName,
        EnglishName = EnglishName,
        ChineseName = ChineseName
    };

    /// 如果已经存在则更新relation
    public void AddGame(Galgame game, Career relation)
    {
        StaffGame? tmp = Games.FirstOrDefault(g => g.GameId == game.Uuid);
        if (tmp is not null)
        {
            tmp.Relation = relation;
            return;
        }
        Games.Add(new StaffGame {Game = game, Relation = relation});
    }
}

public class StaffGame
{
    [BsonIgnore] public Galgame Game { get; set; } = null!;
    public Career Relation { get; set; }

    #region LiteDB

    public Guid GameId
    {
        get => Game.Uuid;
        set => LoadedGameId = value;
    }

    public Guid LoadedGameId { get; private set; } = Guid.Empty;

    #endregion
}

public class StaffRelation : Staff
{
    public Career Relation { get; set; }
}

public class StaffIdentifier
{
    public string?[] Ids { get; set; } = new string[Galgame.PhraserNumber];
    public string? JapaneseName { get; set; }
    public string? EnglishName { get; set; }
    public string? ChineseName { get; set; }

    public int Match(Staff staff)
    {
        var score = 0;
        score += JapaneseName is not null && staff.JapaneseName == JapaneseName ? 1 : 0;
        score += EnglishName is not null && staff.EnglishName == EnglishName ? 1 : 0;
        score += ChineseName is not null && staff.ChineseName == ChineseName ? 1 : 0;
        for (var i = 0; i < Galgame.PhraserNumber; i++)
            score += Ids[i] is not null && staff.Ids[i] == Ids[i] ? 1 : 0;
        return score;
    }
}