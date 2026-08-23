using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(EventCardPool))]
public class FiendsmithsTract() : BaseSpellCard(1, CardType.Skill, CardRarity.Event, TargetType.None) {
    public override int CardId => 98567237;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        CardModel? selected = (await CardSelectCmd.FromCombatPile(
                choiceContext,
                PileType.Draw.GetPile(Owner),
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 1),
                FiendsmithUtil.IsLightFiendMonster))
            .FirstOrDefault();
        if (selected == null) return;

        await CardPileCmd.Add(selected, PileType.Hand);
        if (IsUpgraded) return;

        CardModel? discarded = (await CardSelectCmd.FromHandForDiscard(
                choiceContext,
                Owner,
                new CardSelectorPrefs(
                    new LocString("cards", "V_YGO_CARD_FIENDSMITHS_TRACT.discardSelectionScreenPrompt"),
                    1),
                null,
                this))
            .FirstOrDefault();
        if (discarded != null) {
            await CardCmd.Discard(choiceContext, discarded);
        }
    }
}
