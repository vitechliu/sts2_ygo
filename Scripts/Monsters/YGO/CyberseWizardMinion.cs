using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Actions;
using VYgo.Scripts.Cards.Category.Playmaker;

namespace VYgo.Scripts.Monsters.YGO;

public class CyberseWizardMinion : BaseMonster {
    public override int CardId => 36033786;

    public override bool BasicAttackAction => false;

    public override Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options
    ) {
        if (options.Source is not CyberseWizard sourceCard) return Task.CompletedTask;

        return ApplyMonsterAction<CyberseWizardAction>(
            choiceContext,
            Creature,
            sourceCard.Weak,
            owner.Creature,
            sourceCard,
            true
        );
    }
}
