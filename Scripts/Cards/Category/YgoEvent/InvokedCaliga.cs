using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Cards.Category.YgoEvent;

[RegisterCard(typeof(YgoEventCardPool))]
public class InvokedCaliga()
    : BaseExtraFusionCard(-1, CardType.Skill, CardRarity.Event, TargetType.None) {
    private const string MaterialAttribute = "暗";

    public override int CardId => 13529466;
    public override int BaseAttackVar => 5;
    public override int BaseLifeVar => 8;
    public override int UpgradeLifeVar => 4;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..base.AdditionalHoverTips,
        HoverTipFactory.FromCard<AleistertheInvoker>(),
        HoverTipFactory.FromPower<InvokedCaligaBufferPower>()
    ];

    public override bool CanUseFusionMaterial(SummonMaterial material) {
        return InvokedFusionUtil.CanUseMaterial(material, MaterialAttribute);
    }

    public override bool HasValidFusionMaterials(IReadOnlyList<SummonMaterial> materials) {
        return base.HasValidFusionMaterials(materials)
            && InvokedFusionUtil.HasAleisterAndAttribute(materials, MaterialAttribute);
    }
}
