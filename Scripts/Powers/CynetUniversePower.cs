using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using VYgo.Core;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.Playmaker;

namespace VYgo.Scripts.Powers;

[RegisterPower]
public class CynetUniversePower : BaseActionPower {
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://VYgo/images/cards/61583217.png",
        BigIconPath: "res://VYgo/images/cards/61583217.png"
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<CynetUniverse>(),
        YgoHoverTipConst.PowerAction()
    ];

    public override bool CanExecuteRightClick(ModRightClickExecutionContext context) {
        return base.CanExecuteRightClick(context)
            && PileType.Discard.GetPile(context.Player).Cards.Count(IsMonsterCard) >= Amount;
    }

    protected override async Task<bool> OnAction(ModRightClickExecutionContext context) {
        if (context.PlayerChoiceContext is not { } choiceContext) return false;

        List<CardModel> selectedMonsters = (await CardSelectCmd.FromCombatPile(
                choiceContext,
                PileType.Discard.GetPile(context.Player),
                context.Player,
                new CardSelectorPrefs(SelectionScreenPrompt, Amount),
                IsMonsterCard))
            .ToList();
        if (selectedMonsters.Count != Amount) return false;

        Flash();
        foreach (CardModel monsterCard in selectedMonsters) {
            await CardPileCmd.Add(
                monsterCard,
                monsterCard is BaseExtraCard ? Entry.ExtraPile : PileType.Draw);
        }

        return true;
    }

    private static bool IsMonsterCard(CardModel card) {
        return card is BaseMonsterCard;
    }
}
