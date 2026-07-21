using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Monsters;
using VYgo.Scripts.Pools;
using VYgo.Utils;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
[RegisterCharacterStarterCard(typeof(ZaneTruesdaleCharacter), 3)]
[RegisterCharacterStarterCard(typeof(RedhatCharacter), 15)]
public class CyberDragon() : BaseMonsterCard(energyCost, rarity, targetType, shouldShowInCardLibrary) {
    public override int CardId => 70095154;

    public override YgoMaterialNames? MaterialCardName => YgoMaterialNames.电子龙;
    
    private const int energyCost = 2;
    private const CardRarity rarity = CardRarity.Common;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;


    // protected override IEnumerable<IHoverTip> ExtraHoverTips => new List<IHoverTip>();{
    //     HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
    // }

    public override int BaseAttackVar => 5;
    public override int BaseLifeVar => 3;
    public override int UpgradeAttackVar => 2;

    protected override bool ShouldGlowGoldInternal => IsSpecialSummonActive;
    
    private bool IsSpecialSummonActive => Owner.MinionCount() == 0;

    public override bool TryModifyEnergyCostInCombat(
        CardModel card,
        decimal originalCost,
        out decimal modifiedCost) {
        modifiedCost = originalCost;
        if (card != this || !IsSpecialSummonActive) return false;

        modifiedCost = 0m;
        return true;
    }
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.SpecialSummon()
    ];


    public static bool PlayerHasCyberDragon(Player player) {
        return player.Creature.Pets.Count(c => c.Monster is BaseMonster bm 
                                               && bm.YgoGetCard()?.MaterialCardName == YgoMaterialNames.电子龙) > 0;
    }
}
