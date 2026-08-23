using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Actions;
using VYgo.Scripts.Cards.Category.Common;

namespace VYgo.Scripts.Monsters.YGO;

public class FiendsmithsSequenceMinion: BaseMonster {
    public override int CardId => 49867899;
    public override bool BasicAttackAction { get; set; } = false;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options
    ) {
        if (options.Source is not FiendsmithsSequence sourceCard) return;

        await PowerCmd.Apply<FiendsmithsSequenceAction>(
            choiceContext,
            Creature,
            1m,
            owner.Creature,
            sourceCard,
            true);
    }
}
