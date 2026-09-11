using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using VYgo.Core;
using VYgo.Scripts.Monsters;
using VYgo.Scripts.Cards;
namespace VYgo.Scripts.Powers;
[RegisterPower]
public class CynetConflictPower : BaseActionPower {
    private sealed class Data { public CardModel? Source; public int SetTurn; }
    protected override object InitInternalData() => new Data();
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override PowerAssetProfile AssetProfile => new(IconPath: "res://images/powers/covered_power.png", BigIconPath: "res://images/powers/covered_power.png");
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Banish", 10)];
    public override Task AfterApplied(Creature? applier, CardModel? cardSource) {
        GetInternalData<Data>().Source = cardSource;
        GetInternalData<Data>().SetTurn = Owner.Player?.PlayerCombatState.TurnNumber ?? 0;
        return Task.CompletedTask;
    }
    public override bool CanExecuteRightClick(ModRightClickExecutionContext context) => base.CanExecuteRightClick(context)
        && context.Player.PlayerCombatState.TurnNumber > GetInternalData<Data>().SetTurn;
    protected override async Task<bool> OnAction(ModRightClickExecutionContext context) {
        if (context.PlayerChoiceContext is not { } choiceContext || GetInternalData<Data>().Source is not { } source) return false;
        var player = context.Player;
        int count = player.Creature.Pets.Count(pet => pet.IsAlive && pet.Monster is BaseMonster { SourceCard: BaseVYgoCard card }
            && card.ContainArchetype(YgoArchetypes.CodeTalker));
        var combat = Owner.CombatState;
        await PowerCmd.Remove(this);
        await CardPileCmd.Add(source, PileType.Discard);
        if (count > 0) await PowerCmd.Apply<NegatingPower>(choiceContext, player.Creature, count, player.Creature, source);
        for (int i = 0; i < count; i++) {
            var enemies = combat?.HittableEnemies.Where(enemy => enemy.IsAlive).ToList();
            if (enemies == null || enemies.Count == 0) break;
            await BanishCmd.Banish(player.RunState.Rng.CombatTargets.NextItem(enemies), DynamicVars["Banish"].BaseValue);
        }
        return true;
    }
}
