using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Monsters;
namespace VYgo.Scripts.Actions;
public class AccesscodeTalkerAction : TargetingAttackAction {
    protected override bool IsVisibleInternal => true;
    public override string? CustomIconPath => IntentIconPath;
    public override bool CanAct(ICombatState combatState) => base.CanAct(combatState)
        && Owner.PetOwner is { } player && PileType.Discard.GetPile(player).Cards.Any(card => card is BaseExtraLinkCard);
    protected override async Task OnAct(PlayerChoiceContext choiceContext, Creature? target) {
        if (target == null || Owner.PetOwner is not { } player) return;
        var selected = (await CardSelectCmd.FromCombatPile(choiceContext, PileType.Discard.GetPile(player), player,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), card => card is BaseExtraLinkCard)).FirstOrDefault();
        if (selected?.Pile?.Type != PileType.Discard) return;
        await CardCmd.Exhaust(choiceContext, selected);
        if (!Owner.IsAlive || !target.IsAlive) return;
        await DealMonsterDamage(choiceContext, target);
        if (Owner.Monster is BaseMonster monster) await monster.AfterAttack(choiceContext);
    }
}
