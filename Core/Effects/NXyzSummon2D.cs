using Godot;

namespace VYgo.Core.Effects;

public partial class NXyzSummon2D : Node2D {
    [Export] public NXyzSummonManager Manager = null!;

    private SubViewportContainer _viewportContainer = null!;

    public override void _Ready() {
        base._Ready();
        _viewportContainer = GetNode<SubViewportContainer>("SubViewportContainer");
        ResizeViewport();
    }

    private void ResizeViewport() {
        Vector2 viewportSize = GetViewportRect().Size;
        if (viewportSize.X <= 0f || viewportSize.Y <= 0f) {
            viewportSize = new Vector2(1920f, 1080f);
        }

        _viewportContainer.Position = -viewportSize * 0.5f;
        _viewportContainer.Size = viewportSize;
    }
}
