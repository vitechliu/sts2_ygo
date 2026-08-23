using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Core.Cards;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(CommonCardPool))]
public class FiendsmithsRequiem() : BaseFiendsmithEquipLinkCard {
    public override int CardId => 2463794;

    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 2;

    protected override int EquipAttack => DynamicVars["EquipAttack"].IntValue;
    protected override string GraveyardSelectionPromptKey =>
        "V_YGO_CARD_FIENDSMITHS_REQUIEM.graveyardSelectionScreenPrompt";

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new AttackVar("EquipAttack", 3)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..base.AdditionalHoverTips,
        YgoHoverTipConst.Action(),
        YgoHoverTipConst.SpecialSummon()
    ];

    public override bool CanUseLinkMaterial(SummonMaterial material) {
        return FiendsmithUtil.IsLightFiendMonster(material);
    }

    protected override void OnUpgrade() {
        base.OnUpgrade();
        DynamicVars["EquipAttack"].UpgradeValueBy(3m);
    }
}
