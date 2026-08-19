using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Core;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(CommonCardPool))]
public class AshBlossomJoyousSpring() : BaseMonsterCard(1, CardRarity.Common, TargetType.None) {
    public override int CardId => 14558127;

    public override int BaseAttackVar => 1;
    public override int BaseLifeVar => 1;
    public override int UpgradeAttackVar => 1;
    public override int UpgradeLifeVar => 1;

    // protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
    //     BaseSummonHoverTip,
    // ];
}
