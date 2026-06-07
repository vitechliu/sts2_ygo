using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using VYgo.Scripts.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Single;

[RegisterCard(typeof(RedhatCardPool))]
public class CyberDragon() : BaseVYgoCard(energyCost, type, rarity, targetType, shouldShowInCardLibrary) {
    public override int CardId => 70095154;
    
    
    private const int energyCost = 1;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Common;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;


    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.FromPower<VigorPower>(),
        HoverTipFactory.FromPower<StarscourgePower>(),
    ];
    
    
    // protected override IEnumerable<IHoverTip> ExtraHoverTips => new List<IHoverTip>();{
    //     HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
    // }

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        // new StarsVar(1),
        // new CalculationBaseVar(0m),
        // new CalculationExtraVar(15m),
        // new PowerVar<VigorPower>(15m),
        // new CalculatedVar("CalculatedVigor").WithMultiplier((_, c) => GetStarCards(c.Player).Count())
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        
    }

    protected override void OnUpgrade() {
        // base.DynamicVars["VigorPower"].UpgradeValueBy(5);
    }

}
