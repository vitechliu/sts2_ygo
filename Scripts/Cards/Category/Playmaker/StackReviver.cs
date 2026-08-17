using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class StackReviver() : BaseMonsterCard(0, CardRarity.Common, TargetType.None) {
    public override int CardId => 9523599;

    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 3;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.LinkSummon(),
        YgoHoverTipConst.SpecialSummon(),
    ];
}
