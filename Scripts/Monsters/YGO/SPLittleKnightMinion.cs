using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Minion;
using VYgo.Core;
using VYgo.Scripts.Cards.Category.Link;

namespace VYgo.Scripts.Monsters.YGO;

public class SPLittleKnightMinion: BaseMonster {
    public override int CardId => 29301450;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options) {
        if (options.Source is not SPLittleKnight sourceCard) return;

        var target = owner.RunState.Rng.CombatTargets.NextItem(
            Creature.CombatState.HittableEnemies);
        if (target != null) {
            await BanishCmd.Banish(target, sourceCard.BanishAmount);
        }
    }
}
