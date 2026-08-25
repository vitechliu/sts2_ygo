using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Minion;
using VYgo.Scripts.Cards.Category.Playmaker;
using VYgo.Scripts.Powers;
using VYgo.Utils;

namespace VYgo.Scripts.Monsters.YGO;

public class ClockWyvernMinion: BaseMonster {
    public override int CardId => 21830679;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options) {
        if (owner.MinionCount() < owner.GetMaxMinionCount()) {
            CardModel token = owner.Creature.CombatState.CreateCard<ClockWyvernToken>(owner);
            await CardPileCmd.AddGeneratedCardToCombat(token, PileType.Play, owner);
            await CardCmd.AutoPlay(choiceContext, token, null);
        }

        if (options.Source is not ClockWyvern { IsUpgraded: false }
            || Creature.GetPower<AttackPower>() is not { Amount: > 0 } attackPower) {
            return;
        }

        int halvedAttack = attackPower.Amount / 2;
        await PowerCmd.ModifyAmount(
            choiceContext,
            attackPower,
            halvedAttack - attackPower.Amount,
            owner.Creature,
            options.Source);
    }
}
