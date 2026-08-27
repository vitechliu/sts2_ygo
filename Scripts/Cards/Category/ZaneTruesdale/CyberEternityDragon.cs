using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using VYgo.Core;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberEternityDragon() : BaseExtraFusionCard(-1, CardType.Skill, CardRarity.Token, TargetType.None) {
    public override int CardId => 82315403;

    public override int BaseAttackVar => 5;
    public override int BaseLifeVar => 20;
    public override int UpgradeLifeVar => 5;

    // 「电子龙」怪兽＋机械族怪兽×2
    public override int FusionMaterialCount => 3;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new LifeVar("BoostLife", 5),
        new EnergyVar(1)
    ];

    public int BoostLife => DynamicVars["BoostLife"].IntValue;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..base.AdditionalHoverTips,
        YgoHoverTipConst.Action(),
        YgoHoverTipConst.Enhance(),
        EnergyHoverTip
    ];

    public override bool CanUseFusionMaterial(SummonMaterial material) {
        return material.NameEquals(YgoMaterialNames.电子龙)
            || material.CoreCard?.IsRace(YgoRace.Machine) == true;
    }

    public override bool HasValidFusionMaterials(IReadOnlyList<SummonMaterial> materials) {
        return base.HasValidFusionMaterials(materials)
            && materials.Any(material => material.NameEquals(YgoMaterialNames.电子龙));
    }

    protected override void OnUpgrade() {
        base.OnUpgrade();
        DynamicVars["BoostLife"].UpgradeValueBy(5);
    }
}
