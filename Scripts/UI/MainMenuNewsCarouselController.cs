using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using FileAccess = Godot.FileAccess;

namespace VYgo.Scripts.UI;

/// <summary>
/// 负责主菜单左下角新闻轮播的展示、输入、自动切换和临时数据绑定。
/// </summary>
internal sealed class MainMenuNewsCarouselController {
    private const string CarouselName = "VYgoMainMenuNewsCarousel";
    private const string CarouselScenePath =
        "res://VYgo/scenes/main_menu/news_carousel/main_menu_news_carousel.tscn";
    private const string HoverSfx = "event:/sfx/ui/clicks/ui_hover";
    private const string ClickSfx = "event:/sfx/ui/clicks/ui_click";
    private const double AutoAdvanceSeconds = 6.0;
    private const double TransitionSeconds = 0.28;

    private static readonly Vector2 CarouselSize = new(700f, 390f);
    private static readonly NewsCarouselItem[] PlaceholderItems = [
        new(
            "NEWS_CAROUSEL_1_TITLE",
            "NEWS_CAROUSEL_1_DETAIL",
            "res://VYgo/images/cards/70095154.png"),
        new(
            "NEWS_CAROUSEL_2_TITLE",
            "NEWS_CAROUSEL_2_DETAIL",
            "res://VYgo/images/cards/59281922.png"),
        new(
            "NEWS_CAROUSEL_3_TITLE",
            "NEWS_CAROUSEL_3_DETAIL",
            "res://VYgo/images/cards/39439590.png")
    ];

    private readonly NMainMenu _mainMenu;
    private readonly MainMenuLeftMenuController _leftMenuController;
    private readonly IReadOnlyList<NewsCarouselItem> _items;
    private readonly List<Button> _indicatorButtons = [];

    private Control? _root;
    private TextureRect _backgroundA = null!;
    private TextureRect _backgroundB = null!;
    private TextureRect _activeBackground = null!;
    private TextureRect _inactiveBackground = null!;
    private Control _fallbackBackground = null!;
    private Label _titleLabel = null!;
    private Label _detailLabel = null!;
    private Button _previousButton = null!;
    private Button _nextButton = null!;
    private HBoxContainer _indicatorHost = null!;
    private Dictionary<string, string> _localizedValues = [];
    private Tween? _backgroundTween;
    private NButton? _linkedMenuButton;
    private int _currentIndex;
    private int _navigationStateHash;
    private double _autoAdvanceElapsed;
    private bool _wasInteractionPaused;

    public MainMenuNewsCarouselController(
        NMainMenu mainMenu,
        MainMenuLeftMenuController leftMenuController
    ) {
        _mainMenu = mainMenu;
        _leftMenuController = leftMenuController;
        _items = PlaceholderItems;
    }

    public void Install() {
        if (_mainMenu.GetNodeOrNull<Control>(CarouselName) != null) return;

        PackedScene? scene = ResourceLoader.Load<PackedScene>(CarouselScenePath);
        Control? root = scene?.InstantiateOrNull<Control>();
        if (root == null) {
            Entry.Logger.Warn($"无法实例化主菜单新闻轮播场景：{CarouselScenePath}");
            return;
        }

        root.Name = CarouselName;
        root.SetAnchorsPreset(Control.LayoutPreset.BottomLeft, keepOffsets: false);
        root.OffsetLeft = 96f;
        root.OffsetTop = -418f;
        root.OffsetRight = 96f + CarouselSize.X;
        root.OffsetBottom = -28f;
        _mainMenu.AddChild(root);

        Control? blurBackstop = _mainMenu.GetNodeOrNull<Control>("%BlurBackstop");
        if (blurBackstop != null) {
            _mainMenu.MoveChild(root, blurBackstop.GetIndex());
        }
        else {
            Entry.Logger.Warn("主菜单缺少 BlurBackstop，新闻轮播将保留在当前层级。");
        }

        _root = root;
        if (!TryBindSceneNodes(root)) {
            root.QueueFree();
            _root = null;
            return;
        }

        ConfigureArrowButton(_previousButton, () => SelectRelative(-1, manual: true));
        ConfigureArrowButton(_nextButton, () => SelectRelative(1, manual: true));
        BuildIndicators();
        LoadLocalizedValues();

        if (_items.Count == 0) {
            root.Visible = false;
            Entry.Logger.Warn("主菜单新闻轮播没有可显示的数据。");
            return;
        }

        ShowItem(0, animate: false);
        UpdateNavigationVisibility();
        UpdateFocusNavigation(force: true);
        Entry.Logger.Info($"主菜单新闻轮播已加载，共 {_items.Count} 条占位新闻。");
    }

    private bool TryBindSceneNodes(Control root) {
        _backgroundA = root.GetNodeOrNull<TextureRect>("%BackgroundA")!;
        _backgroundB = root.GetNodeOrNull<TextureRect>("%BackgroundB")!;
        _fallbackBackground = root.GetNodeOrNull<Control>("%FallbackBackground")!;
        _titleLabel = root.GetNodeOrNull<Label>("%TitleLabel")!;
        _detailLabel = root.GetNodeOrNull<Label>("%DetailLabel")!;
        _previousButton = root.GetNodeOrNull<Button>("%PreviousButton")!;
        _nextButton = root.GetNodeOrNull<Button>("%NextButton")!;
        _indicatorHost = root.GetNodeOrNull<HBoxContainer>("%IndicatorHost")!;

        if (_backgroundA == null
            || _backgroundB == null
            || _fallbackBackground == null
            || _titleLabel == null
            || _detailLabel == null
            || _previousButton == null
            || _nextButton == null
            || _indicatorHost == null) {
            Entry.Logger.Warn("主菜单新闻轮播场景缺少必要节点，已跳过模块安装。");
            return false;
        }

        _activeBackground = _backgroundA;
        _inactiveBackground = _backgroundB;
        return true;
    }

    private void ConfigureArrowButton(Button button, Action onPressed) {
        button.Pressed += () => {
            SfxCmd.Play(ClickSfx);
            onPressed();
        };
        button.FocusEntered += () => {
            SfxCmd.Play(HoverSfx);
            UpdateButtonVisual(button);
        };
        button.FocusExited += () => UpdateButtonVisual(button);
        button.MouseEntered += () => {
            SfxCmd.Play(HoverSfx);
            UpdateButtonVisual(button);
        };
        button.MouseExited += () => UpdateButtonVisual(button);
    }

    private void BuildIndicators() {
        foreach (Node child in _indicatorHost.GetChildren()) {
            child.QueueFree();
        }
        _indicatorButtons.Clear();

        for (int index = 0; index < _items.Count; index++) {
            int targetIndex = index;
            var indicator = new Button {
                Name = $"Indicator{index + 1}",
                CustomMinimumSize = new Vector2(18f, 18f),
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                FocusMode = Control.FocusModeEnum.All,
                MouseDefaultCursorShape = Control.CursorShape.PointingHand,
                PivotOffset = new Vector2(9f, 9f),
                TooltipText = $"{index + 1} / {_items.Count}"
            };
            indicator.AddThemeStyleboxOverride("normal", CreateIndicatorStyle(selected: false));
            indicator.AddThemeStyleboxOverride("hover", CreateIndicatorStyle(selected: false, highlighted: true));
            indicator.AddThemeStyleboxOverride("pressed", CreateIndicatorStyle(selected: true));
            indicator.AddThemeStyleboxOverride("focus", CreateIndicatorFocusStyle());
            indicator.Pressed += () => {
                SfxCmd.Play(ClickSfx);
                SelectIndex(targetIndex, manual: true);
            };
            indicator.FocusEntered += () => {
                SfxCmd.Play(HoverSfx);
                UpdateButtonVisual(indicator);
            };
            indicator.FocusExited += () => UpdateButtonVisual(indicator);
            indicator.MouseEntered += () => {
                SfxCmd.Play(HoverSfx);
                UpdateButtonVisual(indicator);
            };
            indicator.MouseExited += () => UpdateButtonVisual(indicator);

            _indicatorHost.AddChild(indicator);
            _indicatorButtons.Add(indicator);
        }
    }

    private static StyleBoxFlat CreateIndicatorStyle(bool selected, bool highlighted = false) {
        Color color = selected
            ? new Color("f7ffff")
            : highlighted
                ? new Color("8eeeff")
                : new Color(0.58f, 0.63f, 0.65f, 0.9f);
        return new StyleBoxFlat {
            BgColor = color,
            BorderColor = selected ? new Color("62dff2") : new Color(0.08f, 0.12f, 0.14f, 0.9f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 16,
            CornerRadiusTopRight = 16,
            CornerRadiusBottomRight = 16,
            CornerRadiusBottomLeft = 16
        };
    }

    private static StyleBoxFlat CreateIndicatorFocusStyle() {
        return new StyleBoxFlat {
            BgColor = Colors.Transparent,
            BorderColor = new Color("ffab3d"),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 16,
            CornerRadiusTopRight = 16,
            CornerRadiusBottomRight = 16,
            CornerRadiusBottomLeft = 16,
            ExpandMarginLeft = 3f,
            ExpandMarginTop = 3f,
            ExpandMarginRight = 3f,
            ExpandMarginBottom = 3f
        };
    }

    private static void UpdateButtonVisual(Button button) {
        bool highlighted = button.HasFocus() || button.IsHovered();
        Tween tween = button.CreateTween().SetParallel();
        tween.TweenProperty(button, "scale", highlighted ? Vector2.One * 1.08f : Vector2.One, 0.1)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(button, "modulate", highlighted ? new Color("fff1cb") : Colors.White, 0.1);
    }

    public void Update(double delta) {
        if (_root == null
            || !GodotObject.IsInstanceValid(_root)
            || !GodotObject.IsInstanceValid(_mainMenu)) {
            return;
        }

        bool shouldShow = _items.Count > 0
            && !_mainMenu.SubmenuStack.SubmenusOpen
            && !_mainMenu.PatchNotesScreen.IsOpen;
        if (_root.Visible != shouldShow) {
            _root.Visible = shouldShow;
            _autoAdvanceElapsed = 0.0;
            _wasInteractionPaused = false;
            if (!shouldShow) {
                ReleaseCarouselFocus();
            }
            UpdateFocusNavigation(force: true);
        }

        if (!shouldShow || _items.Count <= 1) return;

        UpdateFocusNavigation();
        bool interactionPaused = IsInteractionPaused();
        if (interactionPaused) {
            _wasInteractionPaused = true;
            return;
        }

        if (_wasInteractionPaused) {
            _wasInteractionPaused = false;
            _autoAdvanceElapsed = 0.0;
        }

        _autoAdvanceElapsed += delta;
        if (_autoAdvanceElapsed >= AutoAdvanceSeconds) {
            _autoAdvanceElapsed = 0.0;
            SelectRelative(1, manual: false);
        }
    }

    private bool IsInteractionPaused() {
        if (_root == null) return false;

        Control? focusOwner = _root.GetViewport().GuiGetFocusOwner();
        bool ownsFocus = focusOwner != null
            && (focusOwner == _root || _root.IsAncestorOf(focusOwner));
        bool containsMouse = _root.GetGlobalRect().HasPoint(_root.GetGlobalMousePosition());
        return ownsFocus || containsMouse;
    }

    private void ReleaseCarouselFocus() {
        if (_root == null) return;
        Control? focusOwner = _root.GetViewport().GuiGetFocusOwner();
        if (focusOwner != null && _root.IsAncestorOf(focusOwner)) {
            focusOwner.ReleaseFocus();
        }
    }

    private void SelectRelative(int direction, bool manual) {
        if (_items.Count == 0) return;
        int targetIndex = (_currentIndex + direction + _items.Count) % _items.Count;
        SelectIndex(targetIndex, manual);
    }

    private void SelectIndex(int targetIndex, bool manual) {
        if (_items.Count == 0) return;
        if (manual) {
            _autoAdvanceElapsed = 0.0;
        }

        int normalizedIndex = (targetIndex % _items.Count + _items.Count) % _items.Count;
        if (normalizedIndex == _currentIndex) {
            UpdateIndicatorStates();
            return;
        }
        ShowItem(normalizedIndex, animate: true);
    }

    private void ShowItem(int index, bool animate) {
        StopBackgroundTransition();
        _currentIndex = index;

        NewsCarouselItem item = _items[index];
        Texture2D? texture = LoadOptionalTexture(item.BackgroundPath);
        if (texture == null) {
            Entry.Logger.Warn($"新闻轮播背景图缺失，使用颜色回退：{item.BackgroundPath}");
        }

        ApplyCurrentText();
        UpdateIndicatorStates();

        if (!animate) {
            _activeBackground.Texture = texture;
            _activeBackground.Visible = texture != null;
            _activeBackground.Modulate = Colors.White;
            _inactiveBackground.Visible = false;
            _inactiveBackground.Modulate = Colors.Transparent;
            _fallbackBackground.Visible = texture == null;
            return;
        }

        TextureRect oldBackground = _activeBackground;
        TextureRect newBackground = _inactiveBackground;
        bool oldBackgroundMissing = oldBackground.Texture == null;
        newBackground.Texture = texture;
        newBackground.Visible = texture != null;
        newBackground.Modulate = Colors.Transparent;
        _fallbackBackground.Visible = oldBackgroundMissing || texture == null;

        Tween tween = _root!.CreateTween().SetParallel();
        _backgroundTween = tween;
        tween.TweenProperty(oldBackground, "modulate:a", 0f, TransitionSeconds)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Cubic);
        if (texture != null) {
            tween.TweenProperty(newBackground, "modulate:a", 1f, TransitionSeconds)
                .SetEase(Tween.EaseType.InOut)
                .SetTrans(Tween.TransitionType.Cubic);
        }
        tween.Chain().TweenCallback(Callable.From(() => {
            oldBackground.Visible = false;
            oldBackground.Modulate = Colors.Transparent;
            newBackground.Modulate = Colors.White;
            _activeBackground = newBackground;
            _inactiveBackground = oldBackground;
            _fallbackBackground.Visible = newBackground.Texture == null;
            _backgroundTween = null;
        }));
    }

    private void StopBackgroundTransition() {
        if (_backgroundTween == null) return;

        _backgroundTween.Kill();
        _backgroundTween = null;
        _activeBackground.Visible = _activeBackground.Texture != null;
        _activeBackground.Modulate = Colors.White;
        _inactiveBackground.Visible = false;
        _inactiveBackground.Modulate = Colors.Transparent;
        _fallbackBackground.Visible = _activeBackground.Texture == null;
    }

    private static Texture2D? LoadOptionalTexture(string path) {
        return ResourceLoader.Exists(path) ? ResourceLoader.Load<Texture2D>(path) : null;
    }

    public void RefreshTexts() {
        if (_root == null || _items.Count == 0) return;
        LoadLocalizedValues();
        ApplyCurrentText();
    }

    private void LoadLocalizedValues() {
        string locale = TranslationServer.GetLocale();
        string language = locale.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? "zhs" : "eng";
        string path = $"res://VYgo/localization/{language}/main_menu.json";
        try {
            string json = FileAccess.GetFileAsString(path);
            _localizedValues = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
        }
        catch (Exception exception) {
            _localizedValues = [];
            Entry.Logger.Warn($"读取新闻轮播本地化失败（{path}）：{exception.Message}");
        }
    }

    private void ApplyCurrentText() {
        NewsCarouselItem item = _items[_currentIndex];
        _titleLabel.Text = ResolveLocalizedText(item.TitleKey);
        _detailLabel.Text = ResolveLocalizedText(item.DetailKey);
    }

    private string ResolveLocalizedText(string key) {
        if (_localizedValues.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)) {
            return value;
        }

        return key switch {
            "NEWS_CAROUSEL_1_TITLE" => "Don't miss out!",
            "NEWS_CAROUSEL_1_DETAIL" => "The Cyber Dragon series is now available in VYgo.",
            "NEWS_CAROUSEL_2_TITLE" => "Card Spotlight",
            "NEWS_CAROUSEL_2_DETAIL" => "Cyber Dragon Drei is ready to reinforce your Machine strategy.",
            "NEWS_CAROUSEL_3_TITLE" => "Featured This Week",
            "NEWS_CAROUSEL_3_DETAIL" => "Special Summon Cyber Dinosaur and turn the duel around.",
            _ => key
        };
    }

    private void UpdateNavigationVisibility() {
        bool hasMultipleItems = _items.Count > 1;
        _previousButton.Visible = hasMultipleItems;
        _nextButton.Visible = hasMultipleItems;
        _indicatorHost.Visible = hasMultipleItems;
    }

    private void UpdateIndicatorStates() {
        for (int index = 0; index < _indicatorButtons.Count; index++) {
            Button indicator = _indicatorButtons[index];
            bool selected = index == _currentIndex;
            indicator.AddThemeStyleboxOverride("normal", CreateIndicatorStyle(selected));
            indicator.TooltipText = $"{index + 1} / {_items.Count}";
        }
        UpdateFocusNavigation(force: true);
    }

    private void UpdateFocusNavigation(bool force = false) {
        if (_root == null) return;

        NButton[] menuButtons = _leftMenuController.GetVisibleButtons();
        int stateHash = HashCode.Combine(_root.Visible, _items.Count, _currentIndex);
        foreach (NButton button in menuButtons) {
            stateHash = HashCode.Combine(stateHash, button.GetInstanceId());
        }
        if (!force && stateHash == _navigationStateHash) return;
        _navigationStateHash = stateHash;

        if (_linkedMenuButton != null
            && GodotObject.IsInstanceValid(_linkedMenuButton)) {
            _linkedMenuButton.FocusNeighborBottom = new NodePath("");
        }
        _linkedMenuButton = null;

        if (!_root.Visible || _items.Count <= 1 || menuButtons.Length == 0) return;

        NButton menuButton = menuButtons[^1];
        _linkedMenuButton = menuButton;
        NodePath menuPath = menuButton.GetPath();
        NodePath previousPath = _previousButton.GetPath();
        NodePath nextPath = _nextButton.GetPath();
        NodePath selectedIndicatorPath = _indicatorButtons[_currentIndex].GetPath();

        menuButton.FocusNeighborBottom = previousPath;

        _previousButton.FocusNeighborTop = menuPath;
        _previousButton.FocusNeighborLeft = previousPath;
        _previousButton.FocusNeighborRight = nextPath;
        _previousButton.FocusNeighborBottom = selectedIndicatorPath;

        _nextButton.FocusNeighborTop = menuPath;
        _nextButton.FocusNeighborLeft = previousPath;
        _nextButton.FocusNeighborRight = nextPath;
        _nextButton.FocusNeighborBottom = selectedIndicatorPath;

        for (int index = 0; index < _indicatorButtons.Count; index++) {
            Button indicator = _indicatorButtons[index];
            indicator.FocusNeighborTop = menuPath;
            indicator.FocusNeighborLeft = index == 0
                ? previousPath
                : _indicatorButtons[index - 1].GetPath();
            indicator.FocusNeighborRight = index == _indicatorButtons.Count - 1
                ? nextPath
                : _indicatorButtons[index + 1].GetPath();
        }
    }

    private sealed record NewsCarouselItem(
        string TitleKey,
        string DetailKey,
        string BackgroundPath);
}
