using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.CyberDragon;

[RegisterCard(typeof(RedhatCardPool))]
[RegisterCharacterStarterCard(typeof(RedhatCharacter), 1)]
public class CyberEndDragon() : BaseExtraFusionCard(energyCost, rarity, targetType, shouldShowInCardLibrary) {
    public override int CardId => 1546123;
    public override int FusionMaterialCount => 3;
    
    private const int energyCost = 1;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;


    // protected override IEnumerable<IHoverTip> ExtraHoverTips => new List<IHoverTip>();{
    //     HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
    // }

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(25),
        new LifeVar(12)
    ];
    
    protected override void OnUpgrade() {
        DynamicVars["Life"].UpgradeValueBy(5);
        DynamicVars["Attack"].UpgradeValueBy(5);
    }
}
