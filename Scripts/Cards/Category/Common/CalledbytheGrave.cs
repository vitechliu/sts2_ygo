using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(CommonCardPool))]
public class CalledbytheGrave()
    : BaseSpellCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.None) {
    public override int CardId => 24224830;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<NegatingPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.FromPower<NegatingPower>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        CardModel? selectedCard = (await CardSelectCmd.FromCombatPile(
                prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
                context: choiceContext,
                pile: PileType.Discard.GetPile(Owner),
                player: Owner))
            .FirstOrDefault();
        if (selectedCard == null) {
            return;
        }

        await CardCmd.Exhaust(choiceContext, selectedCard);
        await PowerCmd.Apply<NegatingPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["NegatingPower"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() {
        DynamicVars["NegatingPower"].UpgradeValueBy(1m);
    }
}
