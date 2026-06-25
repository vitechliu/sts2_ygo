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
    Sprite2D GlowSprite,
    ShaderMaterial CardMaterial,
    ShaderMaterial GlowMaterial,
    NCard Source,
    Vector2 DisplaySize
);

public static class Card3DEffectUtil {
    private const float CapturePadding = 32f;
    private const float FxHorizontalPadding = 96f;
    private const float FxVerticalPadding = 190f;
    private const int ReadyWaitFrames = 5;

    private static readonly PackedScene _flipperScene = GD.Load<PackedScene>("res://VYgo/scenes/vfx/Card3DFlipper.tscn");
    private static readonly Shader _cardOutlineShader = GD.Load<Shader>("res://VYgo/shaders/card_3d_outline.gdshader");
    private static readonly Shader _cardOuterGlowShader = GD.Load<Shader>("res://VYgo/shaders/card_outer_glow.gdshader");

    public static async Task RunCard3DEffect(
        NCard source,
        Func<Card3DEffectContext, Task> animate,
        Vector2? centerPosition = null,
        float scaleMultiplier = 1.4f
    ) {
        if (source?.Model == null || !GodotObject.IsInstanceValid(source)) {
            return;
        }

        await RunMultipleCard3DEffect(
            new[] { source.Model },
            async (ctxs, _) => {
                if (ctxs.Count > 0) {
                    await animate(ctxs[0]);
                }
            },
            centerPosition,
            scaleMultiplier,
            horizontalSpacing: 0f
        );
    }

    public static async Task RunMultipleCard3DEffect(
        IEnumerable<CardModel> models,
        Func<IReadOnlyList<Card3DEffectContext>, Vector2, Task> animate,
        Vector2? centerPosition = null,
        float scaleMultiplier = 1.4f,
        float horizontalSpacing = 360f,
        float initialOpacity = 1f
    ) {
        List<CardModel> modelList = models.Where(m => m != null).ToList();
        if (modelList.Count == 0) {
            return;
        }

        if (_flipperScene == null) {
            Entry.Logger.Error("Card3DEffectUtil: failed to load Card3DFlipper.tscn");
            return;
        }

        Vector2 targetGlobalPos = centerPosition ?? (NCombatRoom.Instance?.GetGlobalMousePosition() ?? Vector2.Zero);

        List<Card3DFlipper> flippers = new();
        List<Card3DEffectContext> contexts = new();
        Dictionary<CardModel, NCard> hiddenSourceNodes = new();

        try {
            int count = modelList.Count;
            float totalWidth = (count - 1) * horizontalSpacing;

            for (int i = 0; i < count; i++) {
                CardModel model = modelList[i];
                Vector2 offset = new((i * horizontalSpacing) - totalWidth * 0.5f, 0f);
                Vector2 cardGlobalPos = targetGlobalPos + offset;

                Card3DFlipper? flipper = CreateAndAttachFlipper();
                if (flipper == null) {
                    continue;
                }

                await WaitFrames(flipper, 1);

                NCard? sourceNode = NCard.FindOnTable(model);
                if (sourceNode != null && GodotObject.IsInstanceValid(sourceNode)) {
                    sourceNode.Visible = false;
                    hiddenSourceNodes[model] = sourceNode;
                }

                contexts.Add(await BuildContext(flipper, model, cardGlobalPos, scaleMultiplier, initialOpacity));
                flippers.Add(flipper);
            }

            await animate(contexts, targetGlobalPos);
        }
        finally {
            foreach (var flipper in flippers) {
                if (GodotObject.IsInstanceValid(flipper)) {
                    flipper.QueueFree();
                }
            }
            foreach (var pair in hiddenSourceNodes) {
                if (GodotObject.IsInstanceValid(pair.Value)) {
                    pair.Value.Visible = true;
                }
            }
        }
    }

    static Card3DFlipper? CreateAndAttachFlipper() {
        var flipper = _flipperScene.Instantiate<Card3DFlipper>();
        if (flipper == null) {
            Entry.Logger.Error("Card3DEffectUtil: failed to instantiate Card3DFlipper");
            return null;
        }

        NCombatRoom.Instance?.AddChild(flipper);
        return flipper;
    }

    static async Task WaitFrames(Node node, int frames) {
        for (int i = 0; i < frames; i++) {
            await node.ToSignal(node.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    static async Task<Card3DEffectContext> BuildContext(
        Card3DFlipper flipper,
        CardModel model,
        Vector2 targetGlobalPos,
        float scaleMultiplier,
        float initialOpacity
    ) {
        SubViewport captureVp = flipper.CaptureViewport;
        SubViewport fxVp = flipper.FxViewport;
        MeshInstance3D cardMesh = flipper.CardMesh;
        Node3D pivot = flipper.Pivot;
        Camera3D camera = flipper.Camera;
        Sprite2D displaySprite = flipper.DisplaySprite;
        Sprite2D glowSprite = flipper.GlowSprite;

        Vector2 displaySize = NCard.defaultSize * scaleMultiplier;

        captureVp.Size = RoundToSize(displaySize + Vector2.One * CapturePadding * 2f);
        captureVp.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        captureVp.TransparentBg = true;

        fxVp.Size = RoundToSize(new Vector2(
            captureVp.Size.X + FxHorizontalPadding * 2f,
            captureVp.Size.Y + FxVerticalPadding * 2f
        ));
        fxVp.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        fxVp.TransparentBg = true;

        NCard? clone = CreateCardClone(model, captureVp.Size, scaleMultiplier);
        if (clone == null) {
            throw new InvalidOperationException("Failed to create NCard clone for 3D effect.");
        }
        captureVp.AddChild(clone);

        await WaitFrames(flipper, ReadyWaitFrames);
        clone.UpdateVisuals(PileType.Play, CardPreviewMode.Normal);

        captureVp.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
        await WaitFrames(flipper, 1);
        captureVp.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;

        ShaderMaterial cardMaterial = SetupCardMesh(cardMesh, captureVp.GetTexture(), captureVp.Size);
        SetupCamera(camera, fxVp.Size);

        await WaitFrames(flipper, 1);

        displaySprite.Texture = fxVp.GetTexture();
        displaySprite.FlipH = true;
        displaySprite.FlipV = false;
        displaySprite.GlobalPosition = targetGlobalPos;
        displaySprite.Modulate = new Color(1f, 1f, 1f, initialOpacity);
        displaySprite.ZIndex = 1000;
        displaySprite.ZAsRelative = false;

        ShaderMaterial glowMaterial = SetupGlowSprite(glowSprite, fxVp.GetTexture());

        await WaitFrames(flipper, 1);

        pivot.Rotation = Vector3.Zero;

        return new Card3DEffectContext(
            Pivot: pivot,
            Camera: camera,
            DisplaySprite: displaySprite,
            GlowSprite: glowSprite,
            CardMaterial: cardMaterial,
            GlowMaterial: glowMaterial,
            Source: clone,
            DisplaySize: displaySize
        );
    }

    static Vector2I RoundToSize(Vector2 size) {
        return new(Mathf.RoundToInt(size.X), Mathf.RoundToInt(size.Y));
    }

    static NCard? CreateCardClone(CardModel model, Vector2I viewportSize, float scale) {
        NCard? clone = NCard.Create(model);
        if (clone == null) {
            return null;
        }

        clone.Visibility = ModelVisibility.Visible;
        clone.UpdateVisuals(PileType.Play, CardPreviewMode.Normal);
        clone.Reload();
        clone.Scale = Vector2.One * scale;
        clone.Position = (Vector2)viewportSize * 0.5f;
        return clone;
    }

    static ShaderMaterial SetupCardMesh(MeshInstance3D cardMesh, ViewportTexture texture, Vector2 meshSize) {
        if (cardMesh.Mesh == null) {
            throw new InvalidOperationException("CardMesh.Mesh is null.");
        }
        if (_cardOutlineShader == null) {
            throw new InvalidOperationException("Failed to load card_3d_outline.gdshader.");
        }

        ((QuadMesh)cardMesh.Mesh).Size = meshSize;
        ShaderMaterial material = new() {
            Shader = _cardOutlineShader
        };
        material.SetShaderParameter("card_texture", texture);
        cardMesh.MaterialOverride = material;
        return material;
    }

    static ShaderMaterial SetupGlowSprite(Sprite2D glowSprite, ViewportTexture texture) {
        if (_cardOuterGlowShader == null) {
            throw new InvalidOperationException("Failed to load card_outer_glow.gdshader.");
        }

        ShaderMaterial material = new() {
            Shader = _cardOuterGlowShader
        };
        glowSprite.Texture = texture;
        glowSprite.FlipH = true;
        glowSprite.FlipV = false;
        glowSprite.Material = material;
        return material;
    }

    static void SetupCamera(Camera3D camera, Vector2I viewportSize) {
        camera.Projection = Camera3D.ProjectionType.Perspective;
        camera.Fov = 60f;
        float distance = viewportSize.Y / (2f * Mathf.Tan(Mathf.DegToRad(camera.Fov * 0.5f)));
        camera.Position = new Vector3(0f, 0f, distance);
    }
}
