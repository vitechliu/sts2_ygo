using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using VYgo.Core;
namespace VYgo.Scripts.Cards.Category.Playmaker;
[RegisterCard(typeof(PlaymakerCardPool))]
public class CyberseGadget() : BaseMonsterCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.None) {
    public override int CardId => 645087;
    public override int BaseAttackVar => 5;
    public override int BaseLifeVar => 1;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [BaseSummonHoverTip,
        YgoHoverTipConst.SpecialSummon(), HoverTipFactory.FromCard<CyberseGadgetToken>(),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];
}
