using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using VYgo.Core;
using VYgo.Core.Cards;

namespace VYgo.Scripts.Cards;

public abstract class BaseExtraLinkCard(
    int baseCost,
    CardRarity rarity,
    TargetType target,
    bool showInCardLibrary = true)
    : BaseExtraCard(baseCost, rarity, target, showInCardLibrary), IDirectExtraDeckSummonCard {
    
    protected override YgoType CardYgoType => YgoType.link;
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.LinkSummon()
    ];

    /// <summary>
    /// 卡片召唤条件要求的最少实际怪兽数。
    /// 这与必须精确凑满的 LINK 值是两个独立限制。
    /// </summary>
    public virtual int GetLinkMaterialCount(CoreCard coreCard) {
        return Math.Max(1, coreCard.LinkCount ?? 1);
    }

    public virtual int GetMinLinkMaterialCount(CoreCard coreCard) {
        return GetLinkMaterialCount(coreCard);
    }

    // null means that every available material count at or above the minimum is allowed.
    public virtual int? GetMaxLinkMaterialCount(CoreCard coreCard) {
        // 召唤条件只限制实际怪兽数；总连接值由下方的独立规则校验。
        // 因此像「2只以上」的 LINK-3 可以选 2 只或 3 只怪兽。
        return Math.Max(1, coreCard.LinkCount ?? GetLinkMaterialCount(coreCard));
    }

    public virtual bool CanUseLinkMaterial(SummonMaterial material) {
        return true;
    }

    public virtual bool HasValidLinkMaterials(CoreCard coreCard, IReadOnlyList<SummonMaterial> materials) {
        return materials.Count >= GetMinLinkMaterialCount(coreCard)
            && (GetMaxLinkMaterialCount(coreCard) is not { } maxCount || materials.Count <= maxCount)
            && SummonUtil.HasExactLinkMaterialValue(coreCard, materials)
            && materials.All(CanUseLinkMaterial);
    }

    public virtual DirectExtraDeckSummonSpec? CreateDirectExtraDeckSummonSpec(Player owner) {
        if (Owner != owner) return null;

        return new DirectExtraDeckSummonSpec(
            BuildMaterialSelection: () => SummonUtil.BuildLinkMaterialSelection(
                this,
                owner,
                (_, _) => SummonUtil.GetFieldMonsterMaterials(owner)
            ),
            PlayAnimation: ExtraDeckSummonAnimations.PlayLinkSummonAnimation,
            AfterAutoPlay: SummonUtil.TriggerLinkMaterialEffects,
            FinalWaitSeconds: 0.8f
        );
    }
}
