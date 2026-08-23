using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using VYgo.Scripts;

namespace VYgo.Core;

public sealed partial class NSummonMaterialSelectScreen : NCardGridSelectionScreen {
    private const string ScenePath = "res://VYgo/scenes/summon/material_select_screen.tscn";
    private const string OriginBadgeName = "SummonMaterialOriginBadge";

    private CardModel _targetCard = null!;
    private Player _owner = null!;
    private Func<SummonMaterialSelectionSpec?> _buildSpec = null!;
    private SummonMaterialSelectionSpec _spec = null!;

    private readonly List<SummonMaterial> _selectedMaterials = [];
    private NConfirmButton _confirmButton = null!;
    private NCombatPilesContainer _combatPiles = null!;
    private Control _bottomTextContainer = null!;
    private Control _targetTextContainer = null!;
    private MegaRichTextLabel _infoLabel = null!;
    private MegaRichTextLabel _targetLabel = null!;
    private readonly List<CardPile> _materialSourcePiles = [];
    private bool _showBlockedMessage;
    private bool _refreshQueued;
    private bool _completed;
    private bool _subscribed;

    protected override IEnumerable<Control> PeekButtonTargets => [
        _bottomTextContainer,
        _targetTextContainer
    ];

    public static NSummonMaterialSelectScreen? Create(
        CardModel targetCard,
        Player owner,
        Func<SummonMaterialSelectionSpec?> buildSpec
    ) {
        SummonMaterialSelectionSpec? spec = buildSpec();
        if (spec?.HasValidCombination != true) return null;

        PackedScene? scene = ResourceLoader.Load<PackedScene>(
            ScenePath,
            null,
            ResourceLoader.CacheMode.Reuse
        );
        if (scene == null) {
            Entry.Logger.Error("Failed to load summon material selection scene: " + ScenePath);
            return null;
        }

        NSummonMaterialSelectScreen screen = scene.Instantiate<NSummonMaterialSelectScreen>(
            PackedScene.GenEditState.Disabled
        );
        screen.Name = nameof(NSummonMaterialSelectScreen);
        screen._targetCard = targetCard;
        screen._owner = owner;
        screen._buildSpec = buildSpec;
        screen._spec = spec;
        screen._cards = spec.CandidateCards;
        return screen;
    }

    public override void _Ready() {
        EnsureOverlayLayout();
        ConnectSignalsAndInitGrid();

        _confirmButton = GetNode<NConfirmButton>("%Confirm");
        _combatPiles = GetNode<NCombatPilesContainer>("%CombatPiles");
        _bottomTextContainer = GetNode<Control>("%BottomText");
        _targetTextContainer = GetNode<Control>("%TargetText");
        _infoLabel = GetNode<MegaRichTextLabel>("%BottomLabel");
        _targetLabel = GetNode<MegaRichTextLabel>("%TargetLabel");

        _targetLabel.Text = FormatTargetText();
        _confirmButton.Disable();
        _confirmButton.Connect(
            NClickableControl.SignalName.Released,
            Callable.From<NButton>(_ => CompleteSelection())
        );

        _combatPiles.Initialize(_owner);
        _combatPiles.Disable();
        _combatPiles.SetVisible(false);
        _peekButton.Connect(
            NPeekButton.SignalName.Toggled,
            Callable.From<NPeekButton>(OnPeekButtonToggled)
        );

        SubscribeToMaterialSources();
        UpdateSelectionState();

        // The material screen is pushed immediately after the extra-deck card screen closes.
        // Reapply the layout after NOverlayStack has finished reordering its backstop and children.
        Callable.From(EnsureOverlayLayout).CallDeferred();
    }

    public override void AfterOverlayOpened() {
        base.AfterOverlayOpened();
        EnsureOverlayLayout();
    }

    public override void AfterOverlayShown() {
        base.AfterOverlayShown();
        EnsureOverlayLayout();
    }

    public override void _Process(double delta) {
        base._Process(delta);
        RefreshVisibleCardDecorations();
    }

    public override void _ExitTree() {
        UnsubscribeFromMaterialSources();
        base._ExitTree();
    }

    protected override void OnCardClicked(CardModel card) {
        SummonMaterial? material = _spec.GetMaterial(card);
        if (material == null) return;

        if (_selectedMaterials.Remove(material)) {
            _grid.UnhighlightCard(card);
            _showBlockedMessage = false;
            UpdateSelectionState();
            return;
        }

        if (!_spec.CanExtendSelection(_selectedMaterials, material)) {
            _showBlockedMessage = true;
            UpdateSelectionState();
            return;
        }

        _selectedMaterials.Add(material);
        _grid.HighlightCard(card);
        _showBlockedMessage = false;
        UpdateSelectionState();
    }

    private void CompleteSelection() {
        if (_completed || !_spec.IsValidSelection(_selectedMaterials)) return;

        CompleteWithCards(_selectedMaterials
            .Select(material => material.Card)
            .OfType<CardModel>());
    }

    private void EnsureOverlayLayout() {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        if (_grid != null && GodotObject.IsInstanceValid(_grid)) {
            _grid.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _grid.SetAnchorAndOffset(Side.Top, 0f, 80f);
        }
    }

    private void CompleteWithCards(IEnumerable<CardModel> cards) {
        if (_completed) return;
        _completed = true;
        UnsubscribeFromMaterialSources();
        _completionSource.TrySetResult(cards.ToList());

        if (NOverlayStack.Instance != null) {
            NOverlayStack.Instance.Remove(this);
        }
        else {
            this.QueueFreeSafely();
        }
    }

    private void QueueSpecRefresh() {
        if (_completed || _refreshQueued) return;
        _refreshQueued = true;
        Callable.From(RefreshSpec).CallDeferred();
    }

    private void RefreshSpec() {
        _refreshQueued = false;
        if (_completed) return;

        SummonMaterialSelectionSpec? refreshedSpec = _buildSpec();
        if (refreshedSpec?.HasValidCombination != true) {
            CompleteWithCards(Array.Empty<CardModel>());
            return;
        }

        List<CardModel> selectedCards = _selectedMaterials
            .Select(material => material.Card)
            .OfType<CardModel>()
            .ToList();
        foreach (SummonMaterial material in _selectedMaterials) {
            if (material.Card != null) {
                _grid.UnhighlightCard(material.Card);
            }
        }

        _spec = refreshedSpec;
        _cards = refreshedSpec.CandidateCards;
        _grid.SetCards(_cards, PileType.None, [SortingOrders.Ascending]);

        _selectedMaterials.Clear();
        foreach (CardModel card in selectedCards) {
            SummonMaterial? material = _spec.GetMaterial(card);
            if (material != null
                && _spec.CanExtendSelection(_selectedMaterials, material)) {
                _selectedMaterials.Add(material);
                _grid.HighlightCard(card);
            }
        }

        _showBlockedMessage = false;
        UpdateSelectionState();
    }

    private void UpdateSelectionState() {
        bool isValid = _spec.IsValidSelection(_selectedMaterials);
        if (isValid) {
            _confirmButton.Enable();
        }
        else {
            _confirmButton.Disable();
        }

        string statusKey = _showBlockedMessage
            ? "V_YGO_SUMMON_MATERIAL_SELECT.blocked"
            : isValid
                ? "V_YGO_SUMMON_MATERIAL_SELECT.ready"
                : "V_YGO_SUMMON_MATERIAL_SELECT.progress";
        LocString status = new("cards", statusKey);
        status.Add("Selected", (decimal)_selectedMaterials.Count);
        status.Add("Min", (decimal)_spec.MinSelect);
        status.Add("Max", (decimal)_spec.MaxSelect);
        _infoLabel.Text = status.GetFormattedText();

        RefreshVisibleCardDecorations();
    }

    private void RefreshVisibleCardDecorations() {
        if (_grid == null || _spec == null) return;

        foreach (NGridCardHolder holder in _grid.CurrentlyDisplayedCardHolders) {
            SummonMaterial? material = _spec.GetMaterial(holder.CardModel);
            if (material == null) continue;

            bool isSelected = _selectedMaterials.Contains(material);
            bool canSelect = isSelected || _spec.CanExtendSelection(_selectedMaterials, material);
            holder.Modulate = canSelect
                ? Colors.White
                : new Color(0.42f, 0.42f, 0.42f, 0.72f);
            UpdateOriginBadge(holder, material);
        }
    }

    private static void UpdateOriginBadge(NGridCardHolder holder, SummonMaterial material) {
        Label? badge = holder.GetNodeOrNull<Label>(OriginBadgeName);
        if (badge == null) {
            badge = new Label {
                Name = OriginBadgeName,
                Position = new Vector2(-142f, -205f),
                Size = new Vector2(76f, 32f),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore
            };
            badge.AddThemeFontSizeOverride("font_size", 19);
            badge.AddThemeStyleboxOverride("normal", new StyleBoxFlat {
                BgColor = new Color(0.03f, 0.03f, 0.03f, 0.88f),
                CornerRadiusTopLeft = 7,
                CornerRadiusTopRight = 7,
                CornerRadiusBottomLeft = 7,
                CornerRadiusBottomRight = 7
            });
            holder.AddChildSafely(badge);
        }

        string key = material.IsField
            ? "V_YGO_SUMMON_MATERIAL_SELECT.field"
            : material.SourcePile switch {
                PileType.Draw => "V_YGO_SUMMON_MATERIAL_SELECT.draw",
                PileType.Hand => "V_YGO_SUMMON_MATERIAL_SELECT.hand",
                PileType.Discard => "V_YGO_SUMMON_MATERIAL_SELECT.discard",
                PileType.Exhaust => "V_YGO_SUMMON_MATERIAL_SELECT.exhaust",
                _ when material.SourcePile == Entry.EquipPile =>
                    "V_YGO_SUMMON_MATERIAL_SELECT.equip",
                _ => "V_YGO_SUMMON_MATERIAL_SELECT.pile"
            };
        badge.Text = new LocString("cards", key).GetFormattedText();
        badge.AddThemeColorOverride(
            "font_color",
            material.IsField
                ? new Color("67d9ff")
                : material.SourcePile switch {
                    PileType.Draw => new Color("a8e6a3"),
                    PileType.Hand => new Color("ffd76a"),
                    PileType.Discard => new Color("c5b3ff"),
                    PileType.Exhaust => new Color("ff9a8f"),
                    _ when material.SourcePile == Entry.EquipPile => new Color("67d9ff"),
                    _ => Colors.White
                }
        );
    }

    private string FormatTargetText() {
        LocString text = new("cards", "V_YGO_SUMMON_MATERIAL_SELECT.target");
        text.Add("Target", _targetCard.Title);
        return text.GetFormattedText();
    }

    private void OnPeekButtonToggled(NPeekButton button) {
        if (button.IsPeeking) {
            _combatPiles.Enable();
            _combatPiles.SetVisible(true);
        }
        else {
            _combatPiles.Disable();
            _combatPiles.SetVisible(false);
        }
    }

    private void SubscribeToMaterialSources() {
        if (_subscribed) return;

        _materialSourcePiles.AddRange(new[] {
                PileType.Draw,
                PileType.Hand,
                PileType.Discard,
                PileType.Exhaust,
                Entry.MonsterPile,
                Entry.EquipPile
            }
            .Distinct()
            .Select(pileType => pileType.GetPile(_owner)));
        foreach (CardPile pile in _materialSourcePiles) {
            pile.ContentsChanged += QueueSpecRefresh;
        }
        _owner.Creature.CombatState.CreaturesChanged += OnCreaturesChanged;
        _subscribed = true;
    }

    private void UnsubscribeFromMaterialSources() {
        if (!_subscribed) return;

        foreach (CardPile pile in _materialSourcePiles) {
            pile.ContentsChanged -= QueueSpecRefresh;
        }
        _materialSourcePiles.Clear();
        _owner.Creature.CombatState.CreaturesChanged -= OnCreaturesChanged;
        _subscribed = false;
    }

    private void OnCreaturesChanged(ICombatState _) {
        QueueSpecRefresh();
    }
}
