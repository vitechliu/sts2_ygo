using Godot;

namespace VYgo.Core.Effects;

public partial class NXyzSummon2D : Node2D {
    [Export] public NXyzSummonManager Manager = null!;
    [Export] public NXyzSummonManager ForegroundManager = null!;

    private SubViewportContainer[] _viewportContainers = [];

    public override void _Ready() {
        base._Ready();
        _viewportContainers = [
            GetNode<SubViewportContainer>("SubViewportContainer"),
            GetNode<SubViewportContainer>("ForegroundViewportContainer")
        ];
        ResizeViewport();
    }

    private void ResizeViewport() {
        Vector2 viewportSize = GetViewportRect().Size;
        if (viewportSize.X <= 0f || viewportSize.Y <= 0f) {
            viewportSize = new Vector2(1920f, 1080f);
        }

        foreach (SubViewportContainer container in _viewportContainers) {
            container.Position = -viewportSize * 0.5f;
            container.Size = viewportSize;
        }
    }
}
