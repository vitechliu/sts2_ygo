using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Core.Cards;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Test;

/// <summary>
/// 不注册进任何卡池的同调流程夹具。测试代码可写入 EffectiveLevels 和
/// TreatedAsTuners，覆盖目标等级、素材等级与“视为调整”规则。
/// CardId 借用已有核心数据只用于构造 CardModel，不代表正式同调怪兽。
/// </summary>
[RegisterCard(typeof(SynchroCardPool))]
[RegisterCharacterStarterCard(typeof(RedhatCharacter), 3)]
public sealed class TestSynchroCard()
    : BaseExtraSynchroCard(-1, CardRarity.Basic, TargetType.None, false) {

    public override int CardId => 1861629;
    public int TargetLevel { get; set; } = 8;
    public Dictionary<int, int> EffectiveLevels { get; } = [];
    public HashSet<int> TreatedAsTuners { get; } = [];

    public override int? GetSynchroTargetLevel(CoreCard coreCard) => TargetLevel;

    public override int? GetSynchroMaterialLevel(
        CoreCard coreCard,
        SummonMaterial material
    ) {
        return material.CardId is { } cardId
            && EffectiveLevels.TryGetValue(cardId, out int level)
                ? level
                : base.GetSynchroMaterialLevel(coreCard, material);
    }

    public override bool IsSynchroTuner(
        CoreCard coreCard,
        SummonMaterial material
    ) {
        return material.CardId is { } cardId && TreatedAsTuners.Contains(cardId)
            || base.IsSynchroTuner(coreCard, material);
    }
}
