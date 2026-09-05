using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using VYgo.Core;
using VYgo.Scripts.Actions;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.Machine;
using VYgo.Scripts.Monsters;

namespace VYgo.Scripts.Powers;

[RegisterPower]
public sealed class ClockworkNightPower : BaseActionPower {
    private sealed class Data {
        public CardModel? SourceCard { get; set; }
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://VYgo/images/cards/84797028.png",
        BigIconPath: "res://VYgo/images/cards/84797028.png"
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<ClockworkNight>(),
        YgoHoverTipConst.PowerAction()
    ];

    protected override object InitInternalData() => new Data();

    public override Task AfterApplied(Creature? applier, CardModel? cardSource) {
        GetInternalData<Data>().SourceCard = cardSource;
        RefreshMonsterInfo(Owner);
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner) {
        RefreshMonsterInfo(oldOwner);
        return Task.CompletedTask;
    }

    public override bool CanExecuteRightClick(ModRightClickExecutionContext context) {
        return base.CanExecuteRightClick(context)
            && GetInternalData<Data>().SourceCard is { }
            && PileType.Draw.GetPile(context.Player).Cards.Any(IsEarthMachineMonster);
    }

    protected override async Task<bool> OnAction(ModRightClickExecutionContext context) {
        if (context.PlayerChoiceContext is not { } choiceContext
            || GetInternalData<Data>().SourceCard is not { } sourceCard) {
            return false;
        }

        CardModel? selected = (await CardSelectCmd.FromCombatPile(
                prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
                context: choiceContext,
                pile: PileType.Draw.GetPile(context.Player),
                player: context.Player,
                filter: IsEarthMachineMonster))
            .FirstOrDefault();
        if (selected == null) return false;

        Flash();
        await CardPileCmd.Add(selected, PileType.Hand);
        await CardCmd.Exhaust(choiceContext, sourceCard);
        await PowerCmd.Remove(this);
        return true;
    }

    private static bool IsEarthMachineMonster(CardModel card) {
        return card is BaseMonsterCard monsterCard
            && monsterCard.YgoGetCore().IsRace(YgoRace.Machine)
            && monsterCard.YgoGetCore()?.Attribute == "地";
    }

    private static void RefreshMonsterInfo(Creature playerCreature) {
        foreach (Creature pet in playerCreature.Pets) {
            pet.GetPower<YgoPower>()?.InitInfo();
            BasePerTurnMonsterAction.RefreshActionIntent(pet);
        }
    }
}
