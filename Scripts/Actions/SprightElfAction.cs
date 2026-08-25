using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.Common;
using VYgo.Scripts.Monsters;
using VYgo.Utils;

namespace VYgo.Scripts.Actions;

public sealed class SprightElfAction : BasePerTurnMonsterAction {
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.Action(),
        YgoHoverTipConst.SpecialSummon(),
    ];

    protected override bool IsVisibleInternal => true;

    public override TargetType TargetType => TargetType.None;

    protected override string? IntentIconPath => "res://VYgo/images/powers/reborn.png";
    public override string? CustomIconPath => IntentIconPath;

    public override bool CanAct(ICombatState combatState) {
        return base.CanAct(combatState)
            && Owner.PetOwner is { } player
            && player.MinionCount() < player.GetMaxMinionCount()
            && PileType.Discard.GetPile(player).Cards.Any(IsLevel2Monster);
    }

    protected override async Task OnAct(
        PlayerChoiceContext choiceContext,
        Creature? target
    ) {
        Player? player = Owner.PetOwner;
        if (player == null || player.MinionCount() >= player.GetMaxMinionCount()) return;

        BaseMonsterCard? selected = (await CardSelectCmd.FromCombatPile(
                choiceContext,
                PileType.Discard.GetPile(player),
                player,
                new CardSelectorPrefs(SelectionScreenPrompt, 1),
                IsLevel2Monster))
            .OfType<BaseMonsterCard>()
            .FirstOrDefault();
        if (selected == null || player.MinionCount() >= player.GetMaxMinionCount()) return;

        SpendUses();
        await selected.AutoPlayAndCaptureSummonedCreature(choiceContext, null);
    }

    private static bool IsLevel2Monster(CardModel card) {
        return card is BaseMonsterCard monster
            && !monster.IsExtra
            && YgoSummonRules.IsLevel2OrRank2(monster.YgoGetCore());
    }
}
