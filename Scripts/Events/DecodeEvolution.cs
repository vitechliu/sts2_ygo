using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using VYgo.Scripts.Cards.Category.Playmaker;

namespace VYgo.Scripts.Events;

[RegisterSharedEvent]
public sealed class DecodeEvolution : ModEventTemplate {
    private const string PortraitPath = "res://VYgo/images/events/decode_evolution.png";

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: PortraitPath
    );

    public override bool IsAllowed(IRunState runState) {
        return runState.Players.Count > 0
            && runState.Players.All(player =>
                player.Deck.Cards.Any(card => card is DecodeTalker));
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => [
        new EventOption(
            this,
            EvolveIntoIntegration,
            InitialOptionKey("INTEGRATION"),
            HoverTipFactory.FromCardWithCardHoverTips<DecodeTalkerIntegration>()),
        new EventOption(
            this,
            EvolveIntoHeatsoul,
            InitialOptionKey("HEATSOUL"),
            HoverTipFactory.FromCardWithCardHoverTips<DecodeTalkerHeatsoul>()),
        new EventOption(
            this,
            EvolveIntoExtended,
            InitialOptionKey("EXTENDED"),
            HoverTipFactory.FromCardWithCardHoverTips<DecodeTalkerExtended>()),
        new EventOption(this, LeaveUnchanged, InitialOptionKey("LEAVE"))
    ];

    private Task EvolveIntoIntegration() {
        return TransformDecodeTalkers<DecodeTalkerIntegration>("INTEGRATED");
    }

    private Task EvolveIntoHeatsoul() {
        return TransformDecodeTalkers<DecodeTalkerHeatsoul>("HEATSOUL_EVOLVED");
    }

    private Task EvolveIntoExtended() {
        return TransformDecodeTalkers<DecodeTalkerExtended>("EXTENDED_EVOLVED");
    }

    private async Task TransformDecodeTalkers<T>(string resultPage) where T : CardModel {
        var owner = Owner ?? throw new InvalidOperationException("事件玩家尚未就绪。");
        List<CardTransformation> transformations = [];

        foreach (DecodeTalker original in owner.Deck.Cards.OfType<DecodeTalker>().ToList()) {
            T replacement = owner.RunState.CreateCard<T>(owner);
            for (int i = 0; i < original.CurrentUpgradeLevel && replacement.IsUpgradable; i++) {
                CardCmd.Upgrade(replacement, CardPreviewStyle.None);
            }

            transformations.Add(new CardTransformation(original, replacement));
        }

        await CardCmd.Transform(transformations, null, CardPreviewStyle.EventLayout);
        SetEventFinished(PageDescription(resultPage));
    }

    private Task LeaveUnchanged() {
        SetEventFinished(PageDescription("LEFT"));
        return Task.CompletedTask;
    }
}
