using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace VYgo.Scripts.Cards.Category.Common;

/// <summary>
/// 仅供同调测试，实际未开发
/// </summary>
[RegisterCard(typeof(CommonCardPool))]
public class AshBlossomJoyousSpring() : BaseMonsterCard(1, CardRarity.Rare, TargetType.None) {
    public override int CardId => 14558127;

    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 5;
    public override int UpgradeLifeVar => 2;

    // protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
    //     BaseSummonHoverTip,
    // ];
}
