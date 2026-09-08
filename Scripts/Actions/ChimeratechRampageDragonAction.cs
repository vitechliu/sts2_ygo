using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using VYgo.Core;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Monsters;

namespace VYgo.Scripts.Actions;

public sealed class ChimeratechRampageDragonAction : BasePerTurnMonsterAction {
    protected override bool IsVisibleInternal => true;
    public override TargetType TargetType => TargetType.None;
    protected override string? IntentIconPath => "res://images/packed/intents/attack/intent_attack_3.png";
    public override string? CustomIconPath => IntentIconPath;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10, ValueProp.Unpowered), new CardsVar(2)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [YgoHoverTipConst.Action()];
    private static bool IsMaterial(CardModel card) => card is BaseMonsterCard monster
        && monster.YgoGetCore() is { Attribute: "光" } core && core.IsRace(YgoRace.Machine);

    protected override async Task OnAct(PlayerChoiceContext choiceContext, Creature? target) {
        if (Owner.PetOwner is not { } player || Owner.Monster is not BaseMonster { SourceCard: { } source }) return;
        var combat = Owner.CombatState;
        try {
            var pile = PileType.Draw.GetPile(player);
            var selected = (await CardSelectCmd.FromCombatPile(choiceContext, pile, player,
                new CardSelectorPrefs(SelectionScreenPrompt, 0, DynamicVars.Cards.IntValue), IsMaterial)).ToList();
            if (selected.Count == 0) return;
            SpendUses();
            // 先完成送墓，再按选择数量逐次重新确定随机目标。
            foreach (var card in selected) {
                if (card.Pile == pile && IsMaterial(card)) await CardCmd.Discard(choiceContext, card);
            }
            for (int i = 0; i < selected.Count; i++) {
                var enemies = combat.HittableEnemies.ToList();
                if (enemies.Count == 0) break;
                var enemy = player.RunState.Rng.CombatTargets.NextItem(enemies);
                await CreatureCmd.Damage(choiceContext, enemy, DynamicVars.Damage.BaseValue,
                    ValueProp.Unpowered, player.Creature, source, null);
            }
        }
        finally { InvokeExecutionFinished(); }
    }
}
