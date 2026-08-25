using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using VYgo.Core;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(YgoEventCardPool))]
public class LacrimatheCrimsonTears() : BaseMonsterCard(1, CardRarity.Event, TargetType.None) {
    public override int CardId => 28803166;

    public override int BaseAttackVar => 4;
    public override int BaseLifeVar => 4;
    public override int UpgradeAttackVar => 3;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.SendToGraveyard()
    ];
}
