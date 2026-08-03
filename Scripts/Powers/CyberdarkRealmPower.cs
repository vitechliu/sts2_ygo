using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using VYgo.Core;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.ZaneTruesdale;

namespace VYgo.Scripts.Powers;

[RegisterPower]
public class CyberdarkRealmPower : BaseActionPower {
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://VYgo/images/powers/cyberdark_realm_power.png",
        BigIconPath: "res://VYgo/images/powers/cyberdark_realm_power.png"
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<CyberdarkRealm>(),
        YgoHoverTipConst.Action(),
    ];

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants) {
        if (side == CombatSide.Player) {
            await PowerCmd.TickDownDuration(this);
        }
    }

    public override bool CanExecuteRightClick(ModRightClickExecutionContext context) {
        return base.CanExecuteRightClick(context)
            && PileType.Hand.GetPile(context.Player).Cards.Any(IsCyberdarkMonster);
    }

    protected override async Task<bool> OnAction(ModRightClickExecutionContext context) {
        if (context.PlayerChoiceContext == null) return false;

        BaseMonsterCard? selectedMonster = (await CardSelectCmd.FromCombatPile(
                prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
                context: context.PlayerChoiceContext,
                pile: PileType.Hand.GetPile(context.Player),
                player: context.Player,
                filter: IsCyberdarkMonster))
            .OfType<BaseMonsterCard>()
            .FirstOrDefault();
        if (selectedMonster == null) return false;

        selectedMonster.EnergyCost.AddThisTurnOrUntilPlayed(-CyberdarkRealm.CostReduction, reduceOnly: true);
        return true;
    }

    private static bool IsCyberdarkMonster(CardModel card) {
        return card is BaseMonsterCard monsterCard
            && monsterCard.ContainArchetype(YgoArchetypes.Cyberdark);
    }
}
