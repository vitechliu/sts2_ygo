using MegaCrit.Sts2.Core.Commands;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using STS2RitsuLib.Interactions.RightClick;
using VYgo.Utils;
using VYgo.Core;

namespace VYgo.Scripts.Cards.Category.Synchro;

[RegisterCard(typeof(SynchroCardPool))]
public class GlowUpBulb() : BaseRightClickableMonsterCard(1, CardType.Skill, CardRarity.Common, TargetType.None) {
    public override int CardId => 67441435;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.GraveyardAction(),
        YgoHoverTipConst.SpecialSummon()
    ];

    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 1;
    protected override RightClickType ClickType => RightClickType.Graveyard;

    protected override LocString? ValidateRightClick(ModRightClickExecutionContext context) {
        LocString? error = base.ValidateRightClick(context);
        if (error != null) return error;
        if (Owner.MinionCount() >= Owner.GetMaxMinionCount()) return RightClickError("CAPACITY");
        if (PileType.Draw.GetPile(Owner).IsEmpty) return RightClickError("EMPTY_DRAW_PILE");
        if (this.HasUsedEffectOncePerDuelByCard(Owner)) return RightClickError("ONCE_PER_COMBAT");
        return null;
    }

    protected override async Task OnYgoRightClick(ModRightClickExecutionContext context) {
        if (!this.CanUseEffectOncePerDuelByCard(CombatState, Owner)) return;
        NCapstoneContainer.Instance?.Close();
        var addSuccess = await CommonUtil.SendToGraveyardFromDeck(Owner, 1);
        if (!addSuccess) {
            ShowRightClickError(context, RightClickError("EMPTY_DRAW_PILE"));
            return;
        }
        await CardCmd.AutoPlay(context.PlayerChoiceContext, this, null);
    }
}
