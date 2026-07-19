using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
// [RegisterCharacterStarterCard(typeof(ZaneTruesdaleCharacter), 2)]
public class CyberDragonCore() : BaseMonsterCard(energyCost,rarity, targetType, shouldShowInCardLibrary) {
    public override int CardId => 23893227;
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.EnterField(),
        YgoHoverTipConst.NameAs(YgoMaterialNames.电子龙)
    ];
    
    public override YgoMaterialNames? MaterialCardName => YgoMaterialNames.电子龙;
    
    private const int energyCost = 0;
    private const CardRarity rarity = CardRarity.Common;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        await base.OnPlay(choiceContext, cardPlay);

        CardModel? selectedCard = (await CardSelectCmd.FromCombatPile(
                prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
                context: choiceContext,
                pile: PileType.Draw.GetPile(Owner),
                player: Owner,
                filter: IsCyberDragonSpellOrTrap))
            .FirstOrDefault();
        if (selectedCard != null) {
            await CardPileCmd.Add(selectedCard, PileType.Hand);
        }
    }

    private static bool IsCyberDragonSpellOrTrap(CardModel card) {
        return card is BaseVYgoCard ygoCard
            && ygoCard.ContainArchetype(YgoArchetypes.CyberDragon)
            && ygoCard.YgoCardType is YgoType.spell or YgoType.trap;
    }

    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 3;
    public override int UpgradeLifeVar => 2;
}
