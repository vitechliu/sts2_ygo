using Godot;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace VYgo.Scripts.UI;

/// <summary>
/// Owns the replacement background and the process-stable random monster artwork.
/// </summary>
internal sealed class MainMenuVisualController {
    private const string VisualName = "VYgoMainMenuVisual";
    private const string VisualScenePath = "res://VYgo/scenes/main_menu/main_menu_visual.tscn";
    private const float MonsterDesignHeight = 1080f;

    private static readonly Vector2 MonsterAnchor = new(0.6875f, 0.5f);
    private static readonly string[] MonsterPairScenePaths = [
        "res://VYgo/scenes/main_menu/monster_pairs/monster_pair_0001.tscn",
        "res://VYgo/scenes/main_menu/monster_pairs/monster_pair_0002.tscn",
        "res://VYgo/scenes/main_menu/monster_pairs/monster_pair_0011.tscn",
        "res://VYgo/scenes/main_menu/monster_pairs/monster_pair_0012.tscn"
    ];
    private static int? _sessionMonsterPairIndex;

    private readonly NMainMenu _mainMenu;
    private Control? _monsterHost;
    private Control? _activeMonsterPair;

    public MainMenuVisualController(NMainMenu mainMenu) {
        _mainMenu = mainMenu;
    }

    public void Install() {
        Control? originalBackground = _mainMenu.GetNodeOrNull<Control>("%MainMenuBg");
        if (originalBackground == null) {
            Entry.Logger.Warn("主菜单缺少 MainMenuBg，跳过背景替换。");
            return;
        }

        PackedScene? visualScene = ResourceLoader.Load<PackedScene>(VisualScenePath);
        Control? visual = visualScene?.InstantiateOrNull<Control>();
        if (visual == null) {
            Entry.Logger.Warn($"无法实例化主菜单视觉场景：{VisualScenePath}");
            return;
        }

        visual.Name = VisualName;
        _mainMenu.AddChild(visual);
        _mainMenu.MoveChild(visual, originalBackground.GetIndex() + 1);
        originalBackground.Visible = false;

        _monsterHost = visual.GetNodeOrNull<Control>("%MonsterHost");
        if (_monsterHost == null) {
            Entry.Logger.Warn("VYgo 主菜单视觉场景缺少 MonsterHost，仅显示静态背景。");
            return;
        }

        InstallMonsterPair();
        _monsterHost.Connect(Control.SignalName.Resized, Callable.From(UpdateMonsterPairLayout));
        Callable.From(UpdateMonsterPairLayout).CallDeferred();
    }

    private void InstallMonsterPair() {
        _sessionMonsterPairIndex ??= Random.Shared.Next(MonsterPairScenePaths.Length);
        string scenePath = MonsterPairScenePaths[_sessionMonsterPairIndex.Value];
        PackedScene? pairScene = ResourceLoader.Load<PackedScene>(scenePath);
        Control? pair = pairScene?.InstantiateOrNull<Control>();
        if (pair == null) {
            Entry.Logger.Warn($"无法实例化主菜单怪兽组合：{scenePath}；继续显示静态背景。");
            return;
        }

        pair.MouseFilter = Control.MouseFilterEnum.Ignore;
        _monsterHost!.AddChild(pair);
        _activeMonsterPair = pair;
        UpdateMonsterPairLayout();
        Entry.Logger.Info($"主菜单怪兽组合已加载：{scenePath}");
    }

    private void UpdateMonsterPairLayout() {
        if (_monsterHost == null
            || _activeMonsterPair == null
            || !GodotObject.IsInstanceValid(_monsterHost)
            || !GodotObject.IsInstanceValid(_activeMonsterPair)
            || _monsterHost.Size.Y <= 0f) {
            return;
        }

        _activeMonsterPair.Position = _monsterHost.Size * MonsterAnchor;
        _activeMonsterPair.Scale = Vector2.One * (_monsterHost.Size.Y / MonsterDesignHeight);
    }
}
