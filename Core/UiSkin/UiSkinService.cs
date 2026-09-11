using System.Text.Json;
using Godot;
using VYgo.Core.Settings;
using VYgo.Scripts;

namespace VYgo.Core.UiSkin;

/// <summary>仅在本机启用；按实例更换视觉，不修改原版共享纹理或场景。</summary>
public static class UiSkinService {
    public const string ManifestPath = "res://VYgo/ui_skin/skin.json";
    private static readonly Dictionary<string, List<UiSkinRule>> RulesByScene = [];
    private static readonly Dictionary<ulong, List<UiSkinBinding>> Bindings = [];
    private static readonly HashSet<string> Warnings = [];
    private static bool _initialized;
    public static bool InspectorAllowed => OS.GetEnvironment("VYGO_UI_INSPECTOR") == "1";

    public static void Initialize() {
        if (_initialized || (!VYgoModSettings.ReplaceUiSkinOnStartup && !InspectorAllowed)) return;
        _initialized = true;
        try {
            if (VYgoModSettings.ReplaceUiSkinOnStartup && Godot.FileAccess.FileExists(ManifestPath)) {
                var manifest = JsonSerializer.Deserialize<UiSkinManifest>(Godot.FileAccess.GetFileAsString(ManifestPath), UiSkinManifest.JsonOptions);
                if (manifest?.SchemaVersion != 1) throw new InvalidDataException("不支持的皮肤清单版本");
                foreach (var rule in manifest.Rules) {
                    try {
                        Validate(rule);
                        foreach (string scene in rule.Selectors.Select(s => s.Scene).Distinct()) {
                            if (!RulesByScene.TryGetValue(scene, out var list)) RulesByScene[scene] = list = [];
                            list.Add(rule);
                        }
                    } catch (Exception e) { Warn(rule.Id, e.Message); }
                }
            }
            if (RulesByScene.Count == 0 && !InspectorAllowed) return;
            if (Engine.GetMainLoop() is not SceneTree tree) return;
            tree.NodeAdded += OnNodeAdded;
            // 初始化可能早于根节点就绪，统一延迟到安全的主线程节点阶段。
            Callable.From(() => {
                Visit(tree.Root);
                if (InspectorAllowed) tree.Root.AddChild(new UiSkinInspector());
            }).CallDeferred();
        } catch (Exception e) { Warn("manifest", $"皮肤未启用，使用原版界面：{e.Message}"); }
    }

    private static void Validate(UiSkinRule rule) {
        if (string.IsNullOrWhiteSpace(rule.Id) || rule.Selectors.Count == 0 || !rule.States.ContainsKey("normal"))
            throw new InvalidDataException("缺少规则标识、目标或正常状态");
        if (rule.Border.Length != 4 || rule.Border.Any(v => v < 0)) throw new InvalidDataException("九宫格边界无效");
        if (rule.Property == "color" && (rule.ExpectedType != "ColorRect" || rule.Texture.Color is not { Length: 4 })) throw new InvalidDataException("纯色背景标识无效");
        if (rule.Property != "color" && rule.Property != "texture" && !rule.Property.StartsWith("texture_") && !rule.Property.StartsWith("theme_override_styles/"))
            throw new InvalidDataException("不支持的视觉属性");
        if (!new[] { "contain", "cover", "stretch", "nine", "tile" }.Contains(rule.Mode)) throw new InvalidDataException("未知适配方式");
        foreach (var selector in rule.Selectors) {
            if (!selector.Scene.StartsWith("res://") || selector.Scene.Contains("..") || !IsRelativePath(selector.NodePath))
                throw new InvalidDataException("目标路径无效");
        }
        foreach (string resource in rule.States.Values)
            if (!resource.StartsWith("res://VYgo/ui_skin/generated/") || resource.Contains("..") || !ResourceLoader.Exists(resource))
                throw new InvalidDataException($"皮肤资源缺失：{resource}");
    }
    internal static bool IsRelativePath(string value) => !string.IsNullOrEmpty(value) && !value.StartsWith('/') && !value.Contains("..") && !value.Contains(':');
    internal static void Warn(string key, string message) {
        if (Warnings.Add(key)) Entry.Logger.Warn($"UI 皮肤 [{key}]：{message}");
    }
    private static void Visit(Node node) {
        foreach (Node child in node.GetChildren()) Visit(child);
        OnNodeAdded(node);
    }
    private static void OnNodeAdded(Node node) {
        if (node is not Control && node is not Sprite2D) return;
        Callable.From(() => {
            if (!GodotObject.IsInstanceValid(node) || !node.IsInsideTree()) return;
            if (node.IsNodeReady()) Apply(node);
            else node.Connect(Node.SignalName.Ready, Callable.From(() => Apply(node)), (uint)GodotObject.ConnectFlags.OneShot);
        }).CallDeferred();
    }
    public static IEnumerable<(Node Root, string Scene, string Path)> Contexts(Node node) {
        for (Node? root = node; root != null; root = root.GetParent())
            if (!string.IsNullOrEmpty(root.SceneFilePath)) yield return (root, root.SceneFilePath, root.GetPathTo(node).ToString());
    }
    private static void Apply(Node node) {
        if (!GodotObject.IsInstanceValid(node) || Bindings.ContainsKey(node.GetInstanceId())) return;
        var candidates = new List<(UiSkinRule Rule, Node Root)>();
        foreach (var context in Contexts(node)) {
            if (!RulesByScene.TryGetValue(context.Scene, out var rules)) continue;
            foreach (var rule in rules) if (rule.Selectors.Any(s => s.Scene == context.Scene && s.NodePath == context.Path)) candidates.Add((rule, context.Root));
        }
        // 运行时统计图标共用节点路径；只比较与此实例原图匹配的变体。
        foreach (var group in candidates.Where(c => !c.Rule.TextureVariant || GetTexture(node, c.Rule.Property) is { } t && c.Rule.Texture.Matches(t)).DistinctBy(c => c.Rule.Id).GroupBy(c => c.Rule.Property)) {
            var sorted = group.OrderByDescending(c => c.Rule.Priority).ToList();
            if (sorted.Count > 1 && sorted[0].Rule.Priority == sorted[1].Rule.Priority) { Warn(sorted[0].Rule.Id, "目标存在同优先级冲突，保留原版"); continue; }
            var (rule, root) = sorted[0];
            try {
                if (!node.IsClass(rule.ExpectedType)) throw new InvalidDataException($"节点类型变化：{node.GetClass()}");
                var texture = rule.Property == "color" ? null : GetTexture(node, rule.Property);
                bool matches = rule.Property == "color" ? node is ColorRect color && rule.Texture.MatchesColor(color) : texture != null && rule.Texture.Matches(texture);
                if (!matches) throw new InvalidDataException("当前视觉与扫描结果不符，保留原版");
                var binding = new UiSkinBinding(node, root, rule, texture);
                binding.Install();
                if (!Bindings.TryGetValue(node.GetInstanceId(), out var list)) {
                    Bindings[node.GetInstanceId()] = list = [];
                    ulong id = node.GetInstanceId();
                    node.TreeExiting += () => Bindings.Remove(id);
                }
                list.Add(binding);
            } catch (Exception e) { Warn(rule.Id, e.Message); }
        }
    }
    internal static Texture2D? GetTexture(Node node, string property) {
        if (property.StartsWith("theme_override_styles/") && node is Control control)
            return (control.GetThemeStylebox(property.Split('/')[1]) as StyleBoxTexture)?.Texture;
        if (!node.GetPropertyList().Any(p => p["name"].AsString() == property)) return null;
        return node.Get(property).AsGodotObject() as Texture2D;
    }
    public static void Refresh(Control owner) {
        // 只更新绑定到这个交互控件的图层；没有每帧遍历场景树。
        foreach (var list in Bindings.Values.ToArray()) foreach (var binding in list.ToArray())
            if (GodotObject.IsInstanceValid(binding.Node) && (binding.Owner == owner || binding.Node == owner)) binding.Refresh();
    }
    public static string[] MatchingRules(Node node) => Bindings.TryGetValue(node.GetInstanceId(), out var list) ? list.Select(b => b.Rule.Id).ToArray() : [];
    public static UiSkinTextureIdentity OriginalIdentity(Node node, string property, Texture2D current) =>
        Bindings.TryGetValue(node.GetInstanceId(), out var list) && list.FirstOrDefault(b => b.Rule.Property == property) is { } binding
            ? binding.Rule.Texture : UiSkinTextureIdentity.From(current);
}
