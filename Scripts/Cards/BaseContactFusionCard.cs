using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using VYgo.Core;

namespace VYgo.Scripts.Cards;

/// <summary>
/// 可以只使用场上怪兽作为素材，从额外卡组直接进行融合召唤的怪兽。
/// </summary>
public abstract class BaseContactFusionCard(
    int baseCost,
    CardRarity rarity,
    TargetType target,
    bool showInCardLibrary = true)
    : BaseExtraFusionCard(baseCost, rarity, target, showInCardLibrary),
        IDirectExtraDeckSummonCard {

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..base.AdditionalHoverTips,
        YgoHoverTipConst.ContactFusion()
    ];

    public virtual DirectExtraDeckSummonSpec? CreateDirectExtraDeckSummonSpec(Player owner) {
        if (Owner != owner) return null;

        return SummonUtil.CreateDirectFusionSummonSpec(
            this,
            owner,
            _ => SummonUtil.GetFieldMonsterMaterials(owner)
        );
    }
}
