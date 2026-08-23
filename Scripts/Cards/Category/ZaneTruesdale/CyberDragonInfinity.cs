using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Core.Cards;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Powers;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
// [RegisterCharacterStarterCard(typeof(ZaneTruesdaleCharacter), 1)]
public class CyberDragonInfinity()
    : BaseExtraXyzCard(energyCost, rarity, targetType, shouldShowInCardLibrary) {
    public override int CardId => 10443957;
    public override int XyzMaterialCount => 1;
    
    private const int energyCost = -1;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new PowerVar<NegatingPower>(1m)
    ];
    
    public override int BaseAttackVar => 8;
    public override int BaseLifeVar => 5;
    public override int UpgradeAttackVar => 2;
    public override int UpgradeLifeVar => 2;
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        HoverTipFactory.FromPower<NegatingPower>(),
        HoverTipFactory.FromCard<CyberDragonNova>()
    ];
    
    public override bool CanUseXyzMaterial(CoreCard coreCard, SummonMaterial material) {
        return material.Card is CyberDragonNova;
    }

    protected override void OnUpgrade() {
        base.OnUpgrade();
        DynamicVars["NegatingPower"].UpgradeValueBy(1m);
    }
}
