using MegaCrit.Sts2.Core.Commands;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interactions.RightClick;
using VYgo.Core;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(CommonCardPool))]
public class MaxxC() : BaseRightClickableMonsterCard(1, CardRarity.Common, TargetType.None), IModRightClickableCard {
    public override int CardId => 23434538;

    public override int BaseAttackVar => 2;
    public override int BaseLifeVar => 2;
    
    

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.SpecialSummon(),
        YgoHoverTipConst.HandAction()
    ];

    protected override async Task OnYgoRightClick(ModRightClickExecutionContext context) {
        await PowerCmd.Apply<MaxxCPower>(context.PlayerChoiceContext, Owner.Creature, 1m, Owner.Creature, this);
        await CardCmd.Discard(context.PlayerChoiceContext, this);
    }

    protected override void OnUpgrade() {
        EnergyCost.UpgradeBy(-1);
    }
}
