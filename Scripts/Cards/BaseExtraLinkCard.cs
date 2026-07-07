using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using VYgo.Core;
using VYgo.Core.Cards;

namespace VYgo.Scripts.Cards;

public abstract class BaseExtraLinkCard(
    int baseCost,
    CardRarity rarity,
    TargetType target,
    bool showInCardLibrary = true)
    : BaseExtraCard(baseCost, rarity, target, showInCardLibrary) {
    
    protected override YgoType CardYgoType => YgoType.link;
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.LinkSummon()
    ];

    public virtual int GetLinkMaterialCount(CoreCard coreCard) {
        return Math.Max(1, coreCard.LinkCount ?? 1);
    }

    public virtual bool CanUseLinkMaterial(SummonMaterial material) {
        return true;
    }

    public virtual bool HasValidLinkMaterials(CoreCard coreCard, IReadOnlyList<SummonMaterial> materials) {
        return materials.Count >= GetLinkMaterialCount(coreCard);
    }
}
