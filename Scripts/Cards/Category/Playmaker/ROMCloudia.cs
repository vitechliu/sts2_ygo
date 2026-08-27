using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class ROMCloudia() : BaseMonsterCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.None) {
    public override int CardId => 44956694;

    public override int BaseAttackVar => 6;
    public override int BaseLifeVar => 1;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.EnterField(),
        YgoHoverTipConst.SpecialSummon()
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay
    ) {
        await base.OnPlay(choiceContext, cardPlay);
        if (cardPlay.IsAutoPlay) return;

        CardModel? selected = (await CardSelectCmd.FromCombatPile(
                choiceContext,
                PileType.Discard.GetPile(Owner),
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 1),
                IsCyberseMonster))
            .FirstOrDefault();
        if (selected != null) {
            await CardPileCmd.Add(selected, PileType.Hand);
        }
    }

    private static bool IsCyberseMonster(CardModel card) {
        return card is BaseMonsterCard monster
            && !monster.IsExtra
            && monster.YgoGetCore().IsRace(YgoRace.Cyberse);
    }
}
