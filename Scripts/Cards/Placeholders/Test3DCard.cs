using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Placeholders;

[RegisterCard(typeof(RedhatCardPool))]
public class Test3DCard(): BasePlaceholder(CardType.Skill, CardRarity.Common) {
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        return base.OnPlay(choiceContext, cardPlay);
    }
}