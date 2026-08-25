using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Scripts.Cards.Category.Playmaker;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Monsters.YGO;

public class EncodeTalkerMinion: BaseMonster {
    public override int CardId => 6622715;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options) {
        if (options.Source is not EncodeTalker sourceCard) return;

        foreach (var monster in owner.Creature.Pets.Where(pet => pet.IsAlive).ToList()) {
            await PowerCmd.Apply<UntilNextTurnBattleDestructionProtectionPower>(
                choiceContext,
                monster,
                1m,
                owner.Creature,
                sourceCard,
                true);
        }
    }
}
