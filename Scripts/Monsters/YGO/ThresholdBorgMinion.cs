using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MinionLib.Minion;
using VYgo.Scripts.Cards.Category.Playmaker;

namespace VYgo.Scripts.Monsters.YGO;

public class ThresholdBorgMinion: BaseMonster {
    public override int CardId => 31944175;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options) {
        if (options.Source is not ThresholdBorg sourceCard
            || Creature.CombatState is not { } combatState) {
            return;
        }

        foreach (var enemy in combatState.HittableEnemies.ToList()) {
            await PowerCmd.Apply<StrengthPower>(
                choiceContext,
                enemy,
                -sourceCard.StrengthLoss,
                owner.Creature,
                sourceCard);
        }
    }
}
