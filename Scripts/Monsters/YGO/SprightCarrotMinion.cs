using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Actions;
using VYgo.Scripts.Cards.Category.Common;

namespace VYgo.Scripts.Monsters.YGO;

public class SprightCarrotMinion: BaseMonster {
    public override int CardId => 2311090;

    public override bool BasicAttackAction => false;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options
    ) {
        if (options.Source is not SprightCarrot sourceCard) return;

        await ApplyMonsterAction<SprightCarrotAction>(
            choiceContext,
            Creature,
            1m,
            owner.Creature,
            sourceCard,
            true);
    }
}
