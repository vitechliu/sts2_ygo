using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Cards.Category.YgoEvent;

namespace VYgo.Scripts.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class MagicCircleCoreRelic : BaseYgoRelic {
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];

    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained() {
        List<CardPileAddResult> results = [
            await CardPileCmd.Add(Owner.RunState.CreateCard<MagicalMeltdown>(Owner), PileType.Deck),
            await CardPileCmd.Add(Owner.RunState.CreateCard<AleistertheInvoker>(Owner), PileType.Deck),
            await CardPileCmd.Add(Owner.RunState.CreateCard<Invocation>(Owner), PileType.Deck)
        ];
        CardCmd.PreviewCardPileAdd(results, 2f);
    }
}
