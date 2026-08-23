using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Cards.Category.Common;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Monsters.YGO;

public class SprightRedMinion: BaseMonster {
    public override int CardId => 75922381;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options
    ) {
        if (options.Source is not SprightRed sourceCard) return;

        await PowerCmd.Apply<NegatingPower>(
            choiceContext,
            Creature,
            sourceCard.Negating,
            owner.Creature,
            sourceCard);
    }
}
