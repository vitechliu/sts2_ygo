using MegaCrit.Sts2.Core.Commands;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberEmergency() : BaseSpellCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.None) {
    public override int CardId => 60600126;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<NegatingPower>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        List<BaseMonsterCard> candidates = ModelDb.AllCards
            .OfType<BaseMonsterCard>()
            .Where(card => !card.IsExtra && card.ContainArchetype(YgoArchetypes.CyberDragon))
            .ToList();
        BaseMonsterCard? selectedCard = Owner.RunState.Rng.CombatCardSelection.NextItem(candidates);
        if (selectedCard != null) {
            CardModel generatedCard = CombatState.CreateCard(selectedCard, Owner);
            await CardPileCmd.AddGeneratedCardToCombat(generatedCard, PileType.Hand, Owner);
        }

        if (!IsUpgraded || Owner.Creature.GetPower<NegatingPower>() is not { Amount: > 0 } negatingPower) {
            return;
        }

        await PowerCmd.ModifyAmount(
            choiceContext,
            negatingPower,
            -1m,
            Owner.Creature,
            this);
        await CardPileCmd.AddGeneratedCardToCombat(CreateClone(), PileType.Hand, Owner);
    }
}
