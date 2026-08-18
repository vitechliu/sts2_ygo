using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class ClockWyvern() : BaseMonsterCard(1, CardRarity.Uncommon, TargetType.None) {
    public override int CardId => 21830679;

    public override int BaseAttackVar => 6;
    public override int BaseLifeVar => 3;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        HoverTipFactory.FromCard<ClockWyvernToken>()
    ];
}
