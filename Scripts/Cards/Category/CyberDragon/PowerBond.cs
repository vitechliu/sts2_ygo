using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Category.CyberDragon;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
[RegisterCharacterStarterCard(typeof(ZaneTruesdaleCharacter), 2)]
public class PowerBond() : BaseSpellCard(energyCost, CardType.Skill, rarity, targetType, shouldShowInCardLibrary) {
    public override int CardId => 37630732;

    private const int energyCost = 1;
    private const CardRarity rarity = CardRarity.Basic;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
    }
    
    protected override void OnUpgrade()
    {
    }
}
