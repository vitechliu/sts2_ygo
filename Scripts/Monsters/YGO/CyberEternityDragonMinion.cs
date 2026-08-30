using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Actions;
using VYgo.Scripts.Cards.Category.ZaneTruesdale;

namespace VYgo.Scripts.Monsters.YGO;

public class CyberEternityDragonMinion : BaseMonster {
    public override int CardId => 82315403;

    public override bool BasicAttackAction => false;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options
    ) {
        if (options.Source is not CyberEternityDragon sourceCard) return;

        await ApplyMonsterAction<CyberEternityDragonAction>(
            choiceContext,
            Creature,
            sourceCard.BoostLife,
            owner.Creature,
            sourceCard,
            true
        );
    }
}
