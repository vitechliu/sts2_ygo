using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Minion;
using VYgo.Core;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.Playmaker;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Monsters.YGO;

public class CapacitorStalkerMinion: BaseMonster {
    public override int CardId => 29716911;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options
    ) {
        if (options.Source is not CapacitorStalker sourceCard) return;

        Dictionary<CardModel, Creature> targets = owner.Creature.Pets
            .Where(pet => pet != Creature
                && pet.Monster is BaseMonster { SourceCard: BaseMonsterCard card }
                && card.YgoGetCore().IsRace(YgoRace.Cyberse))
            .ToDictionary(
                pet => ((BaseMonster)pet.Monster!).SourceCard!,
                pet => pet);
        if (targets.Count == 0) return;

        CardModel? selected = (await CardSelectCmd.FromCombatPile(
                choiceContext,
                Entry.MonsterPile.GetPile(owner),
                owner,
                new CardSelectorPrefs(sourceCard.SelectionScreenPrompt, 1),
                targets.ContainsKey))
            .FirstOrDefault();
        if (selected != null && targets.TryGetValue(selected, out Creature? target)) {
            await PowerCmd.Apply<AttackPower>(
                choiceContext,
                target,
                sourceCard.BoostAttack,
                Creature,
                sourceCard);
        }
    }

    protected override async Task OnSendToGraveyard(
        PlayerChoiceContext choiceContext,
        Creature creature,
        Player owner
    ) {
        if (SourceCard is not CapacitorStalker sourceCard) return;

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
