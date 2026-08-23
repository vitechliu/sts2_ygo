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
public class FiendsmithsSequence() : BaseFiendsmithEquipLinkCard {
    public override int CardId => 49867899;

    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 4;

    protected override int EquipLife => DynamicVars["EquipLife"].IntValue;
    protected override string GraveyardSelectionPromptKey =>
        "V_YGO_CARD_FIENDSMITHS_SEQUENCE.graveyardSelectionScreenPrompt";

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new LifeVar("EquipLife", 4)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..base.AdditionalHoverTips,
        YgoHoverTipConst.Action(),
        YgoHoverTipConst.FusionSummon()
    ];

    public override bool HasValidLinkMaterials(
        CoreCard coreCard,
        IReadOnlyList<SummonMaterial> materials
    ) {
        return base.HasValidLinkMaterials(coreCard, materials)
            && materials.Any(FiendsmithUtil.IsLightFiendMonster);
    }

    protected override void OnUpgrade() {
        base.OnUpgrade();
        DynamicVars["EquipLife"].UpgradeValueBy(4m);
    }
}
