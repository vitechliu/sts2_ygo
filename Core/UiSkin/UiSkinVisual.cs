using Godot;

namespace VYgo.Core.UiSkin;

/// <summary>只负责一个图层的绘制；原控件仍掌管位置、可见性、点击与动画。</summary>
public partial class UiSkinVisual : Node2D {
    private Control _host = null!;
    private UiSkinRule _rule = null!;
    private Texture2D? _texture;
    private StyleBoxTexture? _nine;
    private Color _selfModulate;
    public void Configure(Control host, UiSkinRule rule) { _host = host; _rule = rule; }
    public override void _Ready() {
        _selfModulate = _host.SelfModulate;
        _host.SelfModulate = new Color(_selfModulate, 0);
        // 背景节点可能还包含文字或图标，替换层仍在这些子节点后方。
        ShowBehindParent = true;
        UseParentMaterial = !_rule.Neutralize;
        // 在片元阶段忽略原版顶点染色，仍留在原节点树中继承裁切、变换和可见性。
        // 不用 TopLevel 绘制，避免在滚动容器外漏出图层。
        if (_rule.Neutralize) Material = new ShaderMaterial {
            Shader = new Shader { Code = "shader_type canvas_item; render_mode unshaded; void fragment() { COLOR = texture(TEXTURE, UV); }" }
        };
        else SelfModulate = _selfModulate;
        _host.Resized += QueueRedraw;
        SetProcess(false);
    }
    public void SetTexture(Texture2D texture) {
        if (_texture == texture) return;
        _texture = texture;
        if (_rule.Mode == "nine" || _host is NinePatchRect && _rule.Mode != "tile") {
            _nine = new StyleBoxTexture { Texture = texture };
            if (_rule.Mode == "nine") for (int i = 0; i < 4; i++) _nine.SetTextureMargin((Side)i, _rule.Border[i]);
            else if (_host is NinePatchRect native) {
                _nine.RegionRect = native.RegionRect;
                _nine.DrawCenter = native.DrawCenter;
                _nine.AxisStretchHorizontal = (StyleBoxTexture.AxisStretchMode)native.AxisStretchHorizontal;
                _nine.AxisStretchVertical = (StyleBoxTexture.AxisStretchMode)native.AxisStretchVertical;
                for (int i = 0; i < 4; i++) _nine.SetTextureMargin((Side)i, native.GetPatchMargin((Side)i));
            }
        }
        QueueRedraw();
    }
    public override void _Draw() {
        if (_texture == null || !GodotObject.IsInstanceValid(_host)) return;
        var rect = new Rect2(Vector2.Zero, _host.Size);
        if (_host is TextureRect flip) DrawSetTransform(new Vector2(flip.FlipH ? rect.Size.X : 0, flip.FlipV ? rect.Size.Y : 0), 0,
            new Vector2(flip.FlipH ? -1 : 1, flip.FlipV ? -1 : 1));
        if (_nine != null) DrawStyleBox(_nine, rect);
        else if (_host is TextureRect tr && _rule.Mode != "tile" && tr.StretchMode is TextureRect.StretchModeEnum.KeepAspectCentered or TextureRect.StretchModeEnum.KeepAspect) {
            Vector2 size = _texture.GetSize();
            float scale = Math.Min(rect.Size.X / size.X, rect.Size.Y / size.Y);
            size *= scale;
            DrawTextureRect(_texture, new Rect2(tr.StretchMode == TextureRect.StretchModeEnum.KeepAspectCentered ? (rect.Size - size) / 2 : Vector2.Zero, size), false);
        } else DrawTextureRect(_texture, rect, _rule.Mode == "tile");
    }
    public void Restore() { if (GodotObject.IsInstanceValid(_host)) _host.SelfModulate = _selfModulate; Visible = false; SetProcess(false); }
    public override void _ExitTree() {
        if (GodotObject.IsInstanceValid(_host)) { _host.Resized -= QueueRedraw; _host.SelfModulate = _selfModulate; }
    }
}
