using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class BalancerLord() : BaseMonsterCard(2, CardRarity.Common, TargetType.None) {
    public override int CardId => 8567955;

    public override int BaseAttackVar => 5;
    public override int BaseLifeVar => 4;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.Action(),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
    ];

    public override Task AfterCardExhausted(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool causedByEthereal
    ) {
        if (card != this || CombatState == null) return Task.CompletedTask;

        var candidates = PileType.Hand.GetPile(Owner).Cards
            .OfType<BaseMonsterCard>()
            .Where(handCard => handCard.YgoGetCore().IsRace(YgoRace.Cyberse))
            .ToList();
        var selected = Owner.RunState.Rng.CombatCardSelection.NextItem(candidates);
        selected?.EnergyCost.SetThisTurnOrUntilPlayed(0);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade() {
        base.OnUpgrade();
        EnergyCost.UpgradeBy(-1);
    }
}
