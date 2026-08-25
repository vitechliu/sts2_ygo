using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(YgoEventCardPool))]
public class FiendsmithsLacrima()
    : BaseExtraFusionCard(-1, CardRarity.Event, TargetType.None) {
    public override int CardId => 46640168;

    public override int BaseAttackVar => 8;
    public override int BaseLifeVar => 8;

    public int GraveyardDamage => DynamicVars["GraveyardDamage"].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new DamageVar("GraveyardDamage", 8m, ValueProp.Unpowered)
    ];

    public override bool CanUseFusionMaterial(SummonMaterial material) {
        return FiendsmithUtil.IsLightFiendMonster(material);
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..base.AdditionalHoverTips,
        YgoHoverTipConst.SpecialSummon()
    ];

    protected override void OnUpgrade() {
        base.OnUpgrade();
        DynamicVars["GraveyardDamage"].UpgradeValueBy(5m);
    }
}
