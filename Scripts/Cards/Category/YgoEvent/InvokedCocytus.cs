using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.YgoEvent;

[RegisterCard(typeof(YgoEventCardPool))]
public class InvokedCocytus()
    : BaseExtraFusionCard(-1, CardType.Skill, CardRarity.Event, TargetType.None) {
    private const string MaterialAttribute = "水";

    public override int CardId => 85908279;
    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 22;

    public int Thorns => DynamicVars["ThornsPower"].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new PowerVar<ThornsPower>(5m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..base.AdditionalHoverTips,
        HoverTipFactory.FromCard<AleistertheInvoker>(),
        HoverTipFactory.FromPower<ThornsPower>()
    ];

    public override bool CanUseFusionMaterial(SummonMaterial material) {
        return InvokedFusionUtil.CanUseMaterial(material, MaterialAttribute);
    }

    public override bool HasValidFusionMaterials(IReadOnlyList<SummonMaterial> materials) {
        return base.HasValidFusionMaterials(materials)
            && InvokedFusionUtil.HasAleisterAndAttribute(materials, MaterialAttribute);
    }

    protected override void OnUpgrade() {
        base.OnUpgrade();
        DynamicVars["ThornsPower"].UpgradeValueBy(3m);
    }
}
