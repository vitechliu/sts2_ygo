using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Core;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberDragonZwei() : BaseMonsterCard(1, CardRarity.Common, TargetType.None) {
    public override int CardId => 5373478;

    public override int BaseAttackVar => 4;
    public override int BaseLifeVar => 3;

    // protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
    //     BaseSummonHoverTip,
    // ];
}
