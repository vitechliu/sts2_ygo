using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Pools;
using VYgo.Utils;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class BalancerLord() : BaseMonsterCard(2, CardRarity.Common, TargetType.None) {
    public override int CardId => 8567955;

    public override int BaseAttackVar => 5;
    public override int BaseLifeVar => 4;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.Action(),
        YgoHoverTipConst.SpecialSummon(),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
    ];

    public override async Task AfterCardExhausted(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool causedByEthereal
    ) {
        if (card != this
            || CombatState == null
            || Owner.MinionCount() >= Owner.GetMaxMinionCount()) {
            return;
        }

        BaseMonsterCard? selected = (await CardSelectCmd.FromHand(
                choiceContext,
                Owner,
                new CardSelectorPrefs(
                    new LocString("cards", "V_YGO_CARD_BALANCER_LORD.exhaustSelectionScreenPrompt"),
                    1),
                IsCyberseMonster,
                this))
            .OfType<BaseMonsterCard>()
            .FirstOrDefault();
        if (selected != null && Owner.MinionCount() < Owner.GetMaxMinionCount()) {
            await CardCmd.AutoPlay(choiceContext, selected, null);
        }
    }

    protected override void OnUpgrade() {
        base.OnUpgrade();
        EnergyCost.UpgradeBy(-1);
    }

    private static bool IsCyberseMonster(CardModel card) {
        return card is BaseMonsterCard monster
            && !monster.IsExtra
            && monster.YgoGetCore().IsRace(YgoRace.Cyberse);
    }
}
