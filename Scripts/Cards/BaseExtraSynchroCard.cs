using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using VYgo.Core;
using VYgo.Core.Cards;

namespace VYgo.Scripts.Cards;

/// <summary>
/// 同调怪兽的通用规则基类。默认规则是一只以上调整加一只以上非调整，
/// 全部素材来自己方场上，且有效等级总和等于目标怪兽等级。
/// </summary>
public abstract class BaseExtraSynchroCard(
    int baseCost,
    CardRarity rarity,
    TargetType target,
    bool showInCardLibrary = true)
    : BaseExtraCard(baseCost, rarity, target, showInCardLibrary),
        IDirectExtraDeckSummonCard {

    protected override YgoType CardYgoType => YgoType.synchro;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.SynchroSummon()
    ];

    public virtual int? GetSynchroTargetLevel(CoreCard coreCard) => coreCard.Level;

    public virtual int? GetSynchroMaterialLevel(
        CoreCard coreCard,
        SummonMaterial material
    ) => material.Level;

    public virtual bool IsSynchroTuner(
        CoreCard coreCard,
        SummonMaterial material
    ) => material.CoreCard?.IsTuner == true;

    public virtual bool CanUseSynchroMaterial(
        CoreCard coreCard,
        SummonMaterial material
    ) {
        return material.IsField
            && material.Creature is { IsAlive: true }
            && GetSynchroMaterialLevel(coreCard, material) is > 0;
    }

    public virtual int GetMinSynchroMaterialCount(CoreCard coreCard) => 2;

    public virtual int? GetMaxSynchroMaterialCount(CoreCard coreCard) => null;

    public virtual bool HasValidSynchroMaterials(
        CoreCard coreCard,
        IReadOnlyList<SummonMaterial> materials
    ) {
        int? targetLevel = GetSynchroTargetLevel(coreCard);
        if (targetLevel is not > 0
            || materials.Count < GetMinSynchroMaterialCount(coreCard)
            || GetMaxSynchroMaterialCount(coreCard) is { } maxCount
                && materials.Count > maxCount
            || materials.Any(material => !CanUseSynchroMaterial(coreCard, material))) {
            return false;
        }

        int tunerCount = 0;
        int nonTunerCount = 0;
        int levelSum = 0;
        foreach (SummonMaterial material in materials) {
            int? level = GetSynchroMaterialLevel(coreCard, material);
            if (level is not > 0) return false;
            levelSum += level.Value;
            if (IsSynchroTuner(coreCard, material)) tunerCount++;
            else nonTunerCount++;
        }

        return tunerCount > 0
            && nonTunerCount > 0
            && levelSum == targetLevel.Value;
    }

    public virtual DirectExtraDeckSummonSpec? CreateDirectExtraDeckSummonSpec(Player owner) {
        if (Owner != owner) return null;

        return SummonUtil.CreateDirectSynchroSummonSpec(
            this,
            owner,
            (_, _) => SummonUtil.GetFieldMonsterMaterials(owner)
        );
    }
}
