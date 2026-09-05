using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.HoverTips;
using MinionLib.Targeting;
using VYgo.Core;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(CommonCardPool))]
public class ForbiddenCrown() : BaseSpellCard(1, CardType.Skill, CardRarity.Rare, MinionTargetTypes.AnyCreature) {
    public override int CardId => 98829635;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<ForbiddenCrownPower>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await PowerCmd.Apply<ForbiddenCrownPower>(
            choiceContext,
            cardPlay.Target,
            1m,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() {
        EnergyCost.UpgradeBy(-1);
    }
}
