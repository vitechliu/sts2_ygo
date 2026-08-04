using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using VYgo.Scripts.Cards.Category.ZaneTruesdale;

namespace VYgo.Scripts.Monsters.YGO;

public class CyberPhoenixMinion: BaseMonster {
    public override int CardId => 3370104;

    protected override Task OnSendToGraveyard(
        PlayerChoiceContext choiceContext,
        Creature creature,
        Player owner) {
        decimal cards = SourceCard?.DynamicVars.Cards.BaseValue
            ?? CyberPhoenix.BaseDraw;
        return CardPileCmd.Draw(choiceContext, cards, owner);
    }
}
