using MegaCrit.Sts2.Core.Entities.Players;
using VYgo.Core.Cards;
using VYgo.Scripts.Monsters;

namespace VYgo.Core;

/// <summary>
/// “2星·2阶·连接2”规则判定。
/// 卡表（VYgo/db.json）把怪兽星级、超量阶级和连接数值都存放在 level 字段，
/// 因此 2星·2阶 统一按 CoreCard.Level == 2 判定，连接2 按 LinkCount == 2 判定。
/// </summary>
public static class YgoSummonRules {
    public static bool IsLevel2OrRank2(CoreCard? coreCard) => coreCard?.Level == 2;

    public static bool IsLink2(CoreCard? coreCard) => coreCard?.LinkCount == 2;

    public static bool IsLevel2Rank2OrLink2(CoreCard? coreCard) =>
        IsLevel2OrRank2(coreCard) || IsLink2(coreCard);

    /// <summary>场上是否存在 2星·2阶 的友方怪兽（用于卫星闪灵怪兽的特召条件）。</summary>
    public static bool ControlsLevel2OrRank2Monster(Player owner) =>
        owner.Creature.Pets.Any(pet =>
            pet.IsAlive
            && pet.Monster is BaseMonster monster
            && IsLevel2OrRank2(monster.YgoGetCore()));
}
