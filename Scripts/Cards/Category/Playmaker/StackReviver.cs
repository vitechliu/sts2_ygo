using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class StackReviver() : BaseMonsterCard(1, CardType.Skill, CardRarity.Common, TargetType.None) {
    public override int CardId => 9523599;


    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Ethereal
    ];
    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 3;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.LinkSummon(),
        YgoHoverTipConst.SpecialSummon(),
    ];

    protected override void OnUpgrade() {
        RemoveKeyword(CardKeyword.Ethereal);
    }
}
