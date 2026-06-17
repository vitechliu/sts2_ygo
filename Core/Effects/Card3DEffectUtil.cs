using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using VYgo.Scripts;

namespace VYgo.Core.Effects;

public sealed record Card3DEffectContext(
    Node3D Pivot,
    Camera3D Camera,
    Sprite2D DisplaySprite,
    NCard Source,
    Vector2 DisplaySize,
    Vector2I CaptureViewportSize,
    Vector2I FxViewportSize
);

public static class Card3DEffectUtil {
    private const float CapturePadding = 40f;
    private const float RotationPaddingRatio = 0.6f;
    private const int ReadyWaitFrames = 5;

    private static readonly PackedScene _flipperScene = GD.Load<PackedScene>("res://VYgo/scenes/vfx/Card3DFlipper.tscn");

    public static async Task RunCard3DEffect(
        NCard source,
        Func<Card3DEffectContext, Task> animate,
        Vector2? centerPosition = null,
        float scaleMultiplier = 1.4f
    ) {
        if (source?.Model == null || !GodotObject.IsInstanceValid(source)) {
            return;
        }

        CardModel model = source.Model;

        if (_flipperScene == null) {
            Entry.Logger.Error("Card3DEffectUtil: failed to load Card3DFlipper.tscn");
            return;
        }

        var flipper = _flipperScene.Instantiate<Card3DFlipper>();
        if (flipper == null) {
            Entry.Logger.Error("Card3DEffectUtil: failed to instantiate Card3DFlipper");
            return;
        }

        if (NCombatRoom.Instance != null) {
            NCombatRoom.Instance.AddChild(flipper);
        }

        // Wait for _Ready() to resolve child nodes.
        for (int i = 0; i < 2; i++) {
            await flipper.ToSignal(flipper.GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        Vector2 targetGlobalPos = centerPosition ?? source.GlobalPosition;
        source.Visible = false;

        try {
            Card3DEffectContext ctx = await BuildContext(flipper, source, model, targetGlobalPos, scaleMultiplier);
            await animate(ctx);
        }
        finally {
            source.Visible = true;
            flipper.QueueFree();
        }
    }

    static async Task<Card3DEffectContext> BuildContext(
        Card3DFlipper flipper,
        NCard source,
        CardModel model,
        Vector2 targetGlobalPos,
        float scaleMultiplier
    ) {
        SubViewport captureVp = flipper.CaptureViewport;
        SubViewport fxVp = flipper.FxViewport;
        MeshInstance3D cardMesh = flipper.CardMesh;
        Node3D pivot = flipper.Pivot;
        Camera3D camera = flipper.Camera;
        Sprite2D displaySprite = flipper.DisplaySprite;

        Vector2 cardRenderScale = source.Scale * scaleMultiplier;
        Vector2 displaySize = NCard.defaultSize * cardRenderScale;

        Vector2 captureDisplaySize = displaySize + Vector2.One * CapturePadding * 2f;
        Vector2I captureVpSize = new(
            Mathf.RoundToInt(captureDisplaySize.X),
            Mathf.RoundToInt(captureDisplaySize.Y)
        );

        float rotationPadding = Mathf.Max(displaySize.X, displaySize.Y) * RotationPaddingRatio;
        Vector2 fxDisplaySize = displaySize + Vector2.One * rotationPadding * 2f;
        Vector2I fxVpSize = new(
            Mathf.RoundToInt(fxDisplaySize.X),
            Mathf.RoundToInt(fxDisplaySize.Y)
        );

        captureVp.Size = captureVpSize;
        captureVp.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        captureVp.TransparentBg = true;

        NCard? clone = NCard.Create(model);
        if (clone == null) {
            throw new InvalidOperationException("Failed to create NCard clone for 3D effect.");
        }

        clone.Visibility = ModelVisibility.Visible;
        clone.UpdateVisuals(PileType.Play, CardPreviewMode.Normal);
        clone.Reload();
        clone.Scale = cardRenderScale;
        clone.Position = (Vector2)captureVpSize * 0.5f;
        captureVp.AddChild(clone);

        for (int i = 0; i < ReadyWaitFrames; i++) {
            await flipper.ToSignal(flipper.GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        if (!GodotObject.IsInstanceValid(clone) || !clone.IsNodeReady()) {
            throw new InvalidOperationException("NCard clone failed to initialize.");
        }
        clone.UpdateVisuals(PileType.Play, CardPreviewMode.Normal);

        ViewportTexture frontTex = captureVp.GetTexture();

        fxVp.Size = fxVpSize;
        fxVp.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        fxVp.TransparentBg = true;

        if (cardMesh.Mesh == null) {
            throw new InvalidOperationException("CardMesh.Mesh is null.");
        }
        ((QuadMesh)cardMesh.Mesh).Size = displaySize;
        cardMesh.MaterialOverride = new StandardMaterial3D {
            AlbedoTexture = frontTex,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Linear
        };

        camera.Projection = Camera3D.ProjectionType.Perspective;
        camera.Fov = 60f;
        camera.Position = new Vector3(0f, 0f, displaySize.Y * 0.9f);

        displaySprite.Texture = fxVp.GetTexture();
        displaySprite.FlipH = true;
        displaySprite.FlipV = false;
        displaySprite.GlobalPosition = targetGlobalPos;
        displaySprite.ZIndex = 1000;
        displaySprite.ZAsRelative = false;

        for (int i = 0; i < 2; i++) {
            await flipper.ToSignal(flipper.GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        pivot.Rotation = Vector3.Zero;

        return new Card3DEffectContext(
            Pivot: pivot,
            Camera: camera,
            DisplaySprite: displaySprite,
            Source: source,
            DisplaySize: displaySize,
            CaptureViewportSize: captureVpSize,
            FxViewportSize: fxVpSize
        );
    }
}
