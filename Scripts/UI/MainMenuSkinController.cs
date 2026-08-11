using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace VYgo.Scripts.UI;

/// <summary>
/// 协调各个独立维护的主菜单皮肤模块。
/// </summary>
internal sealed partial class MainMenuSkinController : Node {
    public static readonly StringName ToolbarName = MainMenuToolbarController.ToolbarName;

    private const string ControllerName = "VYgoMainMenuSkinController";

    private NMainMenu _mainMenu = null!;
    private MainMenuVisualController? _visualController;
    private MainMenuUserInfoController? _userInfoController;
    private MainMenuLeftMenuController? _leftMenuController;
    private MainMenuNewsCarouselController? _newsCarouselController;
    private MainMenuToolbarController? _toolbarController;
    private MenuLayout? _menuLayout;

    private static readonly MenuRule[] MenuRules = [
        new("ContinueButton", MenuPlacement.Left, "CONTINUE", "继续之前的对局"),
        new("AbandonRunButton", MenuPlacement.Left, "ABANDON", "放弃当前对局"),
        new("SingleplayerButton", MenuPlacement.Left, "DUEL", "开始爬塔！"),
        new("MultiplayerButton", MenuPlacement.Left, "MULTIPLAYER", "发起联机模式对战(有风险)"),
        new("TimelineButton", MenuPlacement.Left, "TIMELINE", "时间轴", HasNotification: true),
        new("CompendiumButton", MenuPlacement.Toolbar),
        new("SettingsButton", MenuPlacement.Toolbar),
        new("QuitButton", MenuPlacement.Left, "QUIT", "结束游戏")
    ];

    public static void Install(NMainMenu mainMenu) {
        if (mainMenu.GetNodeOrNull<Node>(ControllerName) != null) return;

        var controller = new MainMenuSkinController {
            Name = ControllerName,
            _mainMenu = mainMenu
        };
        mainMenu.AddChild(controller);
        controller.InstallModules();
        Entry.Logger.Info("VYgo 主菜单布局已安装。");
    }

    private void InstallModules() {
        _menuLayout = DiscoverAndClassifyMenuItems();

        _visualController = new MainMenuVisualController(_mainMenu);
        _visualController.Install();

        _userInfoController = new MainMenuUserInfoController(_mainMenu);
        _userInfoController.Install();

        _leftMenuController = new MainMenuLeftMenuController(_mainMenu, _menuLayout);
        _leftMenuController.Install();

        _newsCarouselController = new MainMenuNewsCarouselController(_mainMenu, _leftMenuController);
        _newsCarouselController.Install();

        _toolbarController = new MainMenuToolbarController(_mainMenu, _leftMenuController, _menuLayout);
        _toolbarController.Install();
    }

    private MenuLayout DiscoverAndClassifyMenuItems() {
        VBoxContainer menuContainer = _mainMenu.GetNodeOrNull<VBoxContainer>("%MainMenuTextButtons")
            ?? throw new InvalidOperationException("Main menu is missing MainMenuTextButtons.");
        NMainMenuTextButton[] currentButtons = menuContainer.GetChildren()
            .OfType<NMainMenuTextButton>()
            .ToArray();
        Dictionary<string, NMainMenuTextButton> buttonsByName = currentButtons
            .GroupBy(button => button.Name.ToString(), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var leftItems = new List<MenuItemDescriptor>();
        var toolbarItems = new List<MenuItemDescriptor>();
        var recognizedButtons = new HashSet<NMainMenuTextButton>();
        foreach (MenuRule rule in MenuRules) {
            if (!buttonsByName.TryGetValue(rule.NodeName, out NMainMenuTextButton? button)) {
                Entry.Logger.Warn($"Main-menu item {rule.NodeName} was not found.");
                continue;
            }

            recognizedButtons.Add(button);
            var item = new MenuItemDescriptor(
                button,
                rule.EnglishText ?? GetCurrentButtonText(button),
                rule.HoverText ?? GetCurrentButtonText(button),
                TryGetButtonLocString(button),
                usesLocalizedText: false,
                rule.HasNotification);
            (rule.Placement == MenuPlacement.Left ? leftItems : toolbarItems).Add(item);
        }

        List<MenuItemDescriptor> unknownItems = currentButtons
            .Where(button => !recognizedButtons.Contains(button))
            .Select(button => new MenuItemDescriptor(
                button,
                GetCurrentButtonText(button),
                GetCurrentButtonText(button),
                TryGetButtonLocString(button),
                usesLocalizedText: true,
                hasNotification: false))
            .ToList();
        ResolveEnglishTexts(unknownItems);
        leftItems.AddRange(unknownItems);

        Entry.Logger.Info(
            $"Main-menu items classified: {leftItems.Count} left, {toolbarItems.Count} toolbar, " +
            $"{unknownItems.Count} third-party/unknown.");
        return new MenuLayout(menuContainer, leftItems, toolbarItems);
    }

    private static LocString? TryGetButtonLocString(NMainMenuTextButton button) {
        return typeof(NMainMenuTextButton)
            .GetField("_locString", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            ?.GetValue(button) as LocString;
    }

    private static string GetCurrentButtonText(NMainMenuTextButton button) {
        LocString? localization = TryGetButtonLocString(button);
        if (localization != null) {
            try {
                return localization.GetFormattedText();
            }
            catch (Exception exception) {
                Entry.Logger.Warn($"Failed to resolve localization for {button.Name}: {exception.Message}");
            }
        }

        string fallback = button.label?.Text ?? button.Name.ToString();
        return string.IsNullOrWhiteSpace(fallback) ? button.Name.ToString() : fallback;
    }

    private static void ResolveEnglishTexts(IReadOnlyList<MenuItemDescriptor> items) {
        if (items.Count == 0 || LocManager.Instance.Language == "eng") return;

        bool overriding = false;
        try {
            LocManager.Instance.StartOverridingLanguageAsEnglish();
            overriding = true;
            foreach (MenuItemDescriptor item in items) {
                if (item.Localization != null) {
                    item.EnglishText = item.Localization.GetFormattedText();
                }
            }
        }
        catch (Exception exception) {
            Entry.Logger.Warn($"Failed to resolve English text for third-party main-menu items: {exception.Message}");
        }
        finally {
            if (overriding) LocManager.Instance.StopOverridingLanguageAsEnglish();
        }
    }

    public override void _Process(double delta) {
        base._Process(delta);
        _leftMenuController?.Update();
        _newsCarouselController?.Update(delta);
        _toolbarController?.Update();
    }

    public override void _Notification(int what) {
        base._Notification(what);
        if (what == NotificationTranslationChanged && IsNodeReady()) {
            _menuLayout?.RefreshLocalizedHoverTexts();
            _leftMenuController?.RefreshTexts();
            _toolbarController?.RefreshCaptions();
            _newsCarouselController?.RefreshTexts();
        }
    }

    internal sealed record MenuLayout(
        VBoxContainer MenuContainer,
        IReadOnlyList<MenuItemDescriptor> LeftItems,
        IReadOnlyList<MenuItemDescriptor> ToolbarItems
    ) {
        public void RefreshLocalizedHoverTexts() {
            foreach (MenuItemDescriptor item in LeftItems.Where(item => item.UsesLocalizedText)) {
                item.RefreshLocalizedHoverText();
            }
        }
    }

    internal sealed class MenuItemDescriptor {
        public NMainMenuTextButton Button { get; }
        public string EnglishText { get; set; }
        public string HoverText { get; private set; }
        public LocString? Localization { get; }
        public bool UsesLocalizedText { get; }
        public bool HasNotification { get; }

        public MenuItemDescriptor(
            NMainMenuTextButton button,
            string englishText,
            string hoverText,
            LocString? localization,
            bool usesLocalizedText,
            bool hasNotification
        ) {
            Button = button;
            EnglishText = englishText;
            HoverText = hoverText;
            Localization = localization;
            UsesLocalizedText = usesLocalizedText;
            HasNotification = hasNotification;
        }

        public void RefreshLocalizedHoverText() {
            if (Localization == null) {
                HoverText = Button.label?.Text ?? HoverText;
                return;
            }

            try {
                HoverText = Localization.GetFormattedText();
            }
            catch (Exception exception) {
                Entry.Logger.Warn($"Failed to refresh localization for {Button.Name}: {exception.Message}");
            }
        }
    }

    private enum MenuPlacement {
        Left,
        Toolbar
    }

    private sealed record MenuRule(
        string NodeName,
        MenuPlacement Placement,
        string? EnglishText = null,
        string? HoverText = null,
        bool HasNotification = false);
}
