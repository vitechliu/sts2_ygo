using System.Text.Json;
using Godot;

namespace VYgo.Core.UiSkin;

/// <summary>开发者通过环境变量显式启用；F8 拾取，F9 导出悬停节点证据。</summary>
public partial class UiSkinInspector : CanvasLayer {
    private Control _overlay = null!;
    private Panel _highlight = null!;
    private Label _label = null!;
    private Control? _target;
    private bool _active;
    public override void _Ready() {
        Name = "VYgoUiSkinInspector";
        Layer = 120;
        ProcessMode = ProcessModeEnum.Always;
        _overlay = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        _overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_overlay);
        _highlight = new Panel { MouseFilter = Control.MouseFilterEnum.Ignore };
        _highlight.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
            BgColor = new Color(0, 0.9f, 0.9f, 0.08f), BorderColor = Colors.Cyan,
            BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2
        });
        _overlay.AddChild(_highlight);
        _label = new Label { Position = new Vector2(20, 20), MouseFilter = Control.MouseFilterEnum.Ignore };
        _label.AddThemeFontSizeOverride("font_size", 18);
        _label.AddThemeColorOverride("font_color", Colors.White);
        _label.AddThemeColorOverride("font_outline_color", Colors.Black);
        _label.AddThemeConstantOverride("outline_size", 6);
        _overlay.AddChild(_label);
        _overlay.Hide();
        SetProcess(false);
    }
    public override void _Input(InputEvent input) {
        if (input is InputEventKey { Pressed: true, Echo: false } key) {
            if (key.Keycode == Key.F8) {
                _active = !_active; _overlay.Visible = _active; SetProcess(_active);
                GetViewport().SetInputAsHandled();
            } else if (_active && key.Keycode == Key.F9) {
                Export(); GetViewport().SetInputAsHandled();
            }
        }
        // 拾取时屏蔽点击和滚轮，避免检查按钮时触发业务动作。
        if (_active && input is InputEventMouseButton) GetViewport().SetInputAsHandled();
    }
    public override void _Process(double delta) {
        _target = null;
        Find(GetTree().Root, GetViewport().GetMousePosition());
        _highlight.Visible = _target != null;
        if (_target == null) { _label.Text = "UI 拾取 · F8 退出 · F9 导出\n悬停到纹理组件上"; return; }
        _highlight.Position = _target.GetGlobalRect().Position;
        _highlight.Size = _target.GetGlobalRect().Size;
        var contexts = UiSkinService.Contexts(_target).ToArray();
        _label.Text = $"UI 拾取 · F8 退出 · F9 导出\n{_target.GetClass()}  {_target.Size}\n{contexts.FirstOrDefault().Scene}\n{contexts.FirstOrDefault().Path}\n规则：{string.Join(", ", UiSkinService.MatchingRules(_target))}";
    }
    private void Find(Node node, Vector2 mouse) {
        if (node == this) return;
        if (node is CanvasItem item && !item.IsVisibleInTree()) return;
        if (node is Control control && control.ClipContents && !control.GetGlobalRect().HasPoint(mouse)) return;
        if (node is TextureRect or NinePatchRect or TextureButton && node is Control candidate && candidate.GetGlobalRect().HasPoint(mouse)) _target = candidate;
        foreach (Node child in node.GetChildren()) Find(child, mouse);
    }
    private void Export() {
        if (_target == null || !GodotObject.IsInstanceValid(_target)) return;
        try {
            string folder = ProjectSettings.GlobalizePath("user://VYgo/ui_skin_inspector");
            Directory.CreateDirectory(folder);
            var properties = new Dictionary<string, UiSkinTextureIdentity>();
            foreach (string property in new[] { "texture", "texture_normal", "texture_hover", "texture_pressed", "texture_disabled", "texture_focused", "theme_override_styles/panel", "theme_override_styles/normal", "theme_override_styles/hover", "theme_override_styles/pressed" }) {
                var texture = UiSkinService.GetTexture(_target, property);
                if (texture != null) properties[property] = UiSkinService.OriginalIdentity(_target, property, texture);
            }
            var record = new {
                schemaVersion = 1, type = _target.GetClass(), size = new[] { _target.Size.X, _target.Size.Y },
                contexts = UiSkinService.Contexts(_target).Select(c => new { scene = c.Scene, nodePath = c.Path }),
                textures = properties, rules = UiSkinService.MatchingRules(_target)
            };
            string file = Path.Combine(folder, $"capture-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.json");
            System.IO.File.WriteAllText(file, JsonSerializer.Serialize(record, UiSkinManifest.JsonOptions));
            DisplayServer.ClipboardSet(JsonSerializer.Serialize(record, UiSkinManifest.JsonOptions));
            UiSkinService.Warn(file, $"拾取证据已保存并复制：{file}");
        } catch (Exception e) { UiSkinService.Warn("inspector-export", e.Message); }
    }
}
