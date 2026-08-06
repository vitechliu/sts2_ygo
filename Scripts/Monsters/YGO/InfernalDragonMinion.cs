using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace VYgo.Scripts.Monsters.YGO;

public class InfernalDragonMinion: BaseMonster {
    public override int CardId => 47754278;

    protected override async Task OnSendToGraveyard(
        PlayerChoiceContext choiceContext,
        Creature creature,
        Player owner) {
        if (SourceCard != null) {
            await CardPileCmd.Add(SourceCard, PileType.Hand);
        }
    }
}
