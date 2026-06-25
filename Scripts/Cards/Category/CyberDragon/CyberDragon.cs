using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using VYgo.Utils;

namespace VYgo.Scripts.Cards.Category.CyberDragon;

[RegisterCard(typeof(RedhatCardPool))]
[RegisterCharacterStarterCard(typeof(RedhatCharacter), 3)]
public class CyberDragon() : BaseMonsterCard(energyCost, rarity, targetType, shouldShowInCardLibrary) {
    public override int CardId => 70095154;

    private const int energyCost = 2;
    private const CardRarity rarity = CardRarity.Common;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;


    // protected override IEnumerable<IHoverTip> ExtraHoverTips => new List<IHoverTip>();{
    //     HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
    // }

    public override int BaseAttackVar => 7;
    public override int BaseLifeVar => 5;
    public override int UpgradeAttackVar => 2;
    public override int UpgradeLifeVar => 1;

    protected override bool ShouldGlowGoldInternal => Active;
    
    bool Active => Owner.MinionCount() <= 0;

    void FlushCost() {
        EnergyCost.SetUntilPlayed(Active ? 0 : CanonicalEnergyCost);
    }
    public override Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw) {
        if (card != this) return Task.CompletedTask;
        FlushCost();
        return Task.CompletedTask;
    }
    public override Task AfterCardEnteredCombat(CardModel card) {
        if (card != this) return Task.CompletedTask;
        FlushCost();
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        if (cardPlay.Card.Owner != this.Owner) return Task.CompletedTask;
        FlushCost();
        return Task.CompletedTask;
    }
}