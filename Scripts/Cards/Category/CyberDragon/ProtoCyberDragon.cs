using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Commands;
using MinionLib.Minion;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Monsters.YGO;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Var;
using VYgo.Utils;

namespace VYgo.Scripts.Cards.Category.CyberDragon;

[RegisterCard(typeof(RedhatCardPool))]
[RegisterCharacterStarterCard(typeof(RedhatCharacter), 1)]
public class ProtoCyberDragon() : BaseMonsterCard(energyCost,rarity, targetType, shouldShowInCardLibrary) {
    public override int CardId => 26439287;

    private const int energyCost = 0;
    private const CardRarity rarity = CardRarity.Common;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    //
    // protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
    //     // HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
    //     // HoverTipFactory.FromPower<VigorPower>(),
    //     // HoverTipFactory.FromPower<StarscourgePower>(),
    // ];


    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(3),
        new LifeVar(2)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        _ = await MinionCmd.AddMinion<ProtoCyberDragonMinion>(choiceContext, Owner, new MinionSummonOptions(
            MaxHp: Life,
            PrimaryStatAmount: Attack,
            Source: this,
            Position: MinionPosition.Front)
        );
    }

    protected override void OnUpgrade() {
        DynamicVars["Life"].UpgradeValueBy(1);
        DynamicVars["Attack"].UpgradeValueBy(1);
    }
}