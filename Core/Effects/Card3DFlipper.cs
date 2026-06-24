using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace VYgo.Core.Effects;

public partial class Card3DFlipper : Node2D {
    public SubViewport CaptureViewport = null!;
    public SubViewport FxViewport = null!;
    public MeshInstance3D CardMesh = null!;
    public Node3D Pivot = null!;
    public Camera3D Camera = null!;
    public Sprite2D DisplaySprite = null!;
    public Sprite2D GlowSprite = null!;

    public override void _Ready() {
        base._Ready();
        CaptureViewport = GetNode<SubViewport>("CaptureViewport");
        FxViewport = GetNode<SubViewport>("FxViewport");
        CardMesh = GetNode<MeshInstance3D>("FxViewport/Pivot/CardMesh");
        Pivot = GetNode<Node3D>("FxViewport/Pivot");
        Camera = GetNode<Camera3D>("FxViewport/Camera3D");
        DisplaySprite = GetNode<Sprite2D>("DisplaySprite");
        GlowSprite = GetNode<Sprite2D>("DisplaySprite/GlowSprite");
    }
}
