using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using VYgo.Core;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(CommonCardPool))]
public class ForbiddenDroplet() : BaseSpellCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) {
    public override int CardId => 24299458;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        List<CardModel> discarded = (await CardSelectCmd.FromHandForDiscard(
            prefs: new CardSelectorPrefs(SelectionScreenPrompt, 0, 999999999),
            context: choiceContext,
            player: Owner,
            filter: null,
            source: this)).ToList();
        await CardCmd.Discard(choiceContext, discarded);

        if (discarded.Count > 0) {
            await PowerCmd.Apply<StrengthPower>(
                choiceContext,
                cardPlay.Target,
                -discarded.Count,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade() {
        EnergyCost.UpgradeBy(-1);
    }
}
