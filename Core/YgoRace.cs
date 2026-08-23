using VYgo.Core.Cards;

namespace VYgo.Core;

/// <summary>
/// 游戏王怪兽种族。
/// </summary>
public enum YgoRace {
    Warrior,
    Insect,
    Machine,
    Dragon,
    Cyberse,
    Fiend,
}

public static class YgoRaceExtensions {
    /// <summary>
    /// 判断核心卡数据是否属于指定种族。
    /// </summary>
    public static bool IsRace(this CoreCard? card, YgoRace race) {
        return card?.Race == race.ToCoreValue();
    }

    /// <summary>
    /// 将种族枚举转换为 <c>db.json</c> 中使用的中文种族名称。
    /// </summary>
    public static string ToCoreValue(this YgoRace race) {
        return race switch {
            YgoRace.Warrior => "战士族",
            YgoRace.Insect => "昆虫族",
            YgoRace.Machine => "机械族",
            YgoRace.Dragon => "龙族",
            YgoRace.Cyberse => "电子界族",
            YgoRace.Fiend => "恶魔族",
            _ => throw new ArgumentOutOfRangeException(nameof(race), race, null),
        };
    }
}
