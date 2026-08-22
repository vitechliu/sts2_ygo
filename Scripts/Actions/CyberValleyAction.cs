using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;
using VYgo.Scripts.Cards.Category.ZaneTruesdale;
using VYgo.Scripts.Monsters;

namespace VYgo.Scripts.Actions;

public sealed class CyberValleyAction : BasePerTurnMonsterAction {
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.Action(),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
    ];

    protected override bool IsVisibleInternal => true;
    public override TargetType TargetType => TargetType.None;
    protected override string? IntentIconPath => "res://images/powers/confused_power.png";
    public override string? CustomIconPath => IntentIconPath;

    public override bool CanAct(ICombatState combatState) {
        return base.CanAct(combatState)
            && Owner.PetOwner != null
            && Owner.Monster is BaseMonster { SourceCard: CyberValley };
    }

    protected override async Task OnAct(
        PlayerChoiceContext choiceContext,
        Creature? target
    ) {
        if (Owner.PetOwner is not { } player
            || Owner.Monster is not BaseMonster {
                SourceCard: CyberValley sourceCard
            } cyberValley
            || !cyberValley.TryReserveSourceCardAsSummonMaterial(sourceCard)) {
            return;
        }

        ICombatState combatState = Owner.CombatState;
        SpendUses();
        await CardCmd.Exhaust(choiceContext, sourceCard);
        await CreatureCmd.Kill(Owner, true);

        List<CardModel> choices = [
            combatState.CreateCard<CyberValleyGuardForm>(player),
        ];
        if (SummonUtil.HasValidFieldTribute(player, 1)) {
            choices.Add(combatState.CreateCard<CyberValleyTributeForm>(player));
        }
        if (PileType.Discard.GetPile(player).Cards.Count > 0) {
            choices.Add(combatState.CreateCard<CyberValleyRecycleForm>(player));
        }

        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            choices,
            player);
        if (selected is BaseCyberValleyOption option) {
            await option.OnChosen(choiceContext);
        }
    }
}
