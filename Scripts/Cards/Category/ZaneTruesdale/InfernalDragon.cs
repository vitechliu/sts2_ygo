using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using VYgo.Core;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class InfernalDragon() : BaseMonsterCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.None) {
    public override int CardId => 47754278;

    public override int BaseAttackVar => 6;
    public override int BaseLifeVar => 1;
    public override int UpgradeAttackVar => 2;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.SendToGraveyard(),
    ];
}
