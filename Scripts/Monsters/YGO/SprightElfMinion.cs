using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Actions;
using VYgo.Scripts.Cards.Category.Common;

namespace VYgo.Scripts.Monsters.YGO;

public class SprightElfMinion: BaseMonster {
    public override int CardId => 27381364;

    public override bool BasicAttackAction => false;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options
    ) {
        if (options.Source is not SprightElf sourceCard) return;

        await PowerCmd.Apply<SprightElfAction>(
            choiceContext,
            Creature,
            1m,
            owner.Creature,
            sourceCard,
            true);
    }
}
