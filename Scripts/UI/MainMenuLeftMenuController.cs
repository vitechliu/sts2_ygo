using Godot;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace VYgo.Scripts.UI;

/// <summary>
/// Owns the left-side main-menu list and exposes the buttons moved to the toolbar.
/// </summary>
internal sealed class MainMenuLeftMenuController {
    private readonly NMainMenu _mainMenu;

    private VBoxContainer _leftMenu = null!;

    public NMainMenuTextButton SettingsButton { get; private set; } = null!;
    public NMainMenuTextButton QuitButton { get; private set; } = null!;

    public MainMenuLeftMenuController(NMainMenu mainMenu) {
        _mainMenu = mainMenu;
    }

    public void Install() {
        _leftMenu = _mainMenu.GetNodeOrNull<VBoxContainer>("%MainMenuTextButtons")
            ?? throw new InvalidOperationException("主菜单缺少 MainMenuTextButtons。");
        SettingsButton = _leftMenu.GetNodeOrNull<NMainMenuTextButton>("SettingsButton")
            ?? throw new InvalidOperationException("主菜单缺少 SettingsButton。");
        QuitButton = _leftMenu.GetNodeOrNull<NMainMenuTextButton>("QuitButton")
            ?? throw new InvalidOperationException("主菜单缺少 QuitButton。");

        _leftMenu.SetAnchorsPreset(Control.LayoutPreset.CenterLeft, keepOffsets: false);
        _leftMenu.OffsetLeft = 96f;
        _leftMenu.OffsetTop = -225f;
        _leftMenu.OffsetRight = 416f;
        _leftMenu.OffsetBottom = 225f;
        _leftMenu.Alignment = BoxContainer.AlignmentMode.Center;
    }

    public NButton[] GetVisibleButtons() {
        return _leftMenu.GetChildren().OfType<NButton>()
            .Where(button => button.Visible && button.IsEnabled)
            .ToArray();
    }
}
