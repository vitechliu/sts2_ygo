using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Minion;
using VYgo.Scripts.Cards.Category.Common;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Monsters.YGO;

public class FiendsmithsDesiraeMinion: BaseMonster {
    public override int CardId => 82135803;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options
    ) {
        if (options.Source is not FiendsmithsDesirae sourceCard) return;

        await PowerCmd.Apply<NegatingPower>(
            choiceContext,
            Creature,
            sourceCard.Negating,
            owner.Creature,
            sourceCard);
    }

    protected override async Task OnSendToGraveyard(
        PlayerChoiceContext choiceContext,
        Creature creature,
        Player owner
    ) {
        if (SourceCard is not FiendsmithsDesirae sourceCard) return;

        await CreatureCmd.Damage(
            choiceContext,
            creature.CombatState.Creatures.Where(target => !target.IsPet).ToList(),
            sourceCard.GraveyardDamage,
            ValueProp.Unpowered,
            creature,
            sourceCard,
            null);
    }
}
