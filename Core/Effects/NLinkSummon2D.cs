using Godot;

namespace VYgo.Core.Effects;

public partial class NLinkSummon2D: Node2D {
    [Export] public NLinkSummonManager manager = null!;
    private ColorRect _background = null!;

    public override void _Ready() {
        ZIndex = 900;
        Vector2 size = GetViewportRect().Size;
        _background = new ColorRect { Position = -size * 0.5f, Size = size,
            Color = new Color(0,0,0,0.55f), MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(_background); MoveChild(_background,0);
        var container = GetNode<SubViewportContainer>("SubViewportContainer");
        container.Position = -size * 0.5f; container.Size = size; container.Stretch = true;
        container.MouseFilter = Control.MouseFilterEnum.Ignore;
        container.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.PremultAlpha };
        Visible = false; manager.Visible = false;
    }
}
