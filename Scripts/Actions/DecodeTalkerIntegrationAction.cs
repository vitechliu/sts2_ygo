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

public sealed class DecodeTalkerIntegrationAction : BasePerTurnMonsterAction {
    protected override bool IsVisibleInternal => true;
    public override TargetType TargetType => TargetType.None;
    protected override string? IntentIconPath => "res://VYgo/images/powers/ygo.png";
    public override string? CustomIconPath => IntentIconPath;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.Action()
    ];

    public override bool CanAct(ICombatState combatState) {
        return base.CanAct(combatState)
            && Owner.PetOwner is { } player
            && Entry.ExtraPile.GetPile(player).Cards.Any(IsCyberseExtraMonster);
    }

    protected override async Task OnAct(PlayerChoiceContext choiceContext, Creature? target) {
        if (Owner.PetOwner is not { } player) return;

        CardModel? selected = (await CardSelectCmd.FromCombatPile(
                choiceContext,
                Entry.ExtraPile.GetPile(player),
                player,
                new CardSelectorPrefs(SelectionScreenPrompt, 1),
                IsCyberseExtraMonster))
            .FirstOrDefault();
        if (selected == null) return;

        SpendUses();
        await CardPileCmd.Add(selected, PileType.Discard);
    }

    private static bool IsCyberseExtraMonster(CardModel card) {
        return card is BaseMonsterCard { IsExtra: true } monster
            && monster.YgoGetCore().IsRace(YgoRace.Cyberse);
    }
}
