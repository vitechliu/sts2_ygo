using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using VYgo.Core;
using VYgo.Scripts.Cards.Category.Common;

namespace VYgo.Scripts.Powers;

[RegisterPower]
public sealed class FiendsmithKyriePower : BaseActionPower {
    private sealed class Data {
        public CardModel? SourceCard { get; set; }
        public int SetTurnNumber { get; set; }
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://VYgo/images/cards/26434972.png",
        BigIconPath: "res://VYgo/images/cards/26434972.png"
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<FiendsmithKyrie>(),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        YgoHoverTipConst.SetCard(),
        YgoHoverTipConst.PowerAction()
    ];

    protected override object InitInternalData() {
        return new Data();
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource) {
        Data data = GetInternalData<Data>();
        data.SourceCard = cardSource;
        data.SetTurnNumber = Owner.Player?.PlayerCombatState.TurnNumber ?? 0;
        return Task.CompletedTask;
    }

    public override bool CanExecuteRightClick(ModRightClickExecutionContext context) {
        Data data = GetInternalData<Data>();
        return base.CanExecuteRightClick(context)
            && data.SourceCard != null
            && context.Player.PlayerCombatState.TurnNumber > data.SetTurnNumber;
    }

    protected override async Task<bool> OnAction(ModRightClickExecutionContext context) {
        if (context.PlayerChoiceContext is not { } choiceContext) return false;

        Data data = GetInternalData<Data>();
        if (data.SourceCard == null) return false;

        Flash();
        await CardCmd.Exhaust(choiceContext, data.SourceCard);
        await PowerCmd.Remove(this);
        await PowerCmd.Apply<FiendsmithKyrieDamageReductionPower>(
            choiceContext,
            context.Player.Creature,
            1m,
            context.Player.Creature,
            data.SourceCard,
            true);
        return true;
    }
}
