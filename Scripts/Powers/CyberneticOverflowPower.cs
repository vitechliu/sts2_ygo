using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using VYgo.Core;
using VYgo.Scripts.Cards;

namespace VYgo.Scripts.Powers;

[RegisterPower]
public class CyberneticOverflowPower : BaseActionPower {
    private sealed class Data {
        public CardModel? Source;
        public int SetTurn;
    }
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://images/powers/covered_power.png", BigIconPath: "res://images/powers/covered_power.png");
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.SetCard(), YgoHoverTipConst.PowerAction(), HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
    ];
    protected override object InitInternalData() => new Data();
    public override Task AfterApplied(Creature? applier, CardModel? cardSource) {
        var data = GetInternalData<Data>();
        data.Source = cardSource;
        data.SetTurn = Owner.Player?.PlayerCombatState.TurnNumber ?? 0;
        return Task.CompletedTask;
    }
    public override bool CanExecuteRightClick(ModRightClickExecutionContext context) =>
        base.CanExecuteRightClick(context) && GetInternalData<Data>().Source != null
        && context.Player.PlayerCombatState.TurnNumber > GetInternalData<Data>().SetTurn;

    private static bool IsMaterial(CardModel card) => card is BaseMonsterCard monster
        && monster.YgoGetCore() is { Attribute: "光" } core && core.IsRace(YgoRace.Machine);

    protected override async Task<bool> OnAction(ModRightClickExecutionContext context) {
        if (context.PlayerChoiceContext is not { } choice || GetInternalData<Data>().Source is not { } source) return false;
        var player = context.Player;
        var combat = player.Creature.CombatState;
        var damage = Amount;
        try {
            Flash();
            await PowerCmd.Remove(this);
            await CardPileCmd.Add(source, PileType.Discard);
            var pile = PileType.Discard.GetPile(player);
            var selected = (await CardSelectCmd.FromCombatPile(choice, pile, player,
                new CardSelectorPrefs(SelectionScreenPrompt, 0, pile.Cards.Count(IsMaterial)), IsMaterial)).ToList();
            int count = 0;
            foreach (var card in selected) {
                if (card.Pile != pile || !IsMaterial(card)) continue;
                await CardCmd.Exhaust(choice, card);
                if (card.Pile?.Type == PileType.Exhaust) count++;
            }
            for (int i = 0; i < count; i++) {
                var enemies = combat?.HittableEnemies.ToList();
                if (enemies == null || enemies.Count == 0) break;
                var enemy = player.RunState.Rng.CombatTargets.NextItem(enemies);
                await CreatureCmd.Damage(choice, enemy, damage, ValueProp.Unpowered, player.Creature, source, null);
            }
            return true;
        }
        finally { InvokeExecutionFinished(); }
    }
}
