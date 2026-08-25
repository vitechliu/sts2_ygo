using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Cards.Category.Playmaker;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Monsters.YGO;

public class DecodeTalkerHeatsoulMinion: BaseMonster {
    public override int CardId => 61245672;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options) {
        if (options.Source is not DecodeTalkerHeatsoul sourceCard) return;
        await PowerCmd.Apply<HeatsoulDrawPower>(
            choiceContext,
            Creature,
            1m,
            owner.Creature,
            sourceCard,
            true);
    }

    public override async Task AfterAttack(PlayerChoiceContext choiceContext) {
        if (Creature.PetOwner is { } owner) {
            await CardPileCmd.Draw(choiceContext, 1, owner);
        }
    }
}
