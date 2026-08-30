using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Orbs;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.YgoEvent;

[RegisterCard(typeof(YgoEventCardPool))]
public class InvokedRaidjin()
    : BaseExtraFusionCard(-1, CardType.Skill, CardRarity.Event, TargetType.None) {
    private const string MaterialAttribute = "风";

    public override int CardId => 49513164;
    public override int BaseAttackVar => 9;
    public override int BaseLifeVar => 12;

    public int LightningCount => DynamicVars["Lightning"].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new DynamicVar("Lightning", 1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..base.AdditionalHoverTips,
        HoverTipFactory.FromCard<AleistertheInvoker>(),
        HoverTipFactory.Static(StaticHoverTip.Channeling),
        HoverTipFactory.FromOrb<LightningOrb>()
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
        DynamicVars["Lightning"].UpgradeValueBy(1m);
    }
}
