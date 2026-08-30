using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Monsters;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Powers;
using VYgo.Scripts.Var;
using VYgo.Utils;

namespace VYgo.Scripts.Cards.Category.YgoEvent;

[RegisterCard(typeof(YgoEventCardPool))]
public class AleistertheInvoker()
    : BaseRightClickableMonsterCard(1, CardType.Skill, CardRarity.Event, TargetType.None) {
    public override int CardId => 86120751;

    public override int BaseAttackVar => 3;
    public override int BaseLifeVar => 3;
    public override int UpgradeAttackVar => 3;

    public int HandBoostAttack => DynamicVars["HandBoostAttack"].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new AttackVar("HandBoostAttack", 5)
    ];

    protected override RightClickType ClickType => RightClickType.Hand;
    protected override int RightClickCost => 0;
    protected override bool ShouldSpendResources => false;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..base.AdditionalHoverTips,
        YgoHoverTipConst.HandAction(),
        YgoHoverTipConst.Enhance()
    ];

    public override bool CanExecuteRightClick(
        ModRightClickExecutionContext context,
        bool toast
    ) {
        return context.Player == Owner
            && base.CanExecuteRightClick(context, toast)
            && Owner.Creature.Pets.Any(IsFusionMonster);
    }

    protected override async Task OnYgoRightClick(ModRightClickExecutionContext context) {
        if (context.PlayerChoiceContext is not { } choiceContext) return;

        NCapstoneContainer.Instance?.Close();
        await CardCmd.Discard(choiceContext, this);

        Dictionary<CardModel, Creature> targets = Owner.Creature.Pets
            .Where(IsFusionMonster)
            .Where(creature => creature.Monster is BaseMonster { SourceCard: not null })
            .ToUniqueSourceCardTargets(nameof(AleistertheInvoker));
        if (targets.Count == 0) return;

        CardModel? selected = (await CardSelectCmd.FromCombatPile(
                choiceContext,
                Entry.MonsterPile.GetPile(Owner),
                Owner,
                new CardSelectorPrefs(
                    new LocString(
                        "cards",
                        "V_YGO_CARD_ALEISTERTHE_INVOKER.handSelectionScreenPrompt"),
                    1),
                targets.ContainsKey))
            .FirstOrDefault();
        if (selected != null && targets.TryGetValue(selected, out Creature? target)) {
            await PowerCmd.Apply<AttackPower>(
                choiceContext,
                target,
                HandBoostAttack,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade() {
        base.OnUpgrade();
        DynamicVars["HandBoostAttack"].UpgradeValueBy(3m);
    }

    private static bool IsFusionMonster(Creature creature) {
        return creature.IsAlive
            && creature.Monster is BaseMonster { SourceCard: BaseExtraFusionCard };
    }
}
