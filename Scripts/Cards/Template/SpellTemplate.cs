using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using VYgo.Core;

namespace VYgo.Scripts.Cards.Template;

// [RegisterCard(typeof(ZaneTruesdaleCardPool))] //需要选择卡牌Pool
public abstract class SpellTemplate() : BaseSpellCard(1, CardType.Attack, CardRarity.Common, TargetType.None) {
    public override int CardId => -1;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        await Task.CompletedTask;
    }
}

