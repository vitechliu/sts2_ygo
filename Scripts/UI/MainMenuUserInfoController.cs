using Godot;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Platform;

namespace VYgo.Scripts.UI;

/// <summary>
/// 负责主菜单左上角用户信息控件的安装与临时数据绑定。
/// </summary>
internal sealed class MainMenuUserInfoController {
    private const string UserInfoName = "VYgoMainMenuUserInfo";
    private const string UserInfoScenePath = "res://VYgo/scenes/main_menu/user_info/main_menu_user_info.tscn";
    private const string FallbackNickname = "DUELIST";
    private const int PlaceholderLevel = 1;
    private const float PlaceholderExperienceProgress = 0.5f;

    private static readonly Vector2 UserInfoPosition = new(20f, 16f);
    private static readonly Vector2 UserInfoSize = new(548f, 112f);

    private readonly NMainMenu _mainMenu;

    public MainMenuUserInfoController(NMainMenu mainMenu) {
        _mainMenu = mainMenu;
    }

    public void Install() {
        if (_mainMenu.GetNodeOrNull<Control>(UserInfoName) != null) return;

        PackedScene? userInfoScene = ResourceLoader.Load<PackedScene>(UserInfoScenePath);
        Control? userInfo = userInfoScene?.InstantiateOrNull<Control>();
        if (userInfo == null) {
            Entry.Logger.Warn($"无法实例化主菜单用户信息场景：{UserInfoScenePath}");
            return;
        }

        userInfo.Name = UserInfoName;
        userInfo.MouseFilter = Control.MouseFilterEnum.Ignore;
        userInfo.SetAnchorsPreset(Control.LayoutPreset.TopLeft, keepOffsets: false);
        userInfo.Position = UserInfoPosition;
        userInfo.Size = UserInfoSize;
        _mainMenu.AddChild(userInfo);

        // 放在原版模糊层之前，使子菜单打开时保留控件并接受原版模糊效果。
        Control? blurBackstop = _mainMenu.GetNodeOrNull<Control>("%BlurBackstop");
        if (blurBackstop != null) {
            _mainMenu.MoveChild(userInfo, blurBackstop.GetIndex());
        }
        else {
            Entry.Logger.Warn("主菜单缺少 BlurBackstop，用户信息控件将保留在当前层级。");
        }

        BindPlaceholderData(userInfo);
    }

    private static void BindPlaceholderData(Control userInfo) {
        Label? nicknameLabel = userInfo.GetNodeOrNull<Label>("%NicknameLabel");
        Label? levelLabel = userInfo.GetNodeOrNull<Label>("%LevelLabel");
        Control? experienceFill = userInfo.GetNodeOrNull<Control>("%ExperienceFill");

        if (nicknameLabel == null || levelLabel == null || experienceFill == null) {
            Entry.Logger.Warn("主菜单用户信息场景缺少昵称、等级或经验节点。");
            return;
        }

        nicknameLabel.Text = ResolveNickname();
        levelLabel.Text = $"Lv. {PlaceholderLevel}";
        experienceFill.AnchorRight = Mathf.Clamp(PlaceholderExperienceProgress, 0f, 1f);
    }

    private static string ResolveNickname() {
        try {
            PlatformType platform = PlatformUtil.PrimaryPlatform;
            ulong playerId = PlatformUtil.GetLocalPlayerId(platform);
            string nickname = PlatformUtil.GetPlayerNameRaw(platform, playerId).Trim();
            return string.IsNullOrWhiteSpace(nickname) ? FallbackNickname : nickname;
        }
        catch (Exception exception) {
            Entry.Logger.Warn($"读取平台昵称失败，使用占位名称 {FallbackNickname}：{exception.Message}");
            return FallbackNickname;
        }
    }
}
