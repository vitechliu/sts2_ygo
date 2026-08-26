using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;
using VYgo.Utils;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberRevsystem() : BaseSpellCard(0, CardType.Skill, CardRarity.Common, TargetType.None) {
    public override int CardId => 33041277;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.SpecialSummon()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        if (Owner.MinionCount() >= Owner.GetMaxMinionCount()) {
            return;
        }

        CardModel? selectedCard = (await CardSelectCmd.FromCombatPile(
                prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
                context: choiceContext,
                pile: PileType.Discard.GetPile(Owner),
                player: Owner,
                filter: IsCyberDragonMonster))
            .FirstOrDefault();
        if (selectedCard != null) {
            await CardCmd.AutoPlay(choiceContext, selectedCard, null);
        }
    }

    private static bool IsCyberDragonMonster(CardModel card) {
        return card is BaseMonsterCard monsterCard
               && monsterCard.ContainArchetype(YgoArchetypes.CyberDragon);
    }

    protected override void OnUpgrade() {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
