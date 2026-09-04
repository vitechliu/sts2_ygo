using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using VYgo.Scripts;
using VYgo.Scripts.Cards.Category.Common;
using VYgo.Scripts.Characters;

namespace VYgo.Scripts.Events;

[RegisterSharedEvent]
public sealed class FiendsmithForge : ModEventTemplate {
    private const string FiendsmithCardCountKey = "FiendsmithCards";
    private const string UpgradeCardCountKey = "UpgradeCards";
    private const string PortraitPath = "res://VYgo/images/events/fiendsmith_forge.png";

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new HpLossVar(10m),
        new GoldVar(15),
        new CardsVar(FiendsmithCardCountKey, 7),
        new CardsVar(UpgradeCardCountKey, 1)
    ];

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: PortraitPath
    );

    // public override IEnumerable<string> GetAssetPaths(IRunState runState) {
    //     string defaultPortraitPath = ImageHelper.GetImagePath(
    //         $"events/{Id.Entry.ToLowerInvariant()}.png"
    //     );
    //     return base.GetAssetPaths(runState)
    //         .Where(path => !string.Equals(path, defaultPortraitPath, StringComparison.Ordinal))
    //         .Append(PortraitPath)
    //         .Distinct();
    // }

    public override bool IsAllowed(IRunState runState) {
        // Act 序号从 0 开始；使用下限判断以兼容其他模组追加的 Act 4 及以后章节。
        // 只要队伍中至少有一名 YGO 角色，联机队伍中的所有玩家就都能遇到该事件。
        return runState.CurrentActIndex >= 1
            && runState.Players.Any(player => player.IsYgoCharacter());
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() {
        var owner = Owner ?? throw new InvalidOperationException("事件玩家尚未就绪。");
        bool canForge = (decimal)owner.Gold >= DynamicVars.Gold.BaseValue
            && owner.Deck.Cards.Any(card => card.IsUpgradable);

        EventOption forgeOption = canForge
            ? new EventOption(this, ForgeCard, InitialOptionKey("FORGE"))
            : new EventOption(this, null, InitialOptionKey("FORGE_LOCKED"));

        return [
            new EventOption(this, JoinFiendsmiths, InitialOptionKey("JOIN"))
                .ThatDoesDamage(DynamicVars.HpLoss.BaseValue),
            forgeOption,
            new EventOption(this, Leave, InitialOptionKey("LEAVE"))
        ];
    }

    private async Task JoinFiendsmiths() {
        var owner = Owner ?? throw new InvalidOperationException("事件玩家尚未就绪。");
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            owner.Creature,
            DynamicVars.HpLoss.BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);

        List<CardCreationResult> candidates = [
            new(owner.RunState.CreateCard<FiendsmithEngraver>(owner)),
            new(owner.RunState.CreateCard<LacrimatheCrimsonTears>(owner)),
            new(owner.RunState.CreateCard<FiendsmithsTract>(owner)),
            new(owner.RunState.CreateCard<FiendsmithsSanct>(owner)),
            new(owner.RunState.CreateCard<FiendsmithKyrie>(owner)),
            new(owner.RunState.CreateCard<FiendsmithsLacrima>(owner)),
            new(owner.RunState.CreateCard<FiendsmithsDesirae>(owner)),
            new(owner.RunState.CreateCard<FiendsmithsRequiem>(owner)),
            new(owner.RunState.CreateCard<FiendsmithsSequence>(owner))
        ];

        var selectionPrompt = new LocString("events", $"{Id.Entry}.selectionScreenPrompt");
        DynamicVars.AddTo(selectionPrompt);
        var selectionPrefs = new CardSelectorPrefs(
            selectionPrompt,
            DynamicVars[FiendsmithCardCountKey].IntValue
        ) {
            Cancelable = false
        };
        await SelectCardsToAddToDeckFromGrid(candidates, selectionPrefs);
        SetEventFinished(PageDescription("JOINED"));
    }

    private async Task ForgeCard() {
        var owner = Owner ?? throw new InvalidOperationException("事件玩家尚未就绪。");
        await PlayerCmd.LoseGold(DynamicVars.Gold.BaseValue, owner, GoldLossType.Spent);

        CardModel? selected = (await CardSelectCmd.FromDeckForUpgrade(
                owner,
                new CardSelectorPrefs(
                    CardSelectorPrefs.UpgradeSelectionPrompt,
                    DynamicVars[UpgradeCardCountKey].IntValue)))
            .FirstOrDefault();
        if (selected != null) {
            CardCmd.Upgrade(selected, CardPreviewStyle.EventLayout);
        }

        SetEventFinished(PageDescription("FORGED"));
    }

    private Task Leave() {
        SetEventFinished(PageDescription("LEFT"));
        return Task.CompletedTask;
    }
}
