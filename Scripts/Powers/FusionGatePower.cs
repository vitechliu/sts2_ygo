using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using VYgo.Core;
using VYgo.Scripts.Cards.Category.Fusion;

namespace VYgo.Scripts.Powers;

[RegisterPower]
public sealed class FusionGatePower : BaseActionPower {
    private sealed class Data {
        public CardModel? SourceCard { get; set; }
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://VYgo/images/powers/fusion_gate_power.png",
        BigIconPath: "res://VYgo/images/powers/fusion_gate_power.png"
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<FusionGate>(),
        YgoHoverTipConst.InfinitePowerAction(),
        YgoHoverTipConst.FusionSummon()
    ];

    protected override object InitInternalData() => new Data();

    public override Task AfterApplied(Creature? applier, CardModel? cardSource) {
        GetInternalData<Data>().SourceCard = cardSource;
        return Task.CompletedTask;
    }

    public override bool CanExecuteRightClick(ModRightClickExecutionContext context) {
        return base.CanExecuteRightClick(context)
            && GetInternalData<Data>().SourceCard is { }
            && SummonUtil.HasFusionSummonTarget(
                context.Player,
                _ => SummonUtil.GetFieldAndHandMonsterMaterials(context.Player),
                _ => PileType.Exhaust);
    }

    protected override async Task<bool> OnAction(ModRightClickExecutionContext context) {
        if (context.PlayerChoiceContext is not { } choiceContext
            || GetInternalData<Data>().SourceCard is not { } sourceCard) {
            return false;
        }

        ExtraDeckSummonResult result = await SummonUtil.ExecuteFusionSummon(
            new FusionSummonRequest(
                SourceCard: sourceCard,
                Owner: context.Player,
                ChoiceContext: choiceContext,
                SelectionPrompt: SelectionScreenPrompt,
                GetAvailableMaterials: _ =>
                    SummonUtil.GetFieldAndHandMonsterMaterials(context.Player),
                GetMaterialDestination: _ => PileType.Exhaust));
        if (result.Success) Flash();

        // 无限启动：成功后也不消耗每回合一次的行动次数。
        return false;
    }
}
