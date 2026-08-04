using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberRepairPlant() : BaseSpellCard(1, CardType.Skill, CardRarity.Basic, TargetType.None) {
    public override int CardId => 86686671;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        CardModel? selectedCard = (await CardSelectCmd.FromCombatPile(
                prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
                context: choiceContext,
                pile: PileType.Draw.GetPile(Owner),
                player: Owner,
                filter: IsLightMachineMonster))
            .FirstOrDefault();
        if (selectedCard != null) {
            await CardPileCmd.Add(selectedCard, PileType.Hand);
        }
    }

    private static bool IsLightMachineMonster(CardModel card) {
        if (card is not BaseMonsterCard monsterCard) return false;

        var coreCard = monsterCard.YgoGetCore();
        return coreCard.IsRace(YgoRace.Machine)
            && coreCard?.Attribute == "光";
    }

    protected override void OnUpgrade() {
        EnergyCost.UpgradeBy(-1);
    }
}
