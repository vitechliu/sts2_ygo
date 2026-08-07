using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using VYgo.Core;

namespace VYgo.Scripts.Cards;

public abstract class BaseExtraFusionCard(
    int baseCost,
    CardRarity rarity,
    TargetType target,
    bool showInCardLibrary = true)
    : BaseExtraCard(baseCost, rarity, target, showInCardLibrary) {

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.FusionSummon()
    ];
    protected override YgoType CardYgoType => YgoType.fusion;

    public virtual int FusionMaterialCount => 2;

    public virtual int MinFusionMaterialCount => FusionMaterialCount;

    // null means that every available material count at or above the minimum is allowed.
    public virtual int? MaxFusionMaterialCount => FusionMaterialCount;

    public virtual bool CanUseFusionMaterial(SummonMaterial material) {
        return true;
    }

    public virtual bool HasValidFusionMaterials(IReadOnlyList<SummonMaterial> materials) {
        return materials.Count >= MinFusionMaterialCount
            && (MaxFusionMaterialCount is not { } maxCount || materials.Count <= maxCount)
            && materials.All(CanUseFusionMaterial);
    }

    internal async Task<bool> InvokeAfterFusionSummoned(SummonPostPlayContext context) {
        await AfterFusionSummoned(context);
        return true;
    }

    protected virtual Task AfterFusionSummoned(SummonPostPlayContext context) {
        return Task.CompletedTask;
    }
}
