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
using VYgo.Utils;

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
                && pet.Monster is BaseMonster { SourceCard: BaseMonsterCard } monster
                && monster.IsRace(YgoRace.Cyberse))
            .ToUniqueSourceCardTargets(nameof(CapacitorStalkerMinion));
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
        //素材送墓时本怪兽已离场，CombatState 可能为 null，改用持有者所在战斗状态
        if (owner.Creature.CombatState is not { } combatState) return;

        await CreatureCmd.Damage(
            choiceContext,
            combatState.Creatures.Where(target => !target.IsPet).ToList(),
            sourceCard.GraveyardDamage,
            ValueProp.Unpowered,
            creature,
            sourceCard,
            null);
    }
}
