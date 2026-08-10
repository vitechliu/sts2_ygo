using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace VYgo.Scripts.UI;

/// <summary>
/// Coordinates the independently maintained main-menu skin modules.
/// </summary>
internal sealed partial class MainMenuSkinController : Node {
    public static readonly StringName ToolbarName = MainMenuToolbarController.ToolbarName;

    private const string ControllerName = "VYgoMainMenuSkinController";

    private NMainMenu _mainMenu = null!;
    private MainMenuVisualController? _visualController;
    private MainMenuLeftMenuController? _leftMenuController;
    private MainMenuToolbarController? _toolbarController;

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
        _visualController = new MainMenuVisualController(_mainMenu);
        _visualController.Install();

        _leftMenuController = new MainMenuLeftMenuController(_mainMenu);
        _leftMenuController.Install();

        _toolbarController = new MainMenuToolbarController(_mainMenu, _leftMenuController);
        _toolbarController.Install();
    }

    public override void _Process(double delta) {
        base._Process(delta);
        _leftMenuController?.Update();
        _toolbarController?.Update();
    }

    public override void _Notification(int what) {
        base._Notification(what);
        if (what == NotificationTranslationChanged && IsNodeReady()) {
            _toolbarController?.RefreshCaptions();
        }
    }
}
