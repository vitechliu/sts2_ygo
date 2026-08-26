using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class ClockWyvernToken() : BaseTokenCard(TargetType.None) {
    public override int CardId => 21830680;

    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 1;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<ClockWyvern>(),
        BaseSummonHoverTip
    ];
}
