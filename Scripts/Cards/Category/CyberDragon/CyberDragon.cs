using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MinionLib.Commands;
using MinionLib.Minion;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Monsters.YGO;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Category.CyberDragon;

[RegisterCard(typeof(RedhatCardPool))]
[RegisterCharacterStarterCard(typeof(RedhatCharacter), 3)]
public class CyberDragon() : BaseVYgoCard(energyCost, type, rarity, targetType, shouldShowInCardLibrary) {
    public override int CardId => 70095154;
    
    private const int energyCost = 2;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Common;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;


    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        // HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        // HoverTipFactory.FromPower<VigorPower>(),
        // HoverTipFactory.FromPower<StarscourgePower>(),
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
        _ = await MinionCmd.AddMinion<CyberDragonMinion>(choiceContext, Owner, new MinionSummonOptions(
            MaxHp: 1m,  // 血量
            PrimaryStatAmount: 2m, // 主要参数（具体内容在随从的 OnSummon 里定义），还有次要参数等可以按需传入
            Source: this,  // 召唤来源（通常是这张牌）
            Position: MinionPosition.Front)
        );
    }

    protected override void OnUpgrade() {
        // base.DynamicVars["VigorPower"].UpgradeValueBy(5);
    }

}
