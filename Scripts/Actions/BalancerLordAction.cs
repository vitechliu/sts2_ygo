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

namespace VYgo.Scripts.Actions;

public sealed class BalancerLordAction : BasePerTurnMonsterAction {
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.Action()
    ];

    protected override bool IsVisibleInternal => true;

    public override TargetType TargetType => TargetType.None;

    protected override string? IntentIconPath => "res://VYgo/images/powers/ygo.png";
    public override string? CustomIconPath => IntentIconPath;

    public override bool CanAct(ICombatState combatState) {
        return base.CanAct(combatState)
            && Owner.PetOwner is { } player
            && PileType.Hand.GetPile(player).Cards.Any(IsCyberseMonster);
    }

    protected override async Task OnAct(
        PlayerChoiceContext choiceContext,
        Creature? target
    ) {
        if (Owner.PetOwner is not { } player) return;

        var selected = (await CardSelectCmd.FromHand(
            choiceContext,
            player,
            new CardSelectorPrefs(SelectionScreenPrompt, 1),
            IsCyberseMonster,
            this
        )).FirstOrDefault();
        if (selected == null) return;

        SpendUses();
        selected.EnergyCost.AddThisCombat(-1);
    }

    private static bool IsCyberseMonster(CardModel model) {
        return model is BaseMonsterCard card && card.YgoGetCore().IsRace(YgoRace.Cyberse);
    }
}
