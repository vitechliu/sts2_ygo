using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace VYgo.Core.Effects;

public partial class Card3DFlipper : Node2D {
    [Export] public SubViewport CaptureViewport = null!;
    [Export] public SubViewport FxViewport = null!;
    [Export] public MeshInstance3D CardMesh = null!;
    [Export] public Node3D Pivot = null!;
    [Export] public Camera3D Camera = null!;
    [Export] public Sprite2D DisplaySprite = null!;

    public async Task PlayFlip(NCard source, Vector2? centerPosition = null, float duration = 1.2f, float scaleMultiplier = 1.4f) {
        if (source?.Model == null || !GodotObject.IsInstanceValid(source)) {
            return;
        }

        CardModel model = source.Model;
        Vector2 displaySize = NCard.defaultSize * source.Scale * scaleMultiplier;
        Vector2I vpSize = new(
            Mathf.RoundToInt(displaySize.X),
            Mathf.RoundToInt(displaySize.Y)
        );

        Vector2 targetGlobalPos = centerPosition ?? source.GlobalPosition;
        source.Visible = false;

        // --- 1. capture the card into a texture ---
        CaptureViewport.Size = vpSize;
        CaptureViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        CaptureViewport.TransparentBg = true;

        NCard? clone = NCard.Create(model);
        if (clone == null) {
            source.Visible = true;
            return;
        }

        clone.Visibility = source.Visibility;
        clone.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
        clone.Scale = new Vector2(scaleMultiplier, scaleMultiplier);
        clone.Position = (Vector2)vpSize * 0.5f - NCard.defaultSize * 0.5f * scaleMultiplier;
        CaptureViewport.AddChild(clone);

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        ViewportTexture frontTex = CaptureViewport.GetTexture();

        // --- 2. setup 3D scene ---
        FxViewport.Size = vpSize;
        FxViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        FxViewport.TransparentBg = true;

        ((QuadMesh)CardMesh.Mesh).Size = displaySize;
        var mat = new StandardMaterial3D {
            AlbedoTexture = frontTex,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Linear
        };
        CardMesh.MaterialOverride = mat;

        Camera.Projection = Camera3D.ProjectionType.Orthogonal;
        Camera.Size = displaySize.Y;

        // --- 3. overlay on 2D UI ---
        DisplaySprite.Texture = FxViewport.GetTexture();
        DisplaySprite.FlipV = true;
        DisplaySprite.GlobalPosition = targetGlobalPos;
        DisplaySprite.ZIndex = 1000;
        DisplaySprite.ZAsRelative = false;

        if (NCombatRoom.Instance != null) {
            NCombatRoom.Instance.AddChild(this);
        }

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        // --- 4. animate real 3D flip ---
        Pivot.Rotation = Vector3.Zero;
        Tween tween = CreateTween();
        tween.TweenProperty(Pivot, "rotation:y", Mathf.DegToRad(360f * 2), duration)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        await tween.AwaitFinished(this);

        // --- 5. cleanup ---
        CaptureViewport.RemoveChild(clone);
        clone.QueueFree();
        CaptureViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
        FxViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
        source.Visible = true;
        QueueFree();
    }
}
