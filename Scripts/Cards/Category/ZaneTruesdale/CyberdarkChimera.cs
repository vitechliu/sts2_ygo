using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using VYgo.Core;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberdarkChimera() : BaseMonsterCard(2, CardType.Skill, CardRarity.Rare, TargetType.None) {
    public override int CardId => 5370235;

    public override int BaseAttackVar => 3;
    public override int BaseLifeVar => 8;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.EnterField(),
        YgoHoverTipConst.SendToGraveyard(),
        HoverTipFactory.FromCard<PowerBond>(),
    ];
}
