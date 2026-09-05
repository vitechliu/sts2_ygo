using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using VYgo.Core;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(CommonCardPool))]
public class FiendsmithsSanctToken() : BaseTokenCard(TargetType.None) {
    public override int CardId => 35552986;

    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 1;

    protected override YgoType CardYgoType => YgoType.token;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<FiendsmithsSanct>(),
        BaseSummonHoverTip
    ];
}
