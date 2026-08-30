using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Category.YgoEvent;

[RegisterCard(typeof(YgoEventCardPool))]
public class InvokedMagellanica()
    : BaseExtraFusionCard(-1, CardType.Skill, CardRarity.Event, TargetType.None) {
    private const string MaterialAttribute = "地";

    public override int CardId => 48791583;
    public override int BaseAttackVar => 15;
    public override int BaseLifeVar => 20;
    public override int UpgradeLifeVar => 4;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..base.AdditionalHoverTips,
        HoverTipFactory.FromCard<AleistertheInvoker>()
    ];

    public override bool CanUseFusionMaterial(SummonMaterial material) {
        return InvokedFusionUtil.CanUseMaterial(material, MaterialAttribute);
    }

    public override bool HasValidFusionMaterials(IReadOnlyList<SummonMaterial> materials) {
        return base.HasValidFusionMaterials(materials)
            && InvokedFusionUtil.HasAleisterAndAttribute(materials, MaterialAttribute);
    }
}
