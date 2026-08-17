using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Test;

[RegisterCard(typeof(RedhatCardPool))]
public class TestYgoCard() : BaseVYgoCard(1, CardType.Skill, CardRarity.Common, TargetType.None) {
    //测试一回合一次
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        if (this.CanUseEffectByCard(CombatState, cardPlay)) {
            Entry.Logger.Info("CanUseTestCard");
        }
        else {
            Entry.Logger.Info("AlreadyUsedTestCard");
        }
    }

    public override int CardId => 0;
}
