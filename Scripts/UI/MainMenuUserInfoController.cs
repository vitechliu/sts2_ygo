using Godot;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Platform;
using VYgo.Core.Progression;
using VYgo.Core.Saves;

namespace VYgo.Scripts.UI;

/// <summary>
/// 负责主菜单左上角用户信息控件的安装与决斗者档案绑定。
/// </summary>
internal sealed class MainMenuUserInfoController {
    private const string UserInfoName = "VYgoMainMenuUserInfo";
    private const string UserInfoScenePath = "res://VYgo/scenes/main_menu/user_info/main_menu_user_info.tscn";
    private const string FallbackNickname = "DUELIST";
    private const double ProfileRefreshIntervalSeconds = 0.25;

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

        BindData(userInfo, DuelistProgressSnapshot.Default);
        if (!TryRefreshProfileData(userInfo)) {
            StartProfileRefreshTimer(userInfo);
        }
    }

    private void StartProfileRefreshTimer(Control userInfo) {
        var refreshTimer = new Godot.Timer {
            Name = "ProfileRefreshTimer",
            WaitTime = ProfileRefreshIntervalSeconds,
            OneShot = false,
            Autostart = true,
        };
        refreshTimer.Timeout += () => {
            if (!GodotObject.IsInstanceValid(userInfo) || !userInfo.IsInsideTree()) {
                refreshTimer.Stop();
                return;
            }

            if (TryRefreshProfileData(userInfo)) {
                refreshTimer.Stop();
                refreshTimer.QueueFree();
            }
        };
        userInfo.AddChild(refreshTimer);
    }

    private bool TryRefreshProfileData(Control userInfo) {
        if (!YgoSave.Instance.TryConsumeDuelistPresentation(
                out DuelistProgressSnapshot progress,
                out DuelistPresentationSnapshot presentation)) {
            return false;
        }

        BindData(userInfo, progress);
        if (presentation.LevelUp != null || presentation.RankUp != null) {
            ShowPresentation(presentation);
        }
        return true;
    }

    private static void BindData(Control userInfo, DuelistProgressSnapshot progress) {
        Label? nicknameLabel = userInfo.GetNodeOrNull<Label>("%NicknameLabel");
        Label? levelLabel = userInfo.GetNodeOrNull<Label>("%LevelLabel");
        Control? experienceFill = userInfo.GetNodeOrNull<Control>("%ExperienceFill");
        Label? experienceLabel = userInfo.GetNodeOrNull<Label>("%ExperienceLabel");
        TextureRect? rankIcon = userInfo.GetNodeOrNull<TextureRect>("%RankIcon");
        Label? rankMinorLabel = userInfo.GetNodeOrNull<Label>("%RankMinorLabel");

        if (nicknameLabel == null
            || levelLabel == null
            || experienceFill == null
            || experienceLabel == null
            || rankIcon == null
            || rankMinorLabel == null) {
            Entry.Logger.Warn("主菜单用户信息场景缺少昵称、等级、经验或段位节点。");
            return;
        }

        nicknameLabel.Text = ResolveNickname();
        levelLabel.Text = $"Lv. {progress.Level}";
        double ratio = progress.ExperienceRequired <= 0L
            ? 0d
            : (double)progress.Experience / progress.ExperienceRequired;
        experienceFill.AnchorRight = Mathf.Clamp((float)ratio, 0f, 1f);
        experienceFill.OffsetRight = 0f;
        experienceLabel.Text = $"{progress.Experience:N0} / {progress.ExperienceRequired:N0}";

        string rankIconPath =
            $"res://VYgo/ui/images/ranks/img_rankicon_{progress.MajorRankIndex:00}_l.png";
        Texture2D? texture = ResourceLoader.Load<Texture2D>(rankIconPath);
        if (texture != null) {
            rankIcon.Texture = texture;
        }
        else {
            Entry.Logger.Warn($"无法加载决斗者段位图标：{rankIconPath}");
        }
        rankMinorLabel.Text = DuelistProgression.FormatMinorRankRoman(progress.MinorRank);
    }

    private void ShowPresentation(DuelistPresentationSnapshot presentation) {
        var lines = new List<string> { "决斗者档案更新" };
        if (presentation.LevelUp is { } levelUp) {
            lines.Add($"Lv.{levelUp.FromLevel} → Lv.{levelUp.ToLevel}");
        }
        if (presentation.RankUp is { } rankUp) {
            string fromRank = DuelistProgression.FormatRank(
                rankUp.FromMajorRankIndex,
                rankUp.FromMinorRank);
            string toRank = DuelistProgression.FormatRank(
                rankUp.ToMajorRankIndex,
                rankUp.ToMinorRank);
            lines.Add($"{fromRank} → {toRank}");
        }

        var toast = new Label {
            Name = "VYgoDuelistProgressToast",
            Text = string.Join('\n', lines),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AnchorLeft = 0.5f,
            AnchorRight = 0.5f,
            OffsetLeft = -300f,
            OffsetTop = 136f,
            OffsetRight = 300f,
            OffsetBottom = 214f,
            Modulate = new Color(1f, 1f, 1f, 0f),
        };
        toast.AddThemeFontSizeOverride("font_size", 24);
        toast.AddThemeColorOverride("font_color", new Color("f4fbff"));
        toast.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.95f));
        toast.AddThemeConstantOverride("outline_size", 6);
        toast.AddThemeStyleboxOverride("normal", new StyleBoxFlat {
            BgColor = new Color(0.025f, 0.055f, 0.075f, 0.94f),
            BorderColor = new Color(0.45f, 0.85f, 0.92f, 0.95f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
        });
        _mainMenu.AddChild(toast);

        Tween tween = toast.CreateTween();
        tween.TweenProperty(toast, "modulate:a", 1f, 0.2)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        tween.TweenInterval(2.2);
        tween.TweenProperty(toast, "modulate:a", 0f, 0.35)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Cubic);
        tween.TweenCallback(Callable.From(toast.QueueFree));
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
