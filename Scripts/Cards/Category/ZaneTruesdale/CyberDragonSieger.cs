using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Core.Cards;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Powers;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
[RegisterCharacterStarterCard(typeof(ZaneTruesdaleCharacter), 1)]
public class CyberDragonSieger() : BaseExtraLinkCard(energyCost, CardType.Attack, rarity, targetType, shouldShowInCardLibrary) {
    public override int CardId => 46724542;
    
    private const int energyCost = -1;
    private const CardRarity rarity = CardRarity.Basic;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new PowerVar<CyberDragonSiegerTemporaryAttackPower>("BoostAttack", BaseAttackVar)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..base.AdditionalHoverTips,
        YgoHoverTipConst.Action(),
        YgoHoverTipConst.Enhance(),
        YgoHoverTipConst.NameAs(YgoMaterialNames.电子龙)
    ];

    public override YgoMaterialNames? MaterialCardName => YgoMaterialNames.电子龙;

    public int BoostAttack => DynamicVars["BoostAttack"].IntValue;

    public override int BaseAttackVar => 5;
    public override int BaseLifeVar => 1;
    public override int UpgradeAttackVar => 2;
    public override int UpgradeLifeVar => 0;

    public override bool CanUseLinkMaterial(SummonMaterial material) {
        return material.CoreCard.IsRace(YgoRace.Machine);
    }

    public override bool HasValidLinkMaterials(
        CoreCard coreCard,
        IReadOnlyList<SummonMaterial> materials
    ) {
        return base.HasValidLinkMaterials(coreCard, materials)
            && materials.Any(material => material.NameEquals(YgoMaterialNames.电子龙));
    }

    protected override void OnUpgrade() {
        base.OnUpgrade();
        DynamicVars["BoostAttack"].UpgradeValueBy(UpgradeAttackVar);
    }
}
