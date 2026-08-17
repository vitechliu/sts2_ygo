using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MinionLib.Commands;
using VYgo.Core;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.Playmaker;
using VYgo.Scripts.Monsters;

namespace VYgo.Scripts.Actions;

public sealed class DualAssembwurmAction : BasePerTurnMonsterAction {
    private const string AttackIntentIconDirectory =
        "res://images/packed/intents/attack";

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.Action(),
        HoverTipFactory.FromPower<VulnerablePower>(),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
    ];

    protected override bool IsVisibleInternal => true;

    public override TargetType TargetType => TargetType.AnyEnemy;

    protected override string? IntentIconPath => Amount > 0m
        ? $"{AttackIntentIconDirectory}/intent_attack_{GetAttackIntentTier((int)Amount)}.png"
        : null;

    public override string? CustomIconPath => IntentIconPath;

    public override bool CanAct(ICombatState combatState) {
        return base.CanAct(combatState)
            && Owner.PetOwner is { } player
            && Owner.Monster is BaseMonster { SourceCard: DualAssembwurm }
            && PileType.Hand.GetPile(player).Cards.Any(IsCyberseMonster);
    }

    protected override async Task OnAct(
        PlayerChoiceContext choiceContext,
        Creature? target
    ) {
        if (target == null
            || Owner.PetOwner is not { } player
            || Owner.Monster is not BaseMonster { SourceCard: DualAssembwurm sourceCard }) {
            return;
        }

        var selected = (await CardSelectCmd.FromHand(
            choiceContext,
            player,
            new CardSelectorPrefs(SelectionScreenPrompt, 1),
            IsCyberseMonster,
            sourceCard
        )).FirstOrDefault();
        if (selected == null) return;

        SpendUses();
        await CardCmd.Exhaust(choiceContext, selected);
        await MinionAnimCmd.PlayBumpAttackAsync(Owner, target);
        await DamageCmd.Attack(Amount)
            .FromCard(sourceCard, null)
            .Targeting(target)
            .WithNoAttackerAnim()
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        if (target is { IsAlive: true }) {
            await PowerCmd.Apply<VulnerablePower>(
                choiceContext,
                target,
                sourceCard.Vulnerable,
                Owner,
                sourceCard);
        }
    }

    private static bool IsCyberseMonster(CardModel model) {
        return model is BaseMonsterCard card && card.YgoGetCore().IsRace(YgoRace.Cyberse);
    }

    private static int GetAttackIntentTier(int damage) {
        if (damage < 5) return 1;
        if (damage < 10) return 2;
        if (damage < 20) return 3;
        if (damage < 40) return 4;
        return 5;
    }
}
