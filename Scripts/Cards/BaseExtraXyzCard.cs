using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using VYgo.Core;
using VYgo.Core.Cards;

namespace VYgo.Scripts.Cards;

public abstract class BaseExtraXyzCard(
    int baseCost,
    CardType type,
    CardRarity rarity,
    TargetType target,
    bool showInCardLibrary = true)
    : BaseExtraCard(baseCost, type, rarity, target, showInCardLibrary), IDirectExtraDeckSummonCard {

    protected override YgoType CardYgoType => YgoType.xyz;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.XyzSummon()
    ];

    public virtual int XyzMaterialCount => 2;
    public virtual int MinXyzMaterialCount => XyzMaterialCount;
    public virtual int? MaxXyzMaterialCount => XyzMaterialCount;

    public virtual int? GetXyzRank(CoreCard coreCard) => coreCard.Rank;

    public virtual bool CanUseXyzMaterial(
        CoreCard coreCard,
        SummonMaterial material
    ) {
        int? rank = GetXyzRank(coreCard);
        return rank is > 0
            && material.IsField
            && material.Card is not BaseTokenCard
            && material.Creature is { IsAlive: true }
            && material.Level == rank;
    }

    public virtual bool HasValidXyzMaterials(
        CoreCard coreCard,
        IReadOnlyList<SummonMaterial> materials
    ) {
        return materials.Count >= MinXyzMaterialCount
            && (MaxXyzMaterialCount is not { } maxCount || materials.Count <= maxCount)
            && materials.All(material => CanUseXyzMaterial(coreCard, material));
    }

    public virtual DirectExtraDeckSummonSpec? CreateDirectExtraDeckSummonSpec(Player owner) {
        if (Owner != owner) return null;

        return SummonUtil.CreateDirectXyzSummonSpec(
            this,
            owner,
            (_, _) => SummonUtil.GetFieldMonsterMaterials(owner)
        );
    }
}
