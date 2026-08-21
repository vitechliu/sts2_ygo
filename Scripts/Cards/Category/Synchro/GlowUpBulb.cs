using MegaCrit.Sts2.Core.Commands;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Ui.Toast;
using VYgo.Core;

namespace VYgo.Scripts.Cards.Category.Synchro;

[RegisterCard(typeof(SynchroCardPool))]
public class GlowUpBulb() : BaseRightClickableMonsterCard(1, CardRarity.Common, TargetType.None) {
    public override int CardId => 67441435;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.GraveyardAction(),
        YgoHoverTipConst.SpecialSummon()
    ];

    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 1;
    protected override RightClickType ClickType => RightClickType.Graveyard;

    protected override async Task OnYgoRightClick(ModRightClickExecutionContext context) {
        Entry.Logger.Info("RightClick1");
        
        var drawPile = PileType.Draw.GetPile(Owner);
        if (drawPile.IsEmpty) {
            RitsuToastService.ShowWarning(
                new LocString("cards", "V_YGO_CARD_GLOW_UP_BULB.hintNoCards").GetFormattedText()
            );
            return;
        }
        if (!this.CanUseEffectOncePerDuelByCard(CombatState, Owner)) {
           EffectUtil.ToastOncePerDuel(this);
           return;
        }
        Entry.Logger.Info("RightClick2");
        NCapstoneContainer.Instance?.Close();
        var addSuccess = await CommonUtil.SendToGraveyardFromDeck(Owner, 1);
        if (!addSuccess) {
            RitsuToastService.ShowWarning(
                new LocString("cards", "V_YGO_CARD_GLOW_UP_BULB.hintNoCards").GetFormattedText()
            );
            return;
        }
        Entry.Logger.Info("RightClick3");
        
        await CardCmd.AutoPlay(context.PlayerChoiceContext, this, null);
        Entry.Logger.Info("RightClick4");
    }
}
