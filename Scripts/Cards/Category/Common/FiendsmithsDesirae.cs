using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Powers;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(EventCardPool))]
public class FiendsmithsDesirae()
    : BaseExtraFusionCard(-1, CardRarity.Event, TargetType.None) {
    public override int CardId => 82135803;

    public override int BaseAttackVar => 9;
    public override int BaseLifeVar => 8;
    public override int FusionMaterialCount => 3;

    public int Negating => DynamicVars["NegatingPower"].IntValue;
    public int GraveyardDamage => DynamicVars["GraveyardDamage"].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new PowerVar<NegatingPower>(3m),
        new DamageVar("GraveyardDamage", 16m, ValueProp.Unpowered)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..base.AdditionalHoverTips,
        HoverTipFactory.FromPower<NegatingPower>()
    ];

    public override bool CanUseFusionMaterial(SummonMaterial material) {
        return FiendsmithUtil.IsLightFiendMonster(material);
    }

    public override bool HasValidFusionMaterials(IReadOnlyList<SummonMaterial> materials) {
        return base.HasValidFusionMaterials(materials)
            && materials.Any(material => material.CardId == 60764609);
    }

    protected override void OnUpgrade() {
        base.OnUpgrade();
        DynamicVars["NegatingPower"].UpgradeValueBy(2m);
        DynamicVars["GraveyardDamage"].UpgradeValueBy(8m);
    }
}
