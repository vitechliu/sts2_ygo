using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class CynetUniverse() : BaseSpellCard(0, CardType.Skill, CardRarity.Common, TargetType.None) {
    public override int CardId => 61583217;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        List<CardModel> selected = (await CardSelectCmd.FromCombatPile(
            choiceContext,
            PileType.Discard.GetPile(Owner),
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, DynamicVars.Cards.IntValue),
            card => card is BaseMonsterCard)).ToList();

        foreach (CardModel card in selected) {
            await CardPileCmd.Add(
                card,
                card is BaseExtraCard ? Entry.ExtraPile : PileType.Draw);
        }
    }

    protected override void OnUpgrade() {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
