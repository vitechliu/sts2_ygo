using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using VYgo.Core;
using VYgo.Scripts;

namespace VYgo.Patches;

[HarmonyPatch(typeof(NCardPileScreen), nameof(NCardPileScreen._Ready))]
public static class ExtraDeckPileScreenPatch {
    private static readonly ConditionalWeakTable<NCardPileScreen, ExtraDeckPileScreenController> Controllers = new();

    private static readonly AccessTools.FieldRef<NCardPileScreen, NCardGrid> GridRef =
        AccessTools.FieldRefAccess<NCardPileScreen, NCardGrid>("_grid");

    [HarmonyPostfix]
    public static void Postfix(NCardPileScreen __instance) {
        if (__instance.Pile.Type != Entry.ExtraPile || Controllers.TryGetValue(__instance, out _)) return;

        Player? owner = __instance.Pile.Cards.FirstOrDefault()?.Owner;
        if (owner == null
            || !LocalContext.IsMe(owner)
            || __instance.Pile != Entry.ExtraPile.GetPile(owner)) {
            return;
        }

        var controller = new ExtraDeckPileScreenController(
            __instance,
            GridRef(__instance),
            owner
        );
        Controllers.Add(__instance, controller);
        controller.Install();
    }
}

internal sealed class ExtraDeckPileScreenController(
    NCardPileScreen screen,
    NCardGrid grid,
    Player owner
) {
    private readonly CardPile _extraPile = screen.Pile;
    private readonly CardPile _handPile = PileType.Hand.GetPile(owner);
    private readonly CardPile _monsterPile = Entry.MonsterPile.GetPile(owner);
    private readonly HashSet<CardModel> _highlightedCards = [];
    private readonly Dictionary<NGridCardHolder, CardModel> _visibleHighlightAssignments = [];
    private NExtraDeckSummonPopup? _popup;
    private bool _refreshQueued;
    private bool _requestPending;
    private bool _disposed;

    public void Install() {
        grid.Connect(
            NCardGrid.SignalName.HolderPressed,
            Callable.From<NCardHolder>(OnHolderPressed)
        );

        _extraPile.ContentsChanged += QueueRefresh;
        _handPile.ContentsChanged += QueueRefresh;
        _monsterPile.ContentsChanged += QueueRefresh;
        owner.Creature.CombatState.CreaturesChanged += OnCreaturesChanged;
        CombatManager.Instance.StateTracker.CombatStateChanged += OnCombatStateChanged;
        CombatManager.Instance.PlayerActionsDisabledChanged += OnCombatStateChanged;
        CombatManager.Instance.TurnStarted += OnCombatStateChanged;
        CombatManager.Instance.TurnEnded += OnCombatStateChanged;
        screen.TreeExiting += Dispose;
        screen.AddChildSafely(NExtraDeckPileInteractionDriver.Create(this));

        RefreshAvailability();
    }

    private void OnHolderPressed(NCardHolder holder) {
        if (_requestPending
            || holder is not NGridCardHolder gridHolder
            || !DirectExtraDeckSummonNetAction.CanRequest(gridHolder.CardModel)) {
            return;
        }

        if (_popup != null && GodotObject.IsInstanceValid(_popup)) {
            if (_popup.Card == gridHolder.CardModel) {
                _popup.Remove();
                _popup = null;
                return;
            }

            _popup.Remove(returnFocus: false);
        }

        _popup = NExtraDeckSummonPopup.Create(
            gridHolder,
            TryConfirmSummon
        );
        screen.AddChildSafely(_popup);
    }

    private bool TryConfirmSummon(CardModel card) {
        if (_requestPending || !DirectExtraDeckSummonNetAction.Request(card)) {
            QueueRefresh();
            Entry.Logger.Warn($"Unable to request direct extra-deck summon for {card.Id}.");
            return false;
        }

        _requestPending = true;
        _popup = null;
        NCapstoneContainer.Instance?.Close();
        return true;
    }

    private void QueueRefresh() {
        if (_disposed || _refreshQueued) return;
        _refreshQueued = true;
        Callable.From(RefreshAvailability).CallDeferred();
    }

    private void RefreshAvailability() {
        _refreshQueued = false;
        if (_disposed || !GodotObject.IsInstanceValid(grid)) return;

        HashSet<CardModel> summonableCards = _extraPile.Cards
            .Where(DirectExtraDeckSummonNetAction.CanRequest)
            .ToHashSet();

        foreach (CardModel card in _highlightedCards.Except(summonableCards).ToList()) {
            grid.UnhighlightCard(card);
            _highlightedCards.Remove(card);
        }

        foreach (CardModel card in summonableCards.Except(_highlightedCards)) {
            grid.HighlightCard(card);
            _highlightedCards.Add(card);
        }

        RefreshVisibleHighlights();

        if (_popup != null
            && GodotObject.IsInstanceValid(_popup)
            && !summonableCards.Contains(_popup.Card)) {
            _popup.Remove();
            _popup = null;
        }
    }

    private void OnCreaturesChanged(ICombatState _) {
        QueueRefresh();
    }

    private void OnCombatStateChanged(CombatState _) {
        QueueRefresh();
    }

    internal void RefreshVisibleHighlights() {
        if (_disposed || !GodotObject.IsInstanceValid(grid)) return;

        HashSet<NGridCardHolder> visibleHolders = grid.CurrentlyDisplayedCardHolders.ToHashSet();
        foreach (NGridCardHolder holder in visibleHolders) {
            if (!_highlightedCards.Contains(holder.CardModel) || holder.CardNode?.CardHighlight == null) {
                _visibleHighlightAssignments.Remove(holder);
                continue;
            }

            holder.CardNode.CardHighlight.Modulate = NCardHighlight.gold;
            if (!_visibleHighlightAssignments.TryGetValue(holder, out CardModel? assignedCard)
                || assignedCard != holder.CardModel) {
                holder.CardNode.CardHighlight.AnimShow();
                _visibleHighlightAssignments[holder] = holder.CardModel;
            }
        }

        foreach (NGridCardHolder holder in _visibleHighlightAssignments.Keys.Except(visibleHolders).ToList()) {
            _visibleHighlightAssignments.Remove(holder);
        }
    }

    private void Dispose() {
        if (_disposed) return;
        _disposed = true;

        _extraPile.ContentsChanged -= QueueRefresh;
        _handPile.ContentsChanged -= QueueRefresh;
        _monsterPile.ContentsChanged -= QueueRefresh;
        owner.Creature.CombatState.CreaturesChanged -= OnCreaturesChanged;
        CombatManager.Instance.StateTracker.CombatStateChanged -= OnCombatStateChanged;
        CombatManager.Instance.PlayerActionsDisabledChanged -= OnCombatStateChanged;
        CombatManager.Instance.TurnStarted -= OnCombatStateChanged;
        CombatManager.Instance.TurnEnded -= OnCombatStateChanged;
        screen.TreeExiting -= Dispose;
    }
}

internal sealed partial class NExtraDeckPileInteractionDriver : Node {
    private ExtraDeckPileScreenController _controller = null!;

    public static NExtraDeckPileInteractionDriver Create(ExtraDeckPileScreenController controller) {
        return new NExtraDeckPileInteractionDriver {
            Name = nameof(NExtraDeckPileInteractionDriver),
            _controller = controller
        };
    }

    public override void _Process(double delta) {
        base._Process(delta);
        _controller.RefreshVisibleHighlights();
    }
}

internal sealed partial class NExtraDeckSummonPopup : PanelContainer {
    private static readonly Vector2 PopupSize = new(250f, 70f);
    private const float HolderGap = 18f;

    private NGridCardHolder _holder = null!;
    private Func<CardModel, bool> _tryConfirm = null!;
    private Button _confirmButton = null!;
    private bool _removing;

    public CardModel Card { get; private set; } = null!;

    public static NExtraDeckSummonPopup Create(
        NGridCardHolder holder,
        Func<CardModel, bool> tryConfirm
    ) {
        return new NExtraDeckSummonPopup {
            Name = nameof(NExtraDeckSummonPopup),
            _holder = holder,
            Card = holder.CardModel,
            _tryConfirm = tryConfirm,
            CustomMinimumSize = PopupSize,
            Size = PopupSize,
            MouseFilter = MouseFilterEnum.Stop,
            ZIndex = 100
        };
    }

    public override void _Ready() {
        AddThemeStyleboxOverride("panel", new StyleBoxFlat {
            BgColor = new Color(0.035f, 0.035f, 0.05f, 0.96f),
            BorderColor = new Color("d7ad43"),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            ContentMarginLeft = 8f,
            ContentMarginTop = 8f,
            ContentMarginRight = 8f,
            ContentMarginBottom = 8f
        });

        _confirmButton = new Button {
            Name = "ConfirmDirectSummon",
            Text = new LocString(
                "cards",
                "V_YGO_EXTRA_DECK_DIRECT_SUMMON.confirm"
            ).GetFormattedText(),
            FocusMode = FocusModeEnum.All,
            MouseDefaultCursorShape = CursorShape.PointingHand,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _confirmButton.AddThemeFontSizeOverride("font_size", 24);
        _confirmButton.AddThemeColorOverride("font_color", new Color("ffe39a"));
        _confirmButton.AddThemeColorOverride("font_hover_color", Colors.White);
        _confirmButton.Pressed += OnConfirmed;
        this.AddChildSafely(_confirmButton);

        UpdatePosition();
        Callable.From(_confirmButton.GrabFocus).CallDeferred();
    }

    public override void _Process(double delta) {
        base._Process(delta);
        if (_removing) return;

        if (!GodotObject.IsInstanceValid(_holder)
            || !_holder.IsInsideTree()
            || !_holder.Visible
            || _holder.CardModel != Card) {
            Remove(returnFocus: false);
            return;
        }

        UpdatePosition();
    }

    public override void _Input(InputEvent inputEvent) {
        if (_removing) return;

        if (inputEvent is InputEventMouseButton mouseButton
            && !mouseButton.Pressed
            && mouseButton.ButtonIndex is MouseButton.Left or MouseButton.Right
            && !GetGlobalRect().HasPoint(GetGlobalMousePosition())) {
            Remove();
            return;
        }

        if (inputEvent.IsActionPressed(MegaInput.cancel)) {
            Remove();
            GetViewport()?.SetInputAsHandled();
        }
    }

    public void Remove(bool returnFocus = true) {
        if (_removing) return;
        _removing = true;

        if (returnFocus && GodotObject.IsInstanceValid(_holder) && _holder.IsInsideTree()) {
            Callable.From(_holder.GrabFocus).CallDeferred();
        }

        this.QueueFreeSafely();
    }

    private void OnConfirmed() {
        if (_removing || _confirmButton.Disabled) return;
        _confirmButton.Disabled = true;

        if (_tryConfirm(Card)) {
            Remove(returnFocus: false);
        }
        else if (GodotObject.IsInstanceValid(_confirmButton)) {
            _confirmButton.Disabled = false;
        }
    }

    private void UpdatePosition() {
        Rect2 holderRect = _holder.GetGlobalRect();
        Vector2 desiredPosition = new(
            holderRect.End.X + HolderGap,
            holderRect.Position.Y + (holderRect.Size.Y - PopupSize.Y) * 0.5f
        );
        Vector2 viewportSize = GetViewportRect().Size;

        if (desiredPosition.X + PopupSize.X > viewportSize.X - HolderGap) {
            desiredPosition.X = holderRect.Position.X - PopupSize.X - HolderGap;
        }

        desiredPosition.X = Math.Clamp(desiredPosition.X, HolderGap, viewportSize.X - PopupSize.X - HolderGap);
        desiredPosition.Y = Math.Clamp(desiredPosition.Y, HolderGap, viewportSize.Y - PopupSize.Y - HolderGap);
        GlobalPosition = desiredPosition;
    }
}
