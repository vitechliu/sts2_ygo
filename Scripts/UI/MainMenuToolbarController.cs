using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Saves;
using FileAccess = Godot.FileAccess;

namespace VYgo.Scripts.UI;

/// <summary>
/// Owns the top toolbar, its presentation, localization, visibility and focus navigation.
/// </summary>
internal sealed class MainMenuToolbarController {
    public static readonly StringName ToolbarName = "VYgoMainMenuToolbar";

    private const string IconRoot = "res://VYgo/ui/images/top_menus/";
    private const float ToolbarSeparation = 12f;

    private static readonly Vector2 ToolbarItemSize = new(88f, 84f);
    private static readonly Vector2 ToolbarVisualSize = new Vector2(88f, 84f);
    private static readonly Color ToolbarHoverColor = new("72ff72");
    private static readonly Color ToolbarShadowColor = new(0.25f, 1f, 0.25f, 0.48f);
    private static readonly Color TransparentWhite = new(1f, 1f, 1f, 0f);

    private readonly NMainMenu _mainMenu;
    private readonly MainMenuLeftMenuController _leftMenuController;
    private readonly MainMenuSkinController.MenuLayout _menuLayout;
    private readonly List<NButton> _toolbarButtons = new();
    private readonly Dictionary<NButton, ToolbarButtonVisual> _toolbarVisuals = new();
    private readonly Dictionary<NButton, Label> _toolbarCaptions = new();
    private readonly Dictionary<NButton, Tween> _toolbarTweens = new();

    private HBoxContainer _toolbar = null!;
    private NButton _profileButton = null!;
    private NButton _patchNotesButton = null!;
    private NMainMenuTextButton _settingsButton = null!;
    private NMainMenuTextButton _compendiumButton = null!;
    private int _navigationStateHash;

    public MainMenuToolbarController(
        NMainMenu mainMenu,
        MainMenuLeftMenuController leftMenuController,
        MainMenuSkinController.MenuLayout menuLayout
    ) {
        _mainMenu = mainMenu;
        _leftMenuController = leftMenuController;
        _menuLayout = menuLayout;
    }

    public void Install() {
        _profileButton = _mainMenu.GetNodeOrNull<NButton>("%ChangeProfileButton")
            ?? throw new InvalidOperationException("主菜单缺少 ChangeProfileButton。");
        _patchNotesButton = _mainMenu.GetNodeOrNull<NButton>("%PatchNotesButton")
            ?? throw new InvalidOperationException("主菜单缺少 PatchNotesButton。");
        _settingsButton = GetToolbarTextButton("SettingsButton");
        _compendiumButton = GetToolbarTextButton("CompendiumButton");

        CreateToolbar();
        MoveReleaseInfo();
        var profile = IconRoot + $"profile_icon_{SaveManager.Instance.CurrentProfileId}.png";
        ConfigureToolbarButton(_profileButton, profile, "P");
        ConfigureToolbarButton(_patchNotesButton, IconRoot + "patch_notes.png", "N");
        ConfigureToolbarButton(_compendiumButton, IconRoot + "wiki.png", "C");
        ConfigureToolbarButton(_settingsButton, IconRoot + "settings.png", "S");
        RefreshCaptions();
        RefreshToolbarSize();
        UpdateFocusNavigation(force: true);
    }

    void MoveReleaseInfo() {
        var releaseInfoLabel = _mainMenu.GetNodeOrNull<Label>("%ReleaseInfo");
        if (releaseInfoLabel == null) return;
        
    }

    /// <summary>
    /// 创建一个原主菜单中不存在的工具栏按钮，并套用与现有按钮相同的图标、标题、hover 和焦点导航样式。
    /// </summary>
    public NButton AddToolbarButton(
        string nodeName,
        string iconPath,
        string text,
        Action<NButton>? onReleased = null,
        string placeholderGlyph = "?"
    ) {
        if (!GodotObject.IsInstanceValid(_toolbar)) {
            throw new InvalidOperationException("工具栏尚未安装，无法新增按钮。");
        }
        if (string.IsNullOrWhiteSpace(nodeName)) {
            throw new ArgumentException("按钮节点名不能为空。", nameof(nodeName));
        }
        if (_toolbarButtons.Any(button => button.Name.ToString() == nodeName)) {
            throw new InvalidOperationException($"工具栏按钮 {nodeName} 已存在。");
        }

        // NButton 会在 AddChild 时执行 _Ready；提前设定 FocusMode，确保其内部也记录为可手柄导航。
        var button = new NButton {
            Name = nodeName,
            FocusMode = Control.FocusModeEnum.All
        };
        _toolbar.AddChild(button);
        ConfigureToolbarButton(button, iconPath, placeholderGlyph);
        SetToolbarCaption(button, text);

        if (onReleased != null) {
            button.Connect(NClickableControl.SignalName.Released,
                Callable.From<NClickableControl>(_ => onReleased(button)));
        }

        RefreshToolbarSize();
        UpdateFocusNavigation(force: true);
        return button;
    }

    private NMainMenuTextButton GetToolbarTextButton(string nodeName) {
        return _menuLayout.ToolbarItems
            .FirstOrDefault(item => item.Button.Name.ToString() == nodeName)?.Button
            ?? throw new InvalidOperationException($"Main-menu toolbar item {nodeName} was not classified.");
    }

    private void CreateToolbar() {
        _toolbar = new HBoxContainer {
            Name = ToolbarName,
            CustomMinimumSize = new Vector2(388f, 84f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.End
        };
        _toolbar.AddThemeConstantOverride("separation", (int)ToolbarSeparation);
        _toolbar.SetAnchorsPreset(Control.LayoutPreset.TopRight, keepOffsets: false);
        _toolbar.OffsetLeft = -730f;
        _toolbar.OffsetTop = 16f;
        _toolbar.OffsetRight = -70f;
        _toolbar.OffsetBottom = 100f;

        _mainMenu.AddChild(_toolbar);
        Control? blurBackstop = _mainMenu.GetNodeOrNull<Control>("%BlurBackstop");
        if (blurBackstop != null) {
            _mainMenu.MoveChild(_toolbar, blurBackstop.GetIndex());
        }
    }

    private void ConfigureToolbarButton(NButton button, string customIconPath, string placeholderGlyph) {
        if (button.GetParent() != _toolbar) {
            button.Reparent(_toolbar, keepGlobalTransform: false);
        }
        button.CustomMinimumSize = ToolbarItemSize;
        button.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        button.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        button.FocusMode = Control.FocusModeEnum.All;
        button.PivotOffset = ToolbarItemSize * 0.5f;

        foreach (CanvasItem child in button.GetChildren().OfType<CanvasItem>()) {
            child.Visible = false;
        }

        ToolbarButtonVisual visual = CreateToolbarVisual(button, customIconPath, placeholderGlyph);
        _toolbarButtons.Add(button);
        _toolbarVisuals[button] = visual;
        button.Connect(NClickableControl.SignalName.Focused,
            Callable.From<NClickableControl>(_ => AnimateToolbarFocus(button, focused: true)));
        button.Connect(NClickableControl.SignalName.Unfocused,
            Callable.From<NClickableControl>(_ => AnimateToolbarFocus(button, focused: false)));
    }

    private ToolbarButtonVisual CreateToolbarVisual(
        NButton button,
        string customIconPath,
        string placeholderGlyph
    ) {
        var visual = new Control {
            Name = "VYgoToolbarVisual",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = ToolbarVisualSize,
            PivotOffset = ToolbarVisualSize * 0.5f
        };
        visual.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        button.AddChild(visual);

        var content = new VBoxContainer {
            Name = "Content",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        content.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        content.AddThemeConstantOverride("separation", 0);
        visual.AddChild(content);

        var iconHolder = new Control {
            Name = "IconHolder",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(56f, 56f) * 1.3f,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter
        };
        content.AddChild(iconHolder);

        var iconShadows = new List<CanvasItem>();
        var textShadowTargets = new List<Label>();
        Texture2D? icon = LoadOptionalTexture(customIconPath) ?? null;
        if (icon != null) {
            var iconShadow = CreateIconRect(icon, "IconShadow");
            iconShadow.SelfModulate = TransparentWhite;
            iconShadow.OffsetLeft = 2f;
            iconShadow.OffsetTop = 3f;
            iconShadow.OffsetRight = 2f;
            iconShadow.OffsetBottom = 3f;
            iconHolder.AddChild(iconShadow);
            iconShadows.Add(iconShadow);

            var iconRect = CreateIconRect(icon, "Icon");
            iconHolder.AddChild(iconRect);
        }
        else {
            Entry.Logger.Warn($"工具栏图标缺失，使用文字占位：{customIconPath}");
            var placeholder = CreateLabel(placeholderGlyph, 30);
            placeholder.Name = "PlaceholderIcon";
            placeholder.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            ConfigureTextShadow(placeholder);
            iconHolder.AddChild(placeholder);
            textShadowTargets.Add(placeholder);
        }

        Label caption = CreateLabel(string.Empty, 18);
        caption.Name = "Caption";
        caption.CustomMinimumSize = new Vector2(88f, 24f);
        ConfigureTextShadow(caption);
        content.AddChild(caption);
        textShadowTargets.Add(caption);
        _toolbarCaptions[button] = caption;
        return new ToolbarButtonVisual(visual, iconShadows, textShadowTargets);
    }

    private static TextureRect CreateIconRect(Texture2D icon, string name) {
        var iconRect = new TextureRect {
            Name = name,
            Texture = icon,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        iconRect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        return iconRect;
    }

    private static void ConfigureTextShadow(Label label) {
        label.AddThemeColorOverride("font_shadow_color", TransparentWhite);
        label.AddThemeConstantOverride("shadow_offset_x", 1);
        label.AddThemeConstantOverride("shadow_offset_y", 2);
        label.AddThemeConstantOverride("shadow_outline_size", 2);
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
        if (!_toolbarVisuals.TryGetValue(button, out ToolbarButtonVisual? visual)) return;
        if (_toolbarTweens.Remove(button, out Tween? previousTween) &&
            GodotObject.IsInstanceValid(previousTween)) {
            previousTween.Kill();
        }

        Tween tween = visual.Root.CreateTween().SetParallel();
        _toolbarTweens[button] = tween;
        tween.TweenProperty(visual.Root, "scale", focused ? Vector2.One * 1.08f : Vector2.One, 0.12)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(visual.Root, "modulate", focused ? ToolbarHoverColor : Colors.White, 0.12);
        foreach (CanvasItem shadow in visual.IconShadows) {
            tween.TweenProperty(shadow, "self_modulate", focused ? ToolbarShadowColor : TransparentWhite, 0.12)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
        }
        foreach (Label label in visual.TextShadowTargets) {
            Color currentColor = label.GetThemeColor("font_shadow_color");
            Color targetColor = focused ? ToolbarShadowColor : TransparentWhite;
            tween.TweenMethod(
                    Callable.From<Color>(color => label.AddThemeColorOverride("font_shadow_color", color)),
                    currentColor,
                    targetColor,
                    0.12)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
        }
    }

    public void RefreshCaptions() {
        if (_toolbarCaptions.Count == 0) return;

        var profileTitle = new LocString("main_menu_ui", "OPEN_PROFILE_SCREEN.title");
        profileTitle.Add("Id", SaveManager.Instance.CurrentProfileId);
        SetToolbarCaption(_profileButton, profileTitle.GetFormattedText());
        SetToolbarCaption(_patchNotesButton, LoadModCaption("PATCH_NOTES", "Patch Notes"));
        SetToolbarCaption(_settingsButton, new LocString("main_menu_ui", "SETTINGS").GetFormattedText());
        SetToolbarCaption(_compendiumButton, new LocString("main_menu_ui", "COMPENDIUM").GetFormattedText());
    }

    private void SetToolbarCaption(NButton button, string text) {
        _toolbarCaptions[button].Text = text;
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
        NButton[] leftButtons = _leftMenuController.GetVisibleButtons();
        NButton[] toolbarButtons = _toolbarButtons
            .Where(button => GodotObject.IsInstanceValid(button) && button.Visible)
            .ToArray();
        int stateHash = leftButtons.Aggregate(17, (hash, button) => HashCode.Combine(hash, button.GetInstanceId()));
        stateHash = toolbarButtons.Aggregate(stateHash,
            (hash, button) => HashCode.Combine(hash, button.GetInstanceId()));
        if (!force && stateHash == _navigationStateHash) return;
        _navigationStateHash = stateHash;

        for (int index = 0; index < toolbarButtons.Length; index++) {
            NButton button = toolbarButtons[index];
            button.FocusNeighborLeft = toolbarButtons[Math.Max(0, index - 1)].GetPath();
            button.FocusNeighborRight = toolbarButtons[Math.Min(toolbarButtons.Length - 1, index + 1)].GetPath();
            if (leftButtons.Length > 0) button.FocusNeighborBottom = leftButtons[0].GetPath();
        }

        if (leftButtons.Length > 0 && toolbarButtons.Length > 0) {
            leftButtons[0].FocusNeighborTop = toolbarButtons[0].GetPath();
        }
    }

    private void RefreshToolbarSize() {
        int itemCount = _toolbarButtons.Count(button => GodotObject.IsInstanceValid(button));
        float width = itemCount * ToolbarItemSize.X + Math.Max(0, itemCount - 1) * ToolbarSeparation;
        _toolbar.CustomMinimumSize = new Vector2(width, ToolbarItemSize.Y);
        _toolbar.OffsetLeft = _toolbar.OffsetRight - width;
    }

    public void Update() {
        if (!GodotObject.IsInstanceValid(_mainMenu) || !GodotObject.IsInstanceValid(_toolbar)) return;

        bool shouldShowToolbar = !_mainMenu.SubmenuStack.SubmenusOpen && !_mainMenu.PatchNotesScreen.IsOpen;
        if (_toolbar.Visible != shouldShowToolbar) _toolbar.Visible = shouldShowToolbar;
        UpdateFocusNavigation();
    }

    private static Texture2D? LoadOptionalTexture(string path) {
        return ResourceLoader.Exists(path) ? ResourceLoader.Load<Texture2D>(path) : null;
    }

    private sealed record ToolbarButtonVisual(
        Control Root,
        IReadOnlyList<CanvasItem> IconShadows,
        IReadOnlyList<Label> TextShadowTargets
    );
}
