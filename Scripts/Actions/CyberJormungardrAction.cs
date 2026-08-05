using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;
using VYgo.Scripts.Cards.Category.Fusion;
using VYgo.Scripts.Monsters;

namespace VYgo.Scripts.Actions;

public sealed class CyberJormungardrAction : BasePerTurnMonsterAction {
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.Action(),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.FromCard<Polymerization>()
    ];

    protected override bool IsVisibleInternal => true;

    public override TargetType TargetType => TargetType.None;

    protected override string? IntentIconPath =>
        "res://VYgo/images/intents/intent_dragon_vortex.png";

    public override string? CustomIconPath => IntentIconPath;

    public override bool CanAct(ICombatState combatState) {
        return base.CanAct(combatState)
            && Owner.PetOwner != null
            && Owner.Monster is BaseMonster { SourceCard: not null };
    }

    protected override async Task OnAct(
        PlayerChoiceContext choiceContext,
        Creature? target
    ) {
        var player = Owner.PetOwner;
        if (player == null
            || Owner.Monster is not BaseMonster cyberJormungardr
            || cyberJormungardr.SourceCard is not { } sourceCard
            || !cyberJormungardr.TryReserveSourceCardAsSummonMaterial(sourceCard)) {
            return;
        }

        SpendUses();
        await CardCmd.Exhaust(choiceContext, sourceCard);
        await CreatureCmd.Kill(Owner, true);

        CardModel polymerization = player.Creature.CombatState
            .CreateCard<Polymerization>(player);
        CardCmd.ApplyKeyword(polymerization, CardKeyword.Exhaust);
        await CardPileCmd.AddGeneratedCardToCombat(
            polymerization,
            PileType.Hand,
            player
        );
    }
}
