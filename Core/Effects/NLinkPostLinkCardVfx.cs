using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using VYgo.Scripts;
using VYgo.Utils;

namespace VYgo.Core.Effects;

[ScriptPath("res://Core/Effects/NLinkPostLinkCardVfx.cs")]
public partial class NLinkPostLinkCardVfx : Node2D {
    public const string ScenePath = "res://VYgo/scenes/summon/link/link_post_link_card_vfx.tscn";

    private static readonly Color LinkBlue = new("45d9ff");
    private static readonly Color LinkViolet = new("d05cff");

    private const float FlightDuration = 0.68f;
    private const float SettleDuration = 0.18f;
    private const int MaxTrailPoints = 14;
    private const float RectParticleTextureSize = 256f;

    private Line2D _trailLine = null!;
    private Line2D _trailCore = null!;
    private Node2D _particleLayer = null!;
    private Sprite2D _impactRing = null!;
    private Sprite2D _impactFlash = null!;
    private Sprite2D _afterimageSprite = null!;
    private Texture2D _rectParticleTexture = null!;
    private CanvasItemMaterial _addMaterial = null!;

    public override void _Ready() {
        base._Ready();
        _trailLine = GetNode<Line2D>("TrailLine");
        _trailCore = GetNode<Line2D>("TrailCore");
        _particleLayer = GetNode<Node2D>("ParticleLayer");
        _impactRing = GetNode<Sprite2D>("ImpactRing");
        _impactFlash = GetNode<Sprite2D>("ImpactFlash");
        _afterimageSprite = GetNode<Sprite2D>("AfterimageSprite");
        _rectParticleTexture = _impactRing.Texture;
        _addMaterial = _impactRing.Material as CanvasItemMaterial ?? new CanvasItemMaterial {
            BlendMode = CanvasItemMaterial.BlendModeEnum.Add
        };
        ResetTransientNodes();
    }

    public static async Task Play(CardModel cardModel, Vector2? centerPosition = null) {
        if (TestMode.IsOn || NCombatRoom.Instance == null) {
            return;
        }

        NLinkPostLinkCardVfx vfx = VFXUtil.GenVFXNode<NLinkPostLinkCardVfx>(ScenePath);
        NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(vfx);
        vfx.GlobalPosition = Vector2.Zero;

        try {
            await vfx.PlayInternal(cardModel, centerPosition ?? GetScreenCenter());
        }
        catch (Exception ex) {
            Entry.Logger.Warn("NLinkPostLinkCardVfx exception: " + ex);
        }
        finally {
            if (GodotObject.IsInstanceValid(vfx)) {
                vfx.QueueFreeSafely();
            }
        }
    }

    private static Vector2 GetScreenCenter() {
        return NGame.Instance?.GetViewportRect().Size * 0.5f ?? Vector2.Zero;
    }

    private async Task PlayInternal(CardModel cardModel, Vector2 centerPosition) {
        await Card3DEffectUtil.RunMultipleCard3DEffect(
            new[] { cardModel },
            async (ctxs, target) => {
                if (ctxs.Count > 0) {
                    await AnimatePostLinkCard(ctxs[0], target);
                }
            },
            centerPosition,
            scaleMultiplier: 1.34f,
            horizontalSpacing: 0f,
            initialOpacity: 1f
        );
    }

    private async Task AnimatePostLinkCard(Card3DEffectContext ctx, Vector2 targetPosition) {
        ConfigureCard(ctx);
        ResetTransientNodes();

        Vector2 arrival = targetPosition;
        ctx.DisplaySprite.GlobalPosition = targetPosition;
        ctx.DisplaySprite.Modulate = new Color(1f, 1f, 1f, 0f);
        ctx.DisplaySprite.ZIndex = 1010;
        ctx.GlowSprite.ZIndex = -1;

        ctx.Pivot.Position = new Vector3(0f, 0f, -1550f);
        ctx.Pivot.RotationDegrees = new Vector3(-18f, 74f, -8f);

        Task trailTask = TrackTrail(ctx.DisplaySprite, ctx.DisplaySize, FlightDuration + 0.08f);

        Tween flyTween = ctx.Pivot.CreateTween().SetParallel();
        flyTween.TweenProperty(ctx.DisplaySprite, "modulate:a", 1f, 0.09f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        flyTween.TweenProperty(ctx.Pivot, "position", Vector3.Zero, FlightDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
        flyTween.TweenProperty(ctx.Pivot, "rotation_degrees", new Vector3(0f, -10f, 0f), FlightDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        TweenShaderFloat(flyTween, ctx.CardMaterial, "outline_strength", 0.8f, 3.2f, FlightDuration * 0.5f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        TweenShaderFloat(flyTween, ctx.GlowMaterial, "glow_opacity", 0.08f, 0.9f, FlightDuration * 0.45f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        TweenShaderFloat(flyTween, ctx.GlowMaterial, "vertical_blur", 1f, 0f, FlightDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);

        await flyTween.AwaitFinished(ctx.Pivot);
        await PlayImpact(ctx, arrival);

        Tween settleTween = ctx.Pivot.CreateTween().SetParallel();
        settleTween.TweenProperty(ctx.Pivot, "rotation_degrees", new Vector3(0f, 5f, 0f), SettleDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);
        TweenShaderFloat(settleTween, ctx.CardMaterial, "outline_strength", 3.2f, 1.6f, SettleDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);
        TweenShaderFloat(settleTween, ctx.GlowMaterial, "glow_opacity", 0.9f, 0.48f, SettleDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);

        await Task.WhenAll(trailTask, settleTween.AwaitFinished(ctx.Pivot));
        await VFXUtil.Wait(0.12f);
    }

    private void ConfigureCard(Card3DEffectContext ctx) {
        ctx.CardMaterial.SetShaderParameter("glow_color", LinkBlue);
        ctx.CardMaterial.SetShaderParameter("outline_strength", 0.8f);
        ctx.CardMaterial.SetShaderParameter("outline_width", 3.0f);
        ctx.CardMaterial.SetShaderParameter("pulse_amount", 0.22f);
        ctx.CardMaterial.SetShaderParameter("pulse_speed", 8.0f);

        ctx.GlowMaterial.SetShaderParameter("glow_color", LinkViolet);
        ctx.GlowMaterial.SetShaderParameter("glow_radius", 18f);
        ctx.GlowMaterial.SetShaderParameter("glow_intensity", 2.15f);
        ctx.GlowMaterial.SetShaderParameter("glow_opacity", 0.08f);
        ctx.GlowMaterial.SetShaderParameter("pulse_amount", 0.2f);
        ctx.GlowMaterial.SetShaderParameter("pulse_speed", 8.0f);
        ctx.GlowMaterial.SetShaderParameter("vertical_blur", 1f);
        ctx.GlowMaterial.SetShaderParameter("vertical_blur_length", 145f);
    }

    private async Task TrackTrail(Sprite2D displaySprite, Vector2 displaySize, float duration) {
        List<Vector2> points = new();
        _trailLine.Modulate = Colors.White;
        _trailCore.Modulate = Colors.White;

        float elapsed = 0f;
        float emitAccumulator = 0f;
        while (elapsed < duration && GodotObject.IsInstanceValid(displaySprite) && IsInsideTree()) {
            await this.AwaitProcessFrame();
            float delta = (float)GetProcessDeltaTime();
            elapsed += delta;
            emitAccumulator += delta;

            Vector2 current = displaySprite.GlobalPosition;
            Vector2 jitter = VFXUtil.RandVec2(Mathf.Lerp(22f, 3f, Mathf.Clamp(elapsed / duration, 0f, 1f)));
            if (points.Count == 0 || points[^1].DistanceTo(current + jitter) > 2f) {
                points.Add(current + jitter);
            }
            while (points.Count > MaxTrailPoints) {
                points.RemoveAt(0);
            }

            Vector2[] trailPoints = points.ToArray();
            _trailLine.Points = trailPoints;
            _trailCore.Points = trailPoints;

            while (emitAccumulator >= 0.018f) {
                emitAccumulator -= 0.018f;
                EmitRectParticle(
                    GetRandomCardEdgePosition(current, displaySize),
                    RandomFlightParticleSize(),
                    RandomLinkBlue(0.42f, 0.78f),
                    VFXUtil.RandVec2(36f),
                    (float)GD.RandRange(0.24f, 0.42f)
                );
            }
        }

        Tween fadeTween = CreateTween().SetParallel();
        fadeTween.TweenProperty(_trailLine, "modulate:a", 0f, 0.24f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        fadeTween.TweenProperty(_trailCore, "modulate:a", 0f, 0.18f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        await fadeTween.AwaitFinished(this);
    }

    private async Task PlayImpact(Card3DEffectContext ctx, Vector2 position) {
        _afterimageSprite.Texture = ctx.DisplaySprite.Texture;
        _afterimageSprite.FlipH = ctx.DisplaySprite.FlipH;
        _afterimageSprite.FlipV = ctx.DisplaySprite.FlipV;
        _afterimageSprite.GlobalPosition = position;
        _afterimageSprite.Scale = ctx.DisplaySprite.Scale;
        _afterimageSprite.Modulate = new Color(LinkBlue, 0.66f);
        _afterimageSprite.Visible = true;

        _impactRing.GlobalPosition = position;
        _impactRing.Scale = Vector2.One * 0.18f;
        _impactRing.Rotation = (float)GD.RandRange(-0.25f, 0.25f);
        _impactRing.Modulate = new Color(LinkBlue, 0.95f);
        _impactRing.Visible = true;

        _impactFlash.GlobalPosition = position;
        _impactFlash.Scale = Vector2.One * 0.4f;
        _impactFlash.Modulate = new Color(1f, 0.9f, 1f, 0.85f);
        _impactFlash.Visible = true;

        EmitImpactRectParticles(position, ctx.DisplaySize);

        Tween impactTween = CreateTween().SetParallel();
        impactTween.TweenProperty(_afterimageSprite, "scale", ctx.DisplaySprite.Scale * 1.62f, 0.38f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        impactTween.TweenProperty(_afterimageSprite, "modulate:a", 0f, 0.38f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        impactTween.TweenProperty(_impactRing, "scale", Vector2.One * 1.35f, 0.34f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        impactTween.TweenProperty(_impactRing, "modulate:a", 0f, 0.34f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        impactTween.TweenProperty(_impactFlash, "scale", Vector2.One * 1.15f, 0.18f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        impactTween.TweenProperty(_impactFlash, "modulate:a", 0f, 0.18f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);

        await impactTween.AwaitFinished(this);
        _afterimageSprite.Visible = false;
        _impactRing.Visible = false;
        _impactFlash.Visible = false;
    }

    private void ResetTransientNodes() {
        if (_trailLine == null) return;

        _trailLine.ClearPoints();
        _trailCore.ClearPoints();
        _impactRing.Visible = false;
        _impactFlash.Visible = false;
        _afterimageSprite.Visible = false;
        foreach (Node child in _particleLayer.GetChildren()) {
            child.QueueFree();
        }
    }

    private void EmitImpactRectParticles(Vector2 center, Vector2 displaySize) {
        int count = 48;
        Vector2 half = displaySize * 0.55f;
        for (int i = 0; i < count; i++) {
            Vector2 origin = GetRandomCardEdgePosition(center, displaySize);
            Vector2 away = origin - center;
            if (away.LengthSquared() < 1f) {
                away = Vector2.Right.Rotated((float)GD.RandRange(0f, Mathf.Tau));
            }
            away = away.Normalized();

            float edgeBoost = (float)GD.RandRange(24f, 150f);
            float drift = (float)GD.RandRange(80f, 260f);
            Vector2 start = origin + away * edgeBoost + new Vector2(
                (float)GD.RandRange(-half.X * 0.14f, half.X * 0.14f),
                (float)GD.RandRange(-half.Y * 0.14f, half.Y * 0.14f)
            );
            EmitRectParticle(
                start,
                RandomImpactParticleSize(),
                RandomLinkBlue(0.5f, 0.9f),
                away * drift + VFXUtil.RandVec2(42f),
                (float)GD.RandRange(0.48f, 0.86f)
            );
        }
    }

    private Vector2 GetRandomCardEdgePosition(Vector2 center, Vector2 displaySize) {
        Vector2 half = displaySize * new Vector2(0.52f, 0.56f);
        float side = (float)GD.Randf();
        if (side < 0.34f) {
            return center + new Vector2(
                (float)(GD.Randf() < 0.5f ? -half.X : half.X) + (float)GD.RandRange(-20f, 20f),
                (float)GD.RandRange(-half.Y, half.Y)
            );
        }
        if (side < 0.68f) {
            return center + new Vector2(
                (float)GD.RandRange(-half.X, half.X),
                (float)(GD.Randf() < 0.5f ? -half.Y : half.Y) + (float)GD.RandRange(-20f, 20f)
            );
        }
        return center + new Vector2(
            (float)GD.RandRange(-half.X * 1.15f, half.X * 1.15f),
            (float)GD.RandRange(-half.Y * 1.15f, half.Y * 1.15f)
        );
    }

    private Vector2 RandomFlightParticleSize() {
        float roll = (float)GD.Randf();
        if (roll < 0.45f) {
            return new Vector2((float)GD.RandRange(8f, 18f), (float)GD.RandRange(38f, 104f));
        }
        if (roll < 0.82f) {
            return new Vector2((float)GD.RandRange(28f, 72f), (float)GD.RandRange(10f, 25f));
        }
        return new Vector2((float)GD.RandRange(22f, 58f), (float)GD.RandRange(22f, 58f));
    }

    private Vector2 RandomImpactParticleSize() {
        float roll = (float)GD.Randf();
        if (roll < 0.38f) {
            return new Vector2((float)GD.RandRange(14f, 28f), (float)GD.RandRange(42f, 130f));
        }
        if (roll < 0.76f) {
            return new Vector2((float)GD.RandRange(42f, 116f), (float)GD.RandRange(14f, 34f));
        }
        return new Vector2((float)GD.RandRange(34f, 86f), (float)GD.RandRange(34f, 86f));
    }

    private Color RandomLinkBlue(float minAlpha, float maxAlpha) {
        return new Color(
            (float)GD.RandRange(0.25f, 0.46f),
            (float)GD.RandRange(0.68f, 0.95f),
            1f,
            (float)GD.RandRange(minAlpha, maxAlpha)
        );
    }

    private void EmitRectParticle(Vector2 globalPosition, Vector2 size, Color color, Vector2 velocity, float lifetime) {
        Sprite2D particle = new() {
            Texture = _rectParticleTexture,
            Material = _addMaterial,
            Centered = true,
            GlobalPosition = globalPosition,
            Rotation = 0f,
            Scale = size / RectParticleTextureSize,
            Modulate = color,
            ZIndex = 1005,
            ZAsRelative = false
        };
        _particleLayer.AddChild(particle);

        Tween tween = particle.CreateTween().SetParallel();
        tween.TweenProperty(particle, "global_position", globalPosition + velocity, lifetime)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(particle, "modulate:a", 0f, lifetime)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Quad);
        tween.TweenCallback(Callable.From(() => {
            if (GodotObject.IsInstanceValid(particle)) {
                particle.QueueFree();
            }
        })).SetDelay(lifetime);
    }

    private static MethodTweener TweenShaderFloat(
        Tween tween,
        ShaderMaterial material,
        StringName parameter,
        float from,
        float to,
        double duration
    ) {
        return tween.TweenMethod(
            Callable.From<float>(value => material.SetShaderParameter(parameter, value)),
            from,
            to,
            duration
        );
    }
}
