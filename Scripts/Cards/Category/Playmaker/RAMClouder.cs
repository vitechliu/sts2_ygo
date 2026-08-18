using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Core;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class RAMClouder() : BaseMonsterCard(1, CardRarity.Common, TargetType.None) {
    public override int CardId => 9190563;

    public override int BaseAttackVar => 5;
    public override int BaseLifeVar => 3;

    // protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
    //     BaseSummonHoverTip,
    // ];
}
