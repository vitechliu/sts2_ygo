using MegaCrit.Sts2.Core.CardSelection;
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
using VYgo.Scripts.Cards;
using VYgo.Utils;

namespace VYgo.Scripts.Powers;

[RegisterPower]
public class CyberneticRevolutionPower : BaseActionPower {
    private sealed class Data {
        public CardModel? Source;
        public int SetTurn;
    }
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://images/powers/covered_power.png", BigIconPath: "res://images/powers/covered_power.png");
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.SetCard(), YgoHoverTipConst.PowerAction(), YgoHoverTipConst.SpecialSummon()
    ];
    protected override object InitInternalData() => new Data();
    public override Task AfterApplied(Creature? applier, CardModel? cardSource) {
        var data = GetInternalData<Data>();
        data.Source = cardSource;
        data.SetTurn = Owner.Player?.PlayerCombatState.TurnNumber ?? 0;
        return Task.CompletedTask;
    }
    private static bool IsTribute(SummonMaterial material) => material.NameEquals(YgoMaterialNames.电子龙);
    private static bool IsTarget(CardModel card) => card is BaseExtraFusionCard fusion
        && fusion.YgoGetCore().IsRace(YgoRace.Machine);
    public override bool CanExecuteRightClick(ModRightClickExecutionContext context) =>
        base.CanExecuteRightClick(context) && GetInternalData<Data>().Source != null
        && context.Player.PlayerCombatState.TurnNumber > GetInternalData<Data>().SetTurn
        && context.Player.MinionCount() <= context.Player.GetMaxMinionCount()
        && SummonUtil.HasValidFieldTribute(context.Player, 1, IsTribute)
        && Entry.ExtraPile.GetPile(context.Player).Cards.Any(IsTarget);

    protected override async Task<bool> OnAction(ModRightClickExecutionContext context) {
        if (context.PlayerChoiceContext is not { } choice || GetInternalData<Data>().Source is not { } source) return false;
        var player = context.Player;
        try {
            if (!await SummonUtil.ExecuteFieldTribute(choice, player, source, 1, IsTribute)) return false;
            // 祭品成功即消耗这次盖伏，送墓触发改变局面也不能重复启动。
            Flash();
            await PowerCmd.Remove(this);
            if (player.Creature.IsDead || player.MinionCount() >= player.GetMaxMinionCount()) return true;
            var pile = Entry.ExtraPile.GetPile(player);
            var selected = (await CardSelectCmd.FromCombatPile(choice, pile, player,
                new CardSelectorPrefs(SelectionScreenPrompt, 1), IsTarget)).OfType<BaseExtraFusionCard>().FirstOrDefault();
            if (selected?.Pile == pile && IsTarget(selected)) {
                // 此处是直接特召，不额外要求或消费融合素材。
                await selected.AutoPlayAndCaptureSummonedCreature(choice, null);
            }
            return true;
        }
        finally { InvokeExecutionFinished(); }
    }
}
