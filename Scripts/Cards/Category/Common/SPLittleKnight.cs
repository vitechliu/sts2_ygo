using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Commands;
using MinionLib.Minion;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Monsters.YGO;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(RedhatCardPool))]
[RegisterCharacterStarterCard(typeof(RedhatCharacter), 1)]
public class SPLittleKnight() : BaseExtraLinkCard(energyCost,rarity, targetType, shouldShowInCardLibrary) {
    public override int CardId => 29301450;
    
    private const int energyCost = 0;
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
}