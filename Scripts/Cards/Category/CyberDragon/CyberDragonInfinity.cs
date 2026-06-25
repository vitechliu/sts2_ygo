using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MinionLib.Commands;
using MinionLib.Minion;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Monsters.YGO;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.CyberDragon;

[RegisterCard(typeof(RedhatCardPool))]
[RegisterCharacterStarterCard(typeof(RedhatCharacter), 1)]
public class CyberDragonInfinity() : BaseExtraCard(energyCost, rarity, targetType, shouldShowInCardLibrary) {
    public override int CardId => 10443957;
    protected override YgoType CardYgoType => YgoType.xyz;
    
    private const int energyCost = 1;
    private const CardRarity rarity = CardRarity.Common;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;


    // protected override IEnumerable<IHoverTip> ExtraHoverTips => new List<IHoverTip>();{
    //     HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
    // }

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(5),
        new LifeVar(4)
    ];
    
    protected override void OnUpgrade() {
        DynamicVars["Life"].UpgradeValueBy(1);
        DynamicVars["Attack"].UpgradeValueBy(1);
    }
}