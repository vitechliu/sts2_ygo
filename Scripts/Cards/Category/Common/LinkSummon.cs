using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(RedhatCardPool))]
// [RegisterCharacterStarterCard(typeof(RedhatCharacter), 1)]
public class LinkSummon() : BaseSummonCard(0, CardType.Skill, CardRarity.Basic, TargetType.None) {
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        return Task.CompletedTask;
    }
}