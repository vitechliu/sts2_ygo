using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Cards.Category.Playmaker;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Monsters.YGO;

public class ExcodeTalkerMinion: BaseMonster {
    public override int CardId => 40669071;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options) {
        if (options.Source is not ExcodeTalker sourceCard) return;

        await PowerCmd.Apply<MinionCapacityReductionPower>(
            choiceContext,
            owner.Creature,
            2m,
            Creature,
            sourceCard,
            true);
    }
}
