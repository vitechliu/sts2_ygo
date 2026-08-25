using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Monsters;
using VYgo.Utils;

namespace VYgo.Scripts.Actions;

public sealed class LinkSpiderAction : BasePerTurnMonsterAction {
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.Action(),
        YgoHoverTipConst.SpecialSummon()
    ];

    protected override bool IsVisibleInternal => true;
    public override TargetType TargetType => TargetType.None;
    protected override string? IntentIconPath => "res://VYgo/images/powers/reborn.png";
    public override string? CustomIconPath => IntentIconPath;

    public override bool CanAct(ICombatState combatState) {
        return base.CanAct(combatState)
            && Owner.PetOwner is { } player
            && player.MinionCount() < player.GetMaxMinionCount()
            && PileType.Hand.GetPile(player).Cards.Any(IsNormalMonster);
    }

    protected override async Task OnAct(
        PlayerChoiceContext choiceContext,
        Creature? target
    ) {
        if (Owner.PetOwner is not { } player
            || Owner.Monster is not BaseMonster { SourceCard: { } sourceCard }
            || player.MinionCount() >= player.GetMaxMinionCount()) {
            return;
        }

        BaseMonsterCard? selected = (await CardSelectCmd.FromHand(
                choiceContext,
                player,
                new CardSelectorPrefs(SelectionScreenPrompt, 1),
                IsNormalMonster,
                sourceCard))
            .OfType<BaseMonsterCard>()
            .FirstOrDefault();
        if (selected == null || player.MinionCount() >= player.GetMaxMinionCount()) return;

        SpendUses();
        await CardCmd.AutoPlay(choiceContext, selected, null);
    }

    private static bool IsNormalMonster(CardModel card) {
        return card is BaseMonsterCard monster
            && !monster.IsExtra
            && !monster.YgoGetCore().IsEffectMonster;
    }
}
