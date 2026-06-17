using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using VYgo.Scripts;

namespace VYgo.Core.Effects;

public partial class Card3DFlipper : Node2D {
    public SubViewport? CaptureViewport;
    public SubViewport? FxViewport;
    public MeshInstance3D? CardMesh;
    public Node3D? Pivot;
    public Camera3D? Camera ;
    public Sprite2D? DisplaySprite;

    public override void _Ready() {
        base._Ready();
        Entry.Logger.Info("Card3DFlipper._Ready start");
        CaptureViewport = GetNode<SubViewport>("CaptureViewport");
        FxViewport = GetNode<SubViewport>("FxViewport");
        CardMesh = GetNode<MeshInstance3D>("FxViewport/Pivot/CardMesh");
        Pivot = GetNode<Node3D>("FxViewport/Pivot");
        Camera = GetNode<Camera3D>("FxViewport/Camera3D");
        DisplaySprite = GetNode<Sprite2D>("DisplaySprite");
        Entry.Logger.Info($"Card3DFlipper._Ready done: CaptureViewport={CaptureViewport != null}, FxViewport={FxViewport != null}, CardMesh={CardMesh != null}, Pivot={Pivot != null}, Camera={Camera != null}, DisplaySprite={DisplaySprite != null}");
    }

    public async Task PlayFlip(NCard source, Vector2? centerPosition = null, float duration = 1.2f, float scaleMultiplier = 1.4f) {
        Entry.Logger.Info("Card3DFlipper.PlayFlip start");
        if (source == null) {
            Entry.Logger.Warn("Card3DFlipper.PlayFlip: source is null");
            return;
        }
        if (!GodotObject.IsInstanceValid(source)) {
            Entry.Logger.Warn("Card3DFlipper.PlayFlip: source is not valid");
            return;
        }
        if (source.Model == null) {
            Entry.Logger.Warn("Card3DFlipper.PlayFlip: source.Model is null");
            return;
        }

        CardModel model = source.Model;
        Entry.Logger.Info($"Card3DFlipper.PlayFlip: model={model.Id}, scale={source.Scale}, centerPosition={centerPosition}");

        // Resolve nodes lazily so PlayFlip can be called immediately after Instantiate().
        CaptureViewport ??= GetNodeOrNull<SubViewport>("CaptureViewport");
        FxViewport ??= GetNodeOrNull<SubViewport>("FxViewport");
        CardMesh ??= GetNodeOrNull<MeshInstance3D>("FxViewport/Pivot/CardMesh");
        Pivot ??= GetNodeOrNull<Node3D>("FxViewport/Pivot");
        Camera ??= GetNodeOrNull<Camera3D>("FxViewport/Camera3D");
        DisplaySprite ??= GetNodeOrNull<Sprite2D>("DisplaySprite");

        if (CaptureViewport == null) { Entry.Logger.Error("Card3DFlipper.PlayFlip: CaptureViewport is null"); return; }
        if (FxViewport == null) { Entry.Logger.Error("Card3DFlipper.PlayFlip: FxViewport is null"); return; }
        if (CardMesh == null) { Entry.Logger.Error("Card3DFlipper.PlayFlip: CardMesh is null"); return; }
        if (Pivot == null) { Entry.Logger.Error("Card3DFlipper.PlayFlip: Pivot is null"); return; }
        if (Camera == null) { Entry.Logger.Error("Card3DFlipper.PlayFlip: Camera is null"); return; }
        if (DisplaySprite == null) { Entry.Logger.Error("Card3DFlipper.PlayFlip: DisplaySprite is null"); return; }

        // Use the card's current on-table scale as the base, then apply the effect multiplier once.
        Vector2 cardRenderScale = source.Scale * scaleMultiplier;
        Vector2 cardBaseSize = NCard.defaultSize;
        Vector2 displaySize = cardBaseSize * cardRenderScale;

        // Add padding to the capture viewport so glows/shadows are not clipped.
        const float capturePadding = 40f;
        Vector2 captureDisplaySize = displaySize + Vector2.One * capturePadding * 2f;
        Vector2I captureVpSize = new(
            Mathf.RoundToInt(captureDisplaySize.X),
            Mathf.RoundToInt(captureDisplaySize.Y)
        );

        // The 3D viewport needs extra vertical/horizontal space for perspective rotation.
        float rotationPadding = Mathf.Max(displaySize.X, displaySize.Y) * 0.6f;
        Vector2 fxDisplaySize = displaySize + Vector2.One * rotationPadding * 2f;
        Vector2I fxVpSize = new(
            Mathf.RoundToInt(fxDisplaySize.X),
            Mathf.RoundToInt(fxDisplaySize.Y)
        );

        Vector2 targetGlobalPos = centerPosition ?? source.GlobalPosition;
        source.Visible = false;

        // Must be in the scene tree before awaiting frame signals.
        if (NCombatRoom.Instance != null) {
            Entry.Logger.Info("Card3DFlipper.PlayFlip: adding to NCombatRoom");
            NCombatRoom.Instance.AddChild(this);
        }
        else {
            Entry.Logger.Warn("Card3DFlipper.PlayFlip: NCombatRoom.Instance is null");
        }

        // --- 1. capture the card into a texture ---
        Entry.Logger.Info($"Card3DFlipper.PlayFlip: setting up CaptureViewport size={captureVpSize}");
        CaptureViewport.Size = captureVpSize;
        CaptureViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        CaptureViewport.TransparentBg = true;

        Entry.Logger.Info("Card3DFlipper.PlayFlip: creating NCard clone");
        NCard? clone = NCard.Create(model);
        if (clone == null) {
            Entry.Logger.Warn("Card3DFlipper.PlayFlip: NCard.Create returned null");
            source.Visible = true;
            return;
        }
        Entry.Logger.Info("Card3DFlipper.PlayFlip: clone created");

        clone.Visibility = source.Visibility;
        clone.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
        clone.Scale = cardRenderScale;
        clone.Position = (Vector2)captureVpSize * 0.5f;
        CaptureViewport.AddChild(clone);
        Entry.Logger.Info($"Card3DFlipper.PlayFlip: clone added to CaptureViewport, clone.Scale={clone.Scale}, clone.Position={clone.Position}, vpSize={captureVpSize}");

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        Entry.Logger.Info("Card3DFlipper.PlayFlip: getting front texture");
        ViewportTexture frontTex = CaptureViewport.GetTexture();
        if (frontTex == null) {
            Entry.Logger.Error("Card3DFlipper.PlayFlip: frontTex is null");
            source.Visible = true;
            return;
        }

        // --- 2. setup 3D scene ---
        Entry.Logger.Info($"Card3DFlipper.PlayFlip: setting up FxViewport size={fxVpSize}");
        FxViewport.Size = fxVpSize;
        FxViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        FxViewport.TransparentBg = true;

        Entry.Logger.Info("Card3DFlipper.PlayFlip: setting up quad material");
        if (CardMesh.Mesh == null) {
            Entry.Logger.Error("Card3DFlipper.PlayFlip: CardMesh.Mesh is null");
            source.Visible = true;
            return;
        }
        ((QuadMesh)CardMesh.Mesh).Size = displaySize;
        var mat = new StandardMaterial3D {
            AlbedoTexture = frontTex,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Linear
        };
        CardMesh.MaterialOverride = mat;

        // Perspective camera so rotation around Y looks like a real 3D flip.
        Camera.Projection = Camera3D.ProjectionType.Perspective;
        Camera.Fov = 60f;
        Camera.Position = new Vector3(0f, 0f, displaySize.Y * 0.9f);

        // --- 3. overlay on 2D UI ---
        Entry.Logger.Info("Card3DFlipper.PlayFlip: setting up DisplaySprite");
        DisplaySprite.Texture = FxViewport.GetTexture();
        DisplaySprite.FlipH = true;
        DisplaySprite.FlipV = false;
        DisplaySprite.GlobalPosition = targetGlobalPos;
        DisplaySprite.ZIndex = 1000;
        DisplaySprite.ZAsRelative = false;

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        // --- 4. animate real 3D flip ---
        Entry.Logger.Info("Card3DFlipper.PlayFlip: starting flip animation");
        Pivot.Rotation = Vector3.Zero;
        Tween tween = CreateTween();
        tween.TweenProperty(Pivot, "rotation:y", Mathf.DegToRad(360f * 2), duration)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        await tween.AwaitFinished(this);

        // --- 5. cleanup ---
        Entry.Logger.Info("Card3DFlipper.PlayFlip: cleanup");
        CaptureViewport.RemoveChild(clone);
        clone.QueueFree();
        CaptureViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
        FxViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
        source.Visible = true;
        QueueFree();
    }
}
