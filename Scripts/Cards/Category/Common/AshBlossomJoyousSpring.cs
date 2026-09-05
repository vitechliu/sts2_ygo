using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Interactions.RightClick;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.HoverTips;
using VYgo.Core;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(CommonCardPool))]
public class AshBlossomJoyousSpring()
    : BaseRightClickableMonsterCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.None) {
    public override int CardId => 14558127;

    protected override RightClickType ClickType => RightClickType.Hand;

    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 5;
    public override int UpgradeLifeVar => 2;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        HoverTipFactory.FromPower<NegatingPower>(),
        YgoHoverTipConst.HandAction()
    ];

    protected override async Task OnYgoRightClick(ModRightClickExecutionContext context) {
        if (context.PlayerChoiceContext is not { } choiceContext) return;

        await PowerCmd.Apply<NegatingPower>(
            choiceContext,
            Owner.Creature,
            1m,
            Owner.Creature,
            this);
        await CardCmd.Exhaust(choiceContext, this);
    }
}
