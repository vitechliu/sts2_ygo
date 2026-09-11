using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace VYgo.Scripts.UI;

/// <summary>只允许打开随 Mod 打包的已注册子页面，不接受远程场景路径。</summary>
internal static class NewsSceneRouter {
    private static readonly IReadOnlyDictionary<string, string> Scenes = new Dictionary<string, string> {
        ["news_sample"] = "res://VYgo/scenes/main_menu/news_carousel/news_sample.tscn"
    };

    public static bool Contains(string id) => Scenes.ContainsKey(id);

    public static void Open(NMainMenu menu, string id) {
        if (!Scenes.TryGetValue(id, out string? path) || menu.SubmenuStack.SubmenusOpen) return;
        string name = $"VYgoNews_{id}";
        NSubmenu? screen = menu.SubmenuStack.GetNodeOrNull<NSubmenu>(name);
        if (screen == null) {
            PackedScene? scene = ResourceLoader.Load<PackedScene>(path);
            screen = scene?.Instantiate<NSubmenu>();
            if (screen == null) return;
            screen.Name = name;
            screen.Visible = false;
            menu.SubmenuStack.AddChild(screen);
        }
        menu.SubmenuStack.Push(screen);
    }
}
