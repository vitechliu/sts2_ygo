using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Localization.Fonts;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace VYgo.Scripts.UI;

/// <summary>
/// Re-skins the original main-menu buttons without replacing their behavior.
/// </summary>
internal sealed class MainMenuLeftMenuController {
    internal const string CustomVisualName = "VYgoLeftMenuVisual";

    private const string MenuFontPath = "res://VYgo/ui/fonts/FOT-KafuTechnoStd-H.otf";
    private const float MenuWidth = 700f;
    private const float ItemHeight = 72f;
    private const float HighlightWidth = 240f;
    private const double FocusDuration = 0.18;
    private const double UnfocusDuration = 0.14;

    private static readonly MenuItemSpec[] ItemSpecs = [
        new("ContinueButton", "MENU 1", "菜单1说明"),
        new("AbandonRunButton", "MENU 2", "菜单2说明"),
        new("SingleplayerButton", "MENU 3", "菜单3说明"),
        new("MultiplayerButton", "MENU 4", "菜单4说明"),
        new("TimelineButton", "MENU 5", "菜单5说明", HasNotification: true),
        new("CompendiumButton", "MENU 6", "菜单6说明")
    ];

    private static readonly Color InactiveTextColor = new("f4f7f5");
    private static readonly Color FocusedTextColor = new("071000");
    private static readonly Color HighlightColor = new("b8ee00");
    private static readonly Color HighlightGlowColor = new(0.65f, 1f, 0f, 0.28f);
    private static readonly Color DisabledModulate = new(1f, 1f, 1f, 0.35f);

    private readonly NMainMenu _mainMenu;
    private readonly Dictionary<NMainMenuTextButton, MenuItemVisual> _items = new();

    private VBoxContainer _leftMenu = null!;
    private Font? _menuFont;
    private Font? _chineseFont;

    public NMainMenuTextButton SettingsButton { get; private set; } = null!;
    public NMainMenuTextButton QuitButton { get; private set; } = null!;

    public MainMenuLeftMenuController(NMainMenu mainMenu) {
        _mainMenu = mainMenu;
    }

    public void Install() {
        _leftMenu = _mainMenu.GetNodeOrNull<VBoxContainer>("%MainMenuTextButtons")
            ?? throw new InvalidOperationException("Main menu is missing MainMenuTextButtons.");
        SettingsButton = _leftMenu.GetNodeOrNull<NMainMenuTextButton>("SettingsButton")
            ?? throw new InvalidOperationException("Main menu is missing SettingsButton.");
        QuitButton = _leftMenu.GetNodeOrNull<NMainMenuTextButton>("QuitButton")
            ?? throw new InvalidOperationException("Main menu is missing QuitButton.");

        _menuFont = ResourceLoader.Load<Font>(MenuFontPath);
        if (_menuFont == null) {
            Entry.Logger.Warn($"Main-menu font is missing: {MenuFontPath}");
        }
        _chineseFont = FontManager.GetSubstituteFont("zhs", FontType.Regular);

        ApplyLayout();
        foreach (MenuItemSpec spec in ItemSpecs) {
            NMainMenuTextButton? button = _leftMenu.GetNodeOrNull<NMainMenuTextButton>(spec.NodeName);
            if (button == null) {
                Entry.Logger.Warn($"Main menu is missing {spec.NodeName}; skipping its custom visual.");
                continue;
            }

            ConfigureButton(button, spec);
        }

        RepositionContinueRunInfo();
        Update();
    }

    private void ApplyLayout() {
        _leftMenu.SetAnchorsPreset(Control.LayoutPreset.TopLeft, keepOffsets: false);
        _leftMenu.OffsetLeft = 96f;
        _leftMenu.OffsetTop = 210f;
        _leftMenu.OffsetRight = 96f + MenuWidth;
        _leftMenu.OffsetBottom = 210f + ItemHeight * ItemSpecs.Length;
        _leftMenu.CustomMinimumSize = new Vector2(MenuWidth, ItemHeight * ItemSpecs.Length);
        _leftMenu.Alignment = BoxContainer.AlignmentMode.Begin;
        _leftMenu.AddThemeConstantOverride("separation", 0);
    }

    private void ConfigureButton(NMainMenuTextButton button, MenuItemSpec spec) {
        button.CustomMinimumSize = new Vector2(MenuWidth, ItemHeight);
        button.SizeFlagsHorizontal = Control.SizeFlags.Fill;
        button.FocusMode = Control.FocusModeEnum.All;

        if (button.label != null) {
            button.label.Visible = false;
        }

        MenuItemVisual item = CreateVisual(button, spec);
        _items[button] = item;

        button.Connect(
            NClickableControl.SignalName.Focused,
            Callable.From<NClickableControl>(_ => SetFocused(button, focused: true)));
        button.Connect(
            NClickableControl.SignalName.Unfocused,
            Callable.From<NClickableControl>(_ => SetFocused(button, focused: false)));

        if (spec.HasNotification) {
            MoveTimelineNotification(item.NotificationHost);
        }

        if (button.HasFocus() && button.IsEnabled) {
            ApplyState(item, focused: true, immediate: true);
        }
    }

    private MenuItemVisual CreateVisual(NMainMenuTextButton button, MenuItemSpec spec) {
        var root = new Control {
            Name = CustomVisualName,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        button.AddChild(root);

        var glowClip = new Control {
            Name = "HighlightGlowClip",
            ClipContents = true,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Position = new Vector2(4f, 4f),
            Size = new Vector2(0f, ItemHeight - 8f)
        };
        root.AddChild(glowClip);

        var glow = new ColorRect {
            Name = "HighlightGlow",
            Color = HighlightGlowColor,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Position = Vector2.Zero,
            Size = new Vector2(HighlightWidth + 8f, ItemHeight - 8f)
        };
        glowClip.AddChild(glow);

        var highlightClip = new Control {
            Name = "HighlightClip",
            ClipContents = true,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Position = new Vector2(8f, 7f),
            Size = new Vector2(0f, ItemHeight - 14f)
        };
        root.AddChild(highlightClip);

        var highlight = new ColorRect {
            Name = "Highlight",
            Color = HighlightColor,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Position = Vector2.Zero,
            Size = new Vector2(HighlightWidth, ItemHeight - 14f)
        };
        highlightClip.AddChild(highlight);

        var accentLine = new ColorRect {
            Name = "AccentLine",
            Color = new Color("aaff00"),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Position = new Vector2(4f, 16f),
            Size = new Vector2(3f, 40f)
        };
        root.AddChild(accentLine);

        Label englishLabel = CreateLabel(spec.EnglishTitle, _menuFont, 34);
        englishLabel.Name = "EnglishTitle";
        englishLabel.Position = new Vector2(20f, 0f);
        englishLabel.Size = new Vector2(205f, ItemHeight);
        englishLabel.PivotOffset = new Vector2(0f, ItemHeight * 0.5f);
        englishLabel.AddThemeColorOverride("font_color", InactiveTextColor);
        englishLabel.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
        englishLabel.AddThemeConstantOverride("outline_size", 3);
        root.AddChild(englishLabel);

        Label arrowLabel = CreateLabel(">", _menuFont, 38);
        arrowLabel.Name = "Arrow";
        arrowLabel.Position = new Vector2(202f, 0f);
        arrowLabel.Size = new Vector2(30f, ItemHeight);
        arrowLabel.Modulate = Colors.Transparent;
        arrowLabel.AddThemeColorOverride("font_color", FocusedTextColor);
        root.AddChild(arrowLabel);

        Label chineseLabel = CreateLabel(spec.ChineseDescription, _chineseFont, 22);
        chineseLabel.Name = "ChineseDescription";
        chineseLabel.Position = new Vector2(252f, 0f);
        chineseLabel.Size = new Vector2(420f, ItemHeight);
        chineseLabel.Modulate = Colors.Transparent;
        chineseLabel.AddThemeColorOverride("font_color", InactiveTextColor);
        chineseLabel.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.92f));
        chineseLabel.AddThemeConstantOverride("outline_size", 3);
        root.AddChild(chineseLabel);

        var notificationHost = new Control {
            Name = "NotificationHost",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Position = new Vector2(-44f, 16f),
            Size = new Vector2(40f, 40f)
        };
        root.AddChild(notificationHost);

        return new MenuItemVisual(
            button,
            root,
            glowClip,
            highlightClip,
            englishLabel,
            arrowLabel,
            chineseLabel,
            notificationHost);
    }

    private static Label CreateLabel(string text, Font? font, int fontSize) {
        var label = new Label {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis
        };
        if (font != null) {
            label.AddThemeFontOverride("font", font);
        }
        label.AddThemeFontSizeOverride("font_size", fontSize);
        return label;
    }

    private void MoveTimelineNotification(Control notificationHost) {
        Control? notification = _mainMenu.GetNodeOrNull<Control>("%TimelineNotificationDot");
        if (notification == null) {
            Entry.Logger.Warn("Main menu is missing TimelineNotificationDot.");
            return;
        }

        notification.Reparent(notificationHost, keepGlobalTransform: false);
        notification.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        notification.MouseFilter = Control.MouseFilterEnum.Ignore;

        TextureRect? icon = notification.GetNodeOrNull<TextureRect>("Icon");
        if (icon != null) {
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            icon.MouseFilter = Control.MouseFilterEnum.Ignore;
        }
    }

    private void RepositionContinueRunInfo() {
        NContinueRunInfo continueInfo = _mainMenu.ContinueRunInfo;
        Callable.From(() => {
            if (!GodotObject.IsInstanceValid(continueInfo)) return;

            continueInfo.SetAnchorsPreset(Control.LayoutPreset.TopLeft, keepOffsets: false);
            continueInfo.Position = new Vector2(650f, -38f);
            continueInfo.Size = new Vector2(420f, 200f);

            FieldInfo? initPosition = typeof(NContinueRunInfo).GetField(
                "_initPosition",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            initPosition?.SetValue(continueInfo, continueInfo.Position);
        }).CallDeferred();
    }

    private void SetFocused(NMainMenuTextButton button, bool focused) {
        if (!_items.TryGetValue(button, out MenuItemVisual? item)) return;
        ApplyState(item, focused && button.IsEnabled, immediate: false);
    }

    private static void ApplyState(MenuItemVisual item, bool focused, bool immediate) {
        item.Tween?.Kill();
        item.Focused = focused;

        float targetWidth = focused ? HighlightWidth : 0f;
        float targetGlowWidth = focused ? HighlightWidth + 8f : 0f;
        Color targetTextColor = focused ? FocusedTextColor : InactiveTextColor;
        float detailAlpha = focused ? 1f : 0f;
        float detailX = focused ? 270f : 252f;

        if (immediate) {
            item.HighlightClip.Size = item.HighlightClip.Size with { X = targetWidth };
            item.GlowClip.Size = item.GlowClip.Size with { X = targetGlowWidth };
            item.EnglishLabel.AddThemeColorOverride("font_color", targetTextColor);
            item.ArrowLabel.Modulate = new Color(1f, 1f, 1f, detailAlpha);
            item.ChineseLabel.Modulate = new Color(1f, 1f, 1f, detailAlpha);
            item.ChineseLabel.Position = item.ChineseLabel.Position with { X = detailX };
            return;
        }

        double duration = focused ? FocusDuration : UnfocusDuration;
        Tween tween = item.Button.CreateTween().SetParallel();
        item.Tween = tween;
        tween.TweenProperty(item.HighlightClip, "size:x", targetWidth, duration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(item.GlowClip, "size:x", targetGlowWidth, duration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        tween.TweenMethod(
                Callable.From<Color>(color => item.EnglishLabel.AddThemeColorOverride("font_color", color)),
                item.EnglishLabel.GetThemeColor("font_color"),
                targetTextColor,
                Math.Min(duration, 0.1))
            .SetEase(Tween.EaseType.Out);

        PropertyTweener arrowTween = tween.TweenProperty(item.ArrowLabel, "modulate:a", detailAlpha, 0.08);
        PropertyTweener detailFadeTween = tween.TweenProperty(item.ChineseLabel, "modulate:a", detailAlpha, 0.1);
        PropertyTweener detailMoveTween = tween.TweenProperty(item.ChineseLabel, "position:x", detailX, duration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        if (focused) {
            arrowTween.SetDelay(0.08);
            detailFadeTween.SetDelay(0.08);
            detailMoveTween.SetDelay(0.04);
        }
    }

    public void Update() {
        foreach ((NMainMenuTextButton button, MenuItemVisual item) in _items) {
            bool enabled = button.IsEnabled;
            if (item.Enabled == enabled) continue;

            item.Enabled = enabled;
            item.Root.Modulate = enabled ? Colors.White : DisabledModulate;
            if (!enabled && item.Focused) {
                ApplyState(item, focused: false, immediate: false);
            }
        }
    }

    public NButton[] GetVisibleButtons() {
        return _leftMenu.GetChildren().OfType<NButton>()
            .Where(button => button.Visible && button.IsEnabled)
            .ToArray();
    }

    internal static bool IsCustomizedButton(NMainMenuTextButton button) {
        return button.GetNodeOrNull<Control>(CustomVisualName) != null;
    }

    private sealed record MenuItemSpec(
        string NodeName,
        string EnglishTitle,
        string ChineseDescription,
        bool HasNotification = false);

    private sealed class MenuItemVisual {
        public NMainMenuTextButton Button { get; }
        public Control Root { get; }
        public Control GlowClip { get; }
        public Control HighlightClip { get; }
        public Label EnglishLabel { get; }
        public Label ArrowLabel { get; }
        public Label ChineseLabel { get; }
        public Control NotificationHost { get; }
        public Tween? Tween { get; set; }
        public bool Focused { get; set; }
        public bool Enabled { get; set; }

        public MenuItemVisual(
            NMainMenuTextButton button,
            Control root,
            Control glowClip,
            Control highlightClip,
            Label englishLabel,
            Label arrowLabel,
            Label chineseLabel,
            Control notificationHost
        ) {
            Button = button;
            Root = root;
            GlowClip = glowClip;
            HighlightClip = highlightClip;
            EnglishLabel = englishLabel;
            ArrowLabel = arrowLabel;
            ChineseLabel = chineseLabel;
            NotificationHost = notificationHost;
            Enabled = !button.IsEnabled;
        }
    }
}
