using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core.Hooks;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class BootStaggeredToken() : BaseMonsterCard(0, CardRarity.Token, TargetType.None), IMonsterSummonHookListener {
    public override int CardId => 70950699;

    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 1;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<BootStaggered>(),
        BaseSummonHoverTip
    ];
}
