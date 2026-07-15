using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Core;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberBarrierDragon() : BaseMonsterCard(2, CardRarity.Basic, TargetType.None) {
    public override int CardId => 68774379;

    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 7;
    public override int UpgradeAttackVar => 0;
    public override int UpgradeLifeVar => 2;

    // protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
    //     BaseSummonHoverTip,
    // ];
    
}
