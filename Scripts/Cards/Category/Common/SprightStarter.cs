using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using VYgo.Core;
using VYgo.Utils;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(CommonCardPool))]
public class SprightStarter() : BaseSpellCard(1, CardType.Skill, CardRarity.Event, TargetType.None) {
    public override int CardId => 15443125;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.SpecialSummon()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        if (Owner.MinionCount() >= MinionUtil.MaxMinionCount) return;

        BaseMonsterCard? selected = (await CardSelectCmd.FromCombatPile(
                choiceContext,
                PileType.Draw.GetPile(Owner),
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 1),
                IsLevel2Monster))
            .OfType<BaseMonsterCard>()
            .FirstOrDefault();
        if (selected == null || Owner.MinionCount() >= MinionUtil.MaxMinionCount) return;

        await selected.AutoPlayAndCaptureSummonedCreature(choiceContext, null);
    }

    protected override void OnUpgrade() {
        EnergyCost.UpgradeBy(-1);
    }

    private static bool IsLevel2Monster(CardModel card) {
        return card is BaseMonsterCard monster
            && !monster.IsExtra
            && YgoSummonRules.IsLevel2OrRank2(monster.YgoGetCore());
    }
}
