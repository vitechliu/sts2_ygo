using VYgo.Core.News;
using MegaCrit.Sts2.Core.Platform;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace VYgo.Scripts.UI;

/// <summary>
/// 负责主菜单新闻展示、在线数据绑定、点击跳转及自动切换。
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
    private readonly NMainMenu _mainMenu;
    private readonly MainMenuLeftMenuController _leftMenuController;
    private IReadOnlyList<NewsItem> _items = [];
    private string _language = "";
    private Task<NewsDownload<NewsFeed>>? _feedRequest;
    private Task<NewsDownload<byte[]>>? _imageRequest;
    private string? _imageKey;
    private static readonly Dictionary<string, Texture2D?> ImageTextures = [];
    private Button _openButton = null!;
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
        _openButton.Pressed += OpenCurrentItem;
        _openButton.FocusEntered += () => SfxCmd.Play(HoverSfx);
        _openButton.MouseEntered += () => SfxCmd.Play(HoverSfx);
        RefreshTexts();

        if (_items.Count == 0) {
            root.Visible = false;
            Entry.Logger.Warn("主菜单新闻轮播没有可显示的数据。");
            return;
        }

        ShowItem(0, animate: false);
        UpdateNavigationVisibility();
        UpdateFocusNavigation(force: true);
        Entry.Logger.Info($"主菜单新闻轮播已加载，共 {_items.Count} 条新闻。");
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
        _openButton = root.GetNodeOrNull<Button>("%OpenButton")!;

        if (_backgroundA == null
            || _backgroundB == null
            || _fallbackBackground == null
            || _titleLabel == null
            || _detailLabel == null
            || _previousButton == null
            || _nextButton == null
            || _indicatorHost == null
            || _openButton == null) {
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
            _indicatorHost.RemoveChild(child);
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

        // 只有帧回调触碰 Godot 对象；后台任务从不持有主菜单节点。
        if (_language != NewsLocalData.Language) RefreshTexts();
        PollDownloads();

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

        NewsItem item = _items[index];
        _imageRequest = null;
        _imageKey = null;
        Texture2D? texture = null;
        if (item.Image.Type == "res") texture = LoadOptionalTexture(item.Image.Value);
        else if (NewsFeedCodec.TryImageUri(item.Image.Value, out Uri? uri)) {
            _imageKey = uri!.AbsoluteUri;
            if (!ImageTextures.TryGetValue(_imageKey, out texture))
                _imageRequest = NewsDownloadCache.Shared.GetImage(uri);
        }
        if (texture == null && _imageRequest == null) {
            Entry.Logger.Warn($"新闻轮播背景图缺失，使用颜色回退：{item.Image.Value}");
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
        if (_root == null) return;
        string language = NewsLocalData.Language;
        if (_language == language) return;
        _language = language;
        _feedRequest = NewsDownloadCache.Shared.GetFeed(language);
        ApplyItems(NewsLocalData.LoadFeed(language));
        PollDownloads();
    }

    private void ApplyItems(IReadOnlyList<NewsItem> items) {
        Control? focus = _root?.GetViewport().GuiGetFocusOwner();
        bool hadFocus = focus != null && _root!.IsAncestorOf(focus);
        string? previousId = _items.Count > 0 ? _items[_currentIndex].Id : null;
        StopBackgroundTransition();
        _imageRequest = null;
        _imageKey = null;
        _items = items;
        _currentIndex = 0;
        BuildIndicators();
        UpdateNavigationVisibility();
        _autoAdvanceElapsed = 0;
        if (items.Count > 0) {
            int retainedIndex = items.ToList().FindIndex(item => item.Id == previousId);
            ShowItem(Math.Max(0, retainedIndex), animate: false);
            if (hadFocus) {
                if (!_openButton.Disabled) _openButton.GrabFocus();
                else if (items.Count > 1) _previousButton.GrabFocus();
                else _leftMenuController.GetVisibleButtons().LastOrDefault()?.GrabFocus();
            }
        }
    }

    private void PollDownloads() {
        if (_feedRequest?.IsCompletedSuccessfully == true) {
            NewsDownload<NewsFeed> result = _feedRequest.Result;
            _feedRequest = null;
            if (result.Value is { Items.Count: > 0 } feed) ApplyItems(feed.Items);
            else if (result.Error != null) Entry.Logger.Warn($"在线新闻读取失败，保留本地内容：{result.Error}");
        }
        if (_imageRequest?.IsCompletedSuccessfully != true || _imageKey == null) return;
        NewsDownload<byte[]> image = _imageRequest.Result;
        _imageRequest = null;
        Texture2D? texture = null;
        try {
            if (image.Value != null) {
                using var decoded = new Image();
                byte[] bytes = image.Value;
                Error error = bytes.Length >= 12 && System.Text.Encoding.ASCII.GetString(bytes, 8, 4) == "WEBP"
                    ? decoded.LoadWebpFromBuffer(bytes)
                    : bytes.Length >= 8 && bytes[0] == 137 && bytes[1] == 80
                        ? decoded.LoadPngFromBuffer(bytes) : decoded.LoadJpgFromBuffer(bytes);
                if (error == Error.Ok && decoded.GetWidth() <= 4096 && decoded.GetHeight() <= 4096) {
                    if (decoded.GetWidth() > 1600 || decoded.GetHeight() > 1200) {
                        double ratio = Math.Min(1600.0 / decoded.GetWidth(), 1200.0 / decoded.GetHeight());
                        decoded.Resize(Math.Max(1, (int)(decoded.GetWidth() * ratio)), Math.Max(1, (int)(decoded.GetHeight() * ratio)));
                    }
                    texture = ImageTexture.CreateFromImage(decoded);
                }
            }
        }
        catch (Exception e) { Entry.Logger.Warn($"新闻图片解码失败：{e.Message}"); }
        if (!ImageTextures.ContainsKey(_imageKey) && ImageTextures.Count >= NewsDownloadCache.MaxImageEntries)
            ImageTextures.Remove(ImageTextures.Keys.First());
        ImageTextures[_imageKey] = texture;
        if (texture == null) Entry.Logger.Warn($"新闻图片加载失败，使用颜色回退：{image.Error ?? _imageKey}");
        StopBackgroundTransition();
        _activeBackground.Texture = texture;
        _activeBackground.Visible = texture != null;
        _fallbackBackground.Visible = texture == null;
    }

    private void ApplyCurrentText() {
        NewsItem item = _items[_currentIndex];
        _titleLabel.Text = item.Title;
        _detailLabel.Text = item.Content;
        _openButton.Disabled = item.Target.Type == "none" || (item.Target.Type == "scene" && !NewsSceneRouter.Contains(item.Target.Value));
        _openButton.MouseDefaultCursorShape = _openButton.Disabled ? Control.CursorShape.Arrow : Control.CursorShape.PointingHand;
        _openButton.TooltipText = _openButton.Disabled ? "" : item.Title;
    }

    private void OpenCurrentItem() {
        if (_items.Count == 0 || _mainMenu.SubmenuStack.SubmenusOpen || _mainMenu.PatchNotesScreen.IsOpen) return;
        NewsAsset target = _items[_currentIndex].Target;
        _autoAdvanceElapsed = 0;
        SfxCmd.Play(ClickSfx);
        try {
            if (target.Type == "url" && NewsFeedCodec.IsHttps(target.Value)) PlatformUtil.OpenUrl(target.Value);
            else if (target.Type == "scene") NewsSceneRouter.Open(_mainMenu, target.Value);
        }
        catch (Exception e) { Entry.Logger.Warn($"打开新闻目标失败：{e.Message}"); }
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

        if (!_root.Visible || _items.Count == 0 || menuButtons.Length == 0) return;
        if (_items.Count == 1) {
            if (!_openButton.Disabled) {
                _linkedMenuButton = menuButtons[^1];
                _linkedMenuButton.FocusNeighborBottom = _openButton.GetPath();
                _openButton.FocusNeighborTop = _linkedMenuButton.GetPath();
                _openButton.FocusNeighborBottom = _openButton.GetPath();
                _openButton.FocusNeighborLeft = _openButton.GetPath();
                _openButton.FocusNeighborRight = _openButton.GetPath();
            }
            return;
        }

        NButton menuButton = menuButtons[^1];
        _linkedMenuButton = menuButton;
        NodePath menuPath = menuButton.GetPath();
        NodePath previousPath = _previousButton.GetPath();
        NodePath nextPath = _nextButton.GetPath();
        NodePath selectedIndicatorPath = _indicatorButtons[_currentIndex].GetPath();

        NodePath openPath = _openButton.Disabled ? previousPath : _openButton.GetPath();
        menuButton.FocusNeighborBottom = openPath;
        _openButton.FocusNeighborTop = menuPath;
        _openButton.FocusNeighborBottom = selectedIndicatorPath;
        _openButton.FocusNeighborLeft = previousPath;
        _openButton.FocusNeighborRight = nextPath;

        _previousButton.FocusNeighborTop = openPath;
        _previousButton.FocusNeighborLeft = previousPath;
        _previousButton.FocusNeighborRight = nextPath;
        _previousButton.FocusNeighborBottom = selectedIndicatorPath;

        _nextButton.FocusNeighborTop = openPath;
        _nextButton.FocusNeighborLeft = previousPath;
        _nextButton.FocusNeighborRight = nextPath;
        _nextButton.FocusNeighborBottom = selectedIndicatorPath;

        for (int index = 0; index < _indicatorButtons.Count; index++) {
            Button indicator = _indicatorButtons[index];
            indicator.FocusNeighborTop = openPath;
            indicator.FocusNeighborLeft = index == 0
                ? previousPath
                : _indicatorButtons[index - 1].GetPath();
            indicator.FocusNeighborRight = index == _indicatorButtons.Count - 1
                ? nextPath
                : _indicatorButtons[index + 1].GetPath();
        }
    }

}
