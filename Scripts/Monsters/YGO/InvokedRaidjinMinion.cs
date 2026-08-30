using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Orbs;
using VYgo.Scripts.Cards.Category.YgoEvent;

namespace VYgo.Scripts.Monsters.YGO;

public class InvokedRaidjinMinion : BaseMonster {
    public override int CardId => 49513164;

    public override async Task AfterAttack(PlayerChoiceContext choiceContext) {
        if (SourceCard is not InvokedRaidjin sourceCard
            || Creature.PetOwner is not { } owner) {
            return;
        }

        for (int i = 0; i < sourceCard.LightningCount; i++) {
            await OrbCmd.Channel<LightningOrb>(choiceContext, owner);
        }
    }
}
