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

    private const string IconRoot = "res://VYgo/images/ui/main_menu/icons";

    private static readonly Vector2 ToolbarItemSize = new(88f, 84f);
    private static readonly Vector2 ToolbarVisualSize = new(88f, 84f);

    private readonly NMainMenu _mainMenu;
    private readonly MainMenuLeftMenuController _leftMenuController;
    private readonly MainMenuSkinController.MenuLayout _menuLayout;
    private readonly Dictionary<NButton, Control> _toolbarVisuals = new();
    private readonly Dictionary<NButton, Label> _toolbarCaptions = new();

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
        ConfigureToolbarButton(_profileButton, "profile.png", "P");
        ConfigureToolbarButton(_patchNotesButton, "patch_notes.png", "N");
        ConfigureToolbarButton(_settingsButton, "settings.png", "S");
        ConfigureToolbarButton(_compendiumButton, "compendium.png", "C");
        RefreshCaptions();
        UpdateFocusNavigation(force: true);
    }

    private NMainMenuTextButton GetToolbarTextButton(string nodeName) {
        return _menuLayout.ToolbarItems
            .FirstOrDefault(item => item.Button.Name.ToString() == nodeName)?.Button
            ?? throw new InvalidOperationException($"Main-menu toolbar item {nodeName} was not classified.");
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
        Texture2D? icon = LoadOptionalTexture(customIconPath) ?? LoadFallbackIcon(button);
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

    public void RefreshCaptions() {
        if (_toolbarCaptions.Count == 0) return;

        var profileTitle = new LocString("main_menu_ui", "OPEN_PROFILE_SCREEN.title");
        profileTitle.Add("Id", SaveManager.Instance.CurrentProfileId);
        _toolbarCaptions[_profileButton].Text = profileTitle.GetFormattedText();
        _toolbarCaptions[_patchNotesButton].Text = LoadModCaption("PATCH_NOTES", "Patch Notes");
        _toolbarCaptions[_settingsButton].Text = new LocString("main_menu_ui", "SETTINGS").GetFormattedText();
        _toolbarCaptions[_compendiumButton].Text = new LocString("main_menu_ui", "COMPENDIUM").GetFormattedText();
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
        NButton[] toolbarButtons = [_profileButton, _patchNotesButton, _settingsButton, _compendiumButton];
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

    public void Update() {
        if (!GodotObject.IsInstanceValid(_mainMenu) || !GodotObject.IsInstanceValid(_toolbar)) return;

        bool shouldShowToolbar = !_mainMenu.SubmenuStack.SubmenusOpen && !_mainMenu.PatchNotesScreen.IsOpen;
        if (_toolbar.Visible != shouldShowToolbar) _toolbar.Visible = shouldShowToolbar;
        UpdateFocusNavigation();
    }

    private static Texture2D? LoadOptionalTexture(string path) {
        return ResourceLoader.Exists(path) ? ResourceLoader.Load<Texture2D>(path) : null;
    }
}
