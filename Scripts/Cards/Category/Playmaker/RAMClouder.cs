using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Core;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class RAMClouder() : BaseMonsterCard(2, CardType.Attack, CardRarity.Common, TargetType.None) {
    public override int CardId => 9190563;

    public override int BaseAttackVar => 8;
    public override int BaseLifeVar => 5;

    // protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
    //     BaseSummonHoverTip,
    // ];
}
