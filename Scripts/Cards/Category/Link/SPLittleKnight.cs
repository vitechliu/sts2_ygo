using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Category.Link;

[RegisterCard(typeof(LinkCardPool))]
public class SPLittleKnight() : BaseExtraLinkCard(energyCost, CardType.Skill, rarity, targetType, shouldShowInCardLibrary) {
    public override int CardId => 29301450;
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.LinkSummon(),
        YgoHoverTipConst.VoidDamage()
    ];
    
    private const int energyCost = -1;
    private const CardRarity rarity = CardRarity.Common;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    //
    // protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
    //     // HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
    //     // HoverTipFactory.FromPower<VigorPower>(),
    //     // HoverTipFactory.FromPower<StarscourgePower>(),
    // ];


    // protected override IEnumerable<IHoverTip> ExtraHoverTips => new List<IHoverTip>();{
    //     HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
    // }

    public override int BaseAttackVar => 16;
    public override int BaseLifeVar => 1;
    public override int UpgradeAttackVar => 5;
    public override int UpgradeLifeVar => 0;

    public override bool CanUseLinkMaterial(SummonMaterial material) {
        return material.IsEffectMonster;
    }
}
