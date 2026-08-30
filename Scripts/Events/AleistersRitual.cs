using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.YgoEvent;
using VYgo.Scripts.Relics;

namespace VYgo.Scripts.Events;

[RegisterActEvent(typeof(Overgrowth))]
[RegisterActEvent(typeof(Underdocks))]
public sealed class AleistersRitual : ModEventTemplate {
    private const string InvokedCardCountKey = "InvokedCards";
    private const string InvokedCandidateCountKey = "InvokedCandidates";
    private const string PotionCountKey = "Potions";
    private const string RareFusionCountKey = "RareFusionCards";
    private const string PortraitPath = "res://VYgo/images/events/aleisters_ritual.png";

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new HpLossVar(8m),
        new GoldVar(20),
        new CardsVar(InvokedCardCountKey, 1),
        new CardsVar(InvokedCandidateCountKey, 6),
        new DynamicVar(PotionCountKey, 3m),
        new CardsVar(RareFusionCountKey, 1)
    ];

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: PortraitPath
    );

    public override bool IsAllowed(IRunState runState) {
        return runState.Players.Any(player => player.IsYgoCharacter());
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() {
        var owner = Owner ?? throw new InvalidOperationException("事件玩家尚未就绪。");
        EventOption alchemyOption = (decimal)owner.Gold >= DynamicVars.Gold.BaseValue
            ? new EventOption(this, BuyPotions, InitialOptionKey("ALCHEMY"))
            : new EventOption(this, null, InitialOptionKey("ALCHEMY_LOCKED"));

        return [
            new EventOption(
                    this,
                    OfferSoul,
                    InitialOptionKey("OFFER_SOUL"),
                    HoverTipFactory.FromRelic<MagicCircleCoreRelic>())
                .ThatDecreasesMaxHp(DynamicVars.HpLoss.BaseValue),
            alchemyOption,
            new EventOption(this, AcceptContract, InitialOptionKey("CONTRACT"))
        ];
    }

    private async Task OfferSoul() {
        var owner = Owner ?? throw new InvalidOperationException("事件玩家尚未就绪。");
        await CreatureCmd.LoseMaxHp(
            new ThrowingPlayerChoiceContext(),
            owner.Creature,
            DynamicVars.HpLoss.BaseValue,
            isFromCard: false);
        await RelicCmd.Obtain<MagicCircleCoreRelic>(owner);

        List<CardCreationResult> candidates = [
            new(owner.RunState.CreateCard<InvokedMechaba>(owner)),
            new(owner.RunState.CreateCard<InvokedCaliga>(owner)),
            new(owner.RunState.CreateCard<InvokedMagellanica>(owner)),
            new(owner.RunState.CreateCard<InvokedPurgatrio>(owner)),
            new(owner.RunState.CreateCard<InvokedCocytus>(owner)),
            new(owner.RunState.CreateCard<InvokedRaidjin>(owner))
        ];

        var selectionPrompt = new LocString("events", $"{Id.Entry}.selectionScreenPrompt");
        DynamicVars.AddTo(selectionPrompt);
        var selectionPrefs = new CardSelectorPrefs(
            selectionPrompt,
            DynamicVars[InvokedCardCountKey].IntValue
        ) {
            Cancelable = false
        };
        await SelectCardsToAddToDeckFromGrid(candidates, selectionPrefs);
        SetEventFinished(PageDescription("SOUL_OFFERED"));
    }

    private async Task BuyPotions() {
        var owner = Owner ?? throw new InvalidOperationException("事件玩家尚未就绪。");
        await PlayerCmd.LoseGold(DynamicVars.Gold.BaseValue, owner, GoldLossType.Spent);

        List<Reward> rewards = Enumerable.Range(0, DynamicVars[PotionCountKey].IntValue)
            .Select(_ => (Reward)new PotionReward(owner))
            .ToList();
        await RewardsCmd.OfferCustom(owner, rewards);
        SetEventFinished(PageDescription("ALCHEMY_COMPLETED"));
    }

    private async Task AcceptContract() {
        var owner = Owner ?? throw new InvalidOperationException("事件玩家尚未就绪。");
        List<BaseExtraFusionCard> candidates = ModelDb.AllCards
            .OfType<BaseExtraFusionCard>()
            .Where(card => card.Rarity == CardRarity.Rare && card.IsUpgradable)
            .ToList();
        if (candidates.Count == 0) {
            throw new InvalidOperationException("未找到可作为召唤契约奖励的稀有融合怪兽卡。");
        }

        CardModel reward = owner.RunState.CreateCard(Rng.NextItem(candidates), owner);
        CardCmd.Upgrade(reward, CardPreviewStyle.None);
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.Add(reward, PileType.Deck),
            2f);
        SetEventFinished(PageDescription("CONTRACT_ACCEPTED"));
    }
}
