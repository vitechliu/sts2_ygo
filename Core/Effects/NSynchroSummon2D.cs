using Godot;

namespace VYgo.Core.Effects;

/// <summary>同调召唤的全屏前后景 SubViewport 包装节点。</summary>
public partial class NSynchroSummon2D : Node2D {
    [Export] public NSynchroSummonManager Manager = null!;
    [Export] public NSynchroSummonManager ForegroundManager = null!;

    private SubViewportContainer[] _viewportContainers = [];
    private ColorRect _backdrop = null!;

    public override void _Ready() {
        base._Ready();
        _viewportContainers = [
            GetNode<SubViewportContainer>("SubViewportContainer"),
            GetNode<SubViewportContainer>("ForegroundViewportContainer")
        ];
        _backdrop = GetNode<ColorRect>("Backdrop");
        GetViewport().SizeChanged += ResizeViewport;
        ResizeViewport();
    }

    public override void _ExitTree() {
        if (GetViewport() != null) GetViewport().SizeChanged -= ResizeViewport;
        base._ExitTree();
    }

    private void ResizeViewport() {
        Vector2 viewportSize = GetViewportRect().Size;
        if (viewportSize.X <= 0f || viewportSize.Y <= 0f) {
            viewportSize = new Vector2(1920f, 1080f);
        }

        foreach (SubViewportContainer container in _viewportContainers) {
            container.Position = -viewportSize * 0.5f;
            container.Size = viewportSize;
            container.GetNode<SubViewport>("SubViewport").Size = new Vector2I(
                Math.Max(1, Mathf.RoundToInt(viewportSize.X)),
                Math.Max(1, Mathf.RoundToInt(viewportSize.Y))
            );
        }
        _backdrop.Position = -viewportSize * 0.5f;
        _backdrop.Size = viewportSize;
    }
}
