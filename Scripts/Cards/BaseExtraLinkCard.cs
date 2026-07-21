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

    public virtual int GetLinkMaterialCount(CoreCard coreCard) {
        return Math.Max(1, coreCard.LinkCount ?? 1);
    }

    public virtual int GetMinLinkMaterialCount(CoreCard coreCard) {
        return GetLinkMaterialCount(coreCard);
    }

    // null means that every available material count at or above the minimum is allowed.
    public virtual int? GetMaxLinkMaterialCount(CoreCard coreCard) {
        return GetLinkMaterialCount(coreCard);
    }

    public virtual bool CanUseLinkMaterial(SummonMaterial material) {
        return true;
    }

    public virtual bool HasValidLinkMaterials(CoreCard coreCard, IReadOnlyList<SummonMaterial> materials) {
        return materials.Count >= GetMinLinkMaterialCount(coreCard)
            && (GetMaxLinkMaterialCount(coreCard) is not { } maxCount || materials.Count <= maxCount)
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
            FinalWaitSeconds: 0.8f
        );
    }
}
