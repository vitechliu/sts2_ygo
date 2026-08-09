using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Saves;
using FileAccess = Godot.FileAccess;

namespace VYgo.Scripts.UI;

internal sealed partial class MainMenuSkinController : Node {
    public static readonly StringName ToolbarName = "VYgoMainMenuToolbar";

    private const string ControllerName = "VYgoMainMenuSkinController";
    private const string VisualName = "VYgoMainMenuVisual";
    private const string VisualScenePath = "res://VYgo/scenes/main_menu/main_menu_visual.tscn";
    private const string BackgroundPath = "res://VYgo/images/ui/main_menu/background.png";
    private const string DecorationBackPath = "res://VYgo/images/ui/main_menu/decoration_back.png";
    private const string DecorationFrontPath = "res://VYgo/images/ui/main_menu/decoration_front.png";
    private const string IconRoot = "res://VYgo/images/ui/main_menu/icons";

    private static readonly Vector2 ToolbarItemSize = new(88f, 84f);
    private static readonly Vector2 ToolbarVisualSize = new(88f, 84f);

    private NMainMenu _mainMenu = null!;
    private VBoxContainer _leftMenu = null!;
    private HBoxContainer _toolbar = null!;
    private NButton _profileButton = null!;
    private NButton _patchNotesButton = null!;
    private NMainMenuTextButton _settingsButton = null!;
    private NMainMenuTextButton _quitButton = null!;
    private readonly Dictionary<NButton, Control> _toolbarVisuals = new();
    private readonly Dictionary<NButton, Label> _toolbarCaptions = new();
    private int _navigationStateHash;

    public static void Install(NMainMenu mainMenu) {
        if (mainMenu.GetNodeOrNull<Node>(ControllerName) != null) return;

        var controller = new MainMenuSkinController {
            Name = ControllerName,
            _mainMenu = mainMenu
        };
        mainMenu.AddChild(controller);
        controller.InstallVisualLayer();
        controller.InstallMenuLayout();
        Entry.Logger.Info("VYgo 主菜单布局已安装。");
    }

    private void InstallVisualLayer() {
        Control? originalBackground = _mainMenu.GetNodeOrNull<Control>("%MainMenuBg");
        if (originalBackground == null) {
            Entry.Logger.Warn("主菜单缺少 MainMenuBg，跳过背景替换。");
            return;
        }

        Texture2D? backgroundTexture = LoadOptionalTexture(BackgroundPath, warnWhenMissing: true);
        if (backgroundTexture == null) {
            Entry.Logger.Warn("未找到 VYgo 主菜单背景，继续使用原版动态背景。");
            return;
        }

        PackedScene? visualScene = ResourceLoader.Load<PackedScene>(VisualScenePath);
        Control? visual = visualScene?.InstantiateOrNull<Control>();
        if (visual == null) {
            Entry.Logger.Warn($"无法实例化主菜单视觉场景：{VisualScenePath}");
            return;
        }

        visual.Name = VisualName;
        SetTexture(visual, "%Background", backgroundTexture);
        SetTexture(visual, "%DecorationBack", LoadOptionalTexture(DecorationBackPath));
        SetTexture(visual, "%DecorationFront", LoadOptionalTexture(DecorationFrontPath));

        _mainMenu.AddChild(visual);
        _mainMenu.MoveChild(visual, originalBackground.GetIndex() + 1);
        originalBackground.Visible = false;
    }

    private void InstallMenuLayout() {
        _leftMenu = _mainMenu.GetNodeOrNull<VBoxContainer>("%MainMenuTextButtons")
            ?? throw new InvalidOperationException("主菜单缺少 MainMenuTextButtons。");

        _profileButton = _mainMenu.GetNodeOrNull<NButton>("%ChangeProfileButton")
            ?? throw new InvalidOperationException("主菜单缺少 ChangeProfileButton。");
        _patchNotesButton = _mainMenu.GetNodeOrNull<NButton>("%PatchNotesButton")
            ?? throw new InvalidOperationException("主菜单缺少 PatchNotesButton。");
        _settingsButton = _leftMenu.GetNodeOrNull<NMainMenuTextButton>("SettingsButton")
            ?? throw new InvalidOperationException("主菜单缺少 SettingsButton。");
        _quitButton = _leftMenu.GetNodeOrNull<NMainMenuTextButton>("QuitButton")
            ?? throw new InvalidOperationException("主菜单缺少 QuitButton。");

        ApplyLeftMenuLayout();
        CreateToolbar();

        ConfigureToolbarButton(_profileButton, "profile.png", "P");
        ConfigureToolbarButton(_patchNotesButton, "patch_notes.png", "N");
        ConfigureToolbarButton(_settingsButton, "settings.png", "S");
        ConfigureToolbarButton(_quitButton, "quit.png", "X");
        RefreshCaptions();
        UpdateFocusNavigation(force: true);
    }

    private void ApplyLeftMenuLayout() {
        _leftMenu.SetAnchorsPreset(Control.LayoutPreset.CenterLeft, keepOffsets: false);
        _leftMenu.OffsetLeft = 96f;
        _leftMenu.OffsetTop = -225f;
        _leftMenu.OffsetRight = 416f;
        _leftMenu.OffsetBottom = 225f;
        _leftMenu.Alignment = BoxContainer.AlignmentMode.Center;
    }

    private void CreateToolbar() {
        // TODO: 后续确定版本号/发布日期与“更新记录”按钮的整合样式；本阶段为 ReleaseInfo 预留右侧空间。
        _toolbar = new HBoxContainer {
            Name = ToolbarName,
            CustomMinimumSize = new Vector2(388f, 84f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.End
        };
        _toolbar.AddThemeConstantOverride("separation", 12);
        _toolbar.SetAnchorsPreset(Control.LayoutPreset.TopRight, keepOffsets: false);
        _toolbar.OffsetLeft = -730f;
        _toolbar.OffsetTop = 16f;
        _toolbar.OffsetRight = -330f;
        _toolbar.OffsetBottom = 100f;

        _mainMenu.AddChild(_toolbar);
        Control? blurBackstop = _mainMenu.GetNodeOrNull<Control>("%BlurBackstop");
        if (blurBackstop != null) {
            _mainMenu.MoveChild(_toolbar, blurBackstop.GetIndex());
        }
    }

    private void ConfigureToolbarButton(NButton button, string iconFileName, string placeholderGlyph) {
        button.Reparent(_toolbar, keepGlobalTransform: false);
        button.CustomMinimumSize = ToolbarItemSize;
        button.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        button.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        button.FocusMode = Control.FocusModeEnum.All;
        button.PivotOffset = ToolbarItemSize * 0.5f;

        foreach (CanvasItem child in button.GetChildren().OfType<CanvasItem>()) {
            child.Visible = false;
        }

        Control visual = CreateToolbarVisual(button, iconFileName, placeholderGlyph);
        _toolbarVisuals[button] = visual;
        button.Connect(NClickableControl.SignalName.Focused,
            Callable.From<NClickableControl>(_ => AnimateToolbarFocus(button, focused: true)));
        button.Connect(NClickableControl.SignalName.Unfocused,
            Callable.From<NClickableControl>(_ => AnimateToolbarFocus(button, focused: false)));
    }

    private Control CreateToolbarVisual(NButton button, string iconFileName, string placeholderGlyph) {
        var visual = new VBoxContainer {
            Name = "VYgoToolbarVisual",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = ToolbarVisualSize,
            Alignment = BoxContainer.AlignmentMode.Center,
            PivotOffset = ToolbarVisualSize * 0.5f
        };
        visual.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        visual.AddThemeConstantOverride("separation", 0);
        button.AddChild(visual);

        var iconHolder = new Control {
            Name = "IconHolder",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(56f, 56f),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter
        };
        visual.AddChild(iconHolder);

        string customIconPath = $"{IconRoot}/{iconFileName}";
        Texture2D? icon = LoadOptionalTexture(customIconPath);
        if (icon == null) {
            icon = LoadFallbackIcon(button);
        }

        if (icon != null) {
            var iconRect = new TextureRect {
                Name = "Icon",
                Texture = icon,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            iconRect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            iconHolder.AddChild(iconRect);
        }
        else {
            Entry.Logger.Warn($"工具栏图标缺失，使用文字占位：{customIconPath}");
            var placeholder = CreateLabel(placeholderGlyph, 30);
            placeholder.Name = "PlaceholderIcon";
            placeholder.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            iconHolder.AddChild(placeholder);
        }

        Label caption = CreateLabel(string.Empty, 18);
        caption.Name = "Caption";
        caption.CustomMinimumSize = new Vector2(88f, 24f);
        visual.AddChild(caption);
        _toolbarCaptions[button] = caption;
        return visual;
    }

    private static Label CreateLabel(string text, int fontSize) {
        var label = new Label {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", new Color("fff1cc"));
        label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.8f));
        label.AddThemeConstantOverride("outline_size", 6);
        return label;
    }

    private Texture2D? LoadFallbackIcon(NButton button) {
        string? fallbackPath = button == _profileButton
            ? $"res://images/ui/profile/profile_icon_{SaveManager.Instance.CurrentProfileId}.png"
            : button == _patchNotesButton
                ? "res://images/ui/main_menu/patch_notes_icon.png"
                : null;
        return fallbackPath == null ? null : LoadOptionalTexture(fallbackPath);
    }

    private void AnimateToolbarFocus(NButton button, bool focused) {
        if (!_toolbarVisuals.TryGetValue(button, out Control? visual)) return;
        Tween tween = visual.CreateTween().SetParallel();
        tween.TweenProperty(visual, "scale", focused ? Vector2.One * 1.08f : Vector2.One, 0.12)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(visual, "modulate", focused ? new Color("ffd36a") : Colors.White, 0.12);
    }

    private void RefreshCaptions() {
        if (_toolbarCaptions.Count == 0) return;

        var profileTitle = new LocString("main_menu_ui", "OPEN_PROFILE_SCREEN.title");
        profileTitle.Add("Id", SaveManager.Instance.CurrentProfileId);
        _toolbarCaptions[_profileButton].Text = profileTitle.GetFormattedText();
        _toolbarCaptions[_patchNotesButton].Text = LoadModCaption("PATCH_NOTES", "Patch Notes");
        _toolbarCaptions[_settingsButton].Text = new LocString("main_menu_ui", "SETTINGS").GetFormattedText();
        _toolbarCaptions[_quitButton].Text = new LocString("main_menu_ui", "QUIT").GetFormattedText();
    }

    private static string LoadModCaption(string key, string fallback) {
        string locale = TranslationServer.GetLocale();
        string language = locale.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? "zhs" : "eng";
        string path = $"res://VYgo/localization/{language}/main_menu.json";
        try {
            string json = FileAccess.GetFileAsString(path);
            Dictionary<string, string>? values = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (values != null && values.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)) {
                return value;
            }
        }
        catch (Exception exception) {
            Entry.Logger.Warn($"读取主菜单本地化失败（{path}）：{exception.Message}");
        }
        return fallback;
    }

    private void UpdateFocusNavigation(bool force = false) {
        NButton[] leftButtons = _leftMenu.GetChildren().OfType<NButton>()
            .Where(button => button.Visible && button.IsEnabled)
            .ToArray();
        NButton[] toolbarButtons = [_profileButton, _patchNotesButton, _settingsButton, _quitButton];
        int stateHash = leftButtons.Aggregate(17, (hash, button) => HashCode.Combine(hash, button.GetInstanceId()));
        if (!force && stateHash == _navigationStateHash) return;
        _navigationStateHash = stateHash;

        for (int index = 0; index < toolbarButtons.Length; index++) {
            NButton button = toolbarButtons[index];
            button.FocusNeighborLeft = toolbarButtons[Math.Max(0, index - 1)].GetPath();
            button.FocusNeighborRight = toolbarButtons[Math.Min(toolbarButtons.Length - 1, index + 1)].GetPath();
            if (leftButtons.Length > 0) button.FocusNeighborBottom = leftButtons[0].GetPath();
        }

        if (leftButtons.Length > 0) {
            leftButtons[0].FocusNeighborTop = _profileButton.GetPath();
        }
    }

    public override void _Process(double delta) {
        base._Process(delta);
        if (!GodotObject.IsInstanceValid(_mainMenu) || !GodotObject.IsInstanceValid(_toolbar)) return;

        bool shouldShowToolbar = !_mainMenu.SubmenuStack.SubmenusOpen && !_mainMenu.PatchNotesScreen.IsOpen;
        if (_toolbar.Visible != shouldShowToolbar) _toolbar.Visible = shouldShowToolbar;
        UpdateFocusNavigation();
    }

    public override void _Notification(int what) {
        base._Notification(what);
        if (what == NotificationTranslationChanged && IsNodeReady()) {
            RefreshCaptions();
        }
    }

    private static Texture2D? LoadOptionalTexture(string path, bool warnWhenMissing = false) {
        if (!ResourceLoader.Exists(path)) {
            if (warnWhenMissing) Entry.Logger.Warn($"资源不存在：{path}");
            return null;
        }
        return ResourceLoader.Load<Texture2D>(path);
    }

    private static void SetTexture(Control root, string nodePath, Texture2D? texture) {
        TextureRect? textureRect = root.GetNodeOrNull<TextureRect>(nodePath);
        if (textureRect != null) textureRect.Texture = texture;
    }
}
