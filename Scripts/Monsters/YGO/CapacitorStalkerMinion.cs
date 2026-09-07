using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
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

        //素材送墓时本怪兽已被 MinionLib 移出战斗，creature.CombatState 为 null。
        //战斗状态按可用性逐级回退，保证伤害在战斗中必定执行。
        ICombatState? combatState = creature.CombatState
            ?? owner.Creature.CombatState
            ?? (owner.RunState?.CurrentRoom as CombatRoom)?.CombatState;
        if (combatState == null) {
            Entry.Logger.Warn(
                "CapacitorStalkerMinion: 送墓时找不到可用战斗状态，跳过送墓伤害。"
            );
            return;
        }

        //本怪兽已死亡，不能作为伤害来源（CreatureCmd.Damage 会跳过死亡来源），改为无来源伤害。
        await CreatureCmd.Damage(
            choiceContext,
            combatState.Creatures.Where(target => !target.IsPet).ToList(),
            sourceCard.GraveyardDamage,
            ValueProp.Unpowered,
            null,
            sourceCard,
            null);
    }
}
