using Godot;
namespace VYgo.Core.Effects;

public partial class NRitualSummon2D : Node2D
{
    [Export] public NRitualSummonManager Manager = null!;
    private ColorRect _background = null!;
    public override void _Ready()
    {
        Vector2 size = GetViewportRect().Size;
        _background = new ColorRect { Position = -size * 0.5f, Size = size, Color = Colors.Transparent, MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(_background); MoveChild(_background, 0);
        var container = GetNode<SubViewportContainer>("SubViewportContainer");
        container.Position = -size * 0.5f; container.Size = size;
        container.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.PremultAlpha };
    }
    public override void _Process(double delta) => _background.Color = new Color(0, 0, 0, Manager.BackgroundOpacity);
}
