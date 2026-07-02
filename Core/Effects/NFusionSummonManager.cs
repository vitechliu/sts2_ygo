using Godot;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using VYgo.Utils;

namespace VYgo.Core.Effects;

public partial class NFusionSummonManager : Node2D {
    private static readonly Color FusionRed = new("ff315e");
    private static readonly Color FusionBlue = new("3fb4ff");
    private static readonly Color FusionViolet = new("bd4cff");

    private Sprite2D _vortexRed = null!;
    private Sprite2D _vortexBlue = null!;
    private Sprite2D _vortexCore = null!;
    private Sprite2D _burstFlash = null!;
    private Sprite2D _thunder = null!;
    private Node2D _particleLayer = null!;

    public override void _Ready() {
        base._Ready();
        _vortexRed = GetNode<Sprite2D>("VortexRed");
        _vortexBlue = GetNode<Sprite2D>("VortexBlue");
        _vortexCore = GetNode<Sprite2D>("VortexCore");
        _burstFlash = GetNode<Sprite2D>("BurstFlash");
        _thunder = GetNode<Sprite2D>("Thunder");
        _particleLayer = GetNode<Node2D>("ParticleLayer");
        Reset();
    }

    public void Reset() {
        _vortexRed.Modulate = new Color(FusionRed, 0f);
        _vortexBlue.Modulate = new Color(FusionBlue, 0f);
        _vortexCore.Modulate = new Color(FusionViolet, 0f);
        _burstFlash.Modulate = new Color(Colors.White, 0f);
        _thunder.Modulate = new Color(FusionBlue, 0f);
        _vortexRed.Scale = Vector2.One * 0.15f;
        _vortexBlue.Scale = Vector2.One * 0.15f;
        _vortexCore.Scale = Vector2.One * 0.08f;
        _burstFlash.Scale = Vector2.One * 0.25f;
        _thunder.Scale = Vector2.One * 0.7f;
        foreach (Node child in _particleLayer.GetChildren()) {
            child.QueueFree();
        }
    }

    public async Task PlayIntro() {
        Reset();

        Tween tween = CreateTween().SetParallel();
        tween.TweenProperty(_vortexRed, "modulate:a", 0.75f, 0.28f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(_vortexBlue, "modulate:a", 0.72f, 0.28f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(_vortexCore, "modulate:a", 0.55f, 0.36f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(_vortexRed, "scale", Vector2.One * 1.35f, 0.5f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(_vortexBlue, "scale", Vector2.One * 1.12f, 0.5f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(_vortexCore, "scale", Vector2.One * 0.88f, 0.5f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);

        _ = SpinVortex(1.7f, 1.0f);
        await tween.AwaitFinished(this);
    }

    public async Task PlayBurst() {
        SFXUtil.Play("event:/vygo/sfx/link_summon_04");
        EmitFusionSparks(36);
        _thunder.Rotation = (float)GD.RandRange(-0.35f, 0.35f);

        Tween tween = CreateTween().SetParallel();
        tween.TweenProperty(_vortexRed, "scale", Vector2.One * 1.8f, 0.24f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(_vortexBlue, "scale", Vector2.One * 1.55f, 0.24f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(_vortexCore, "scale", Vector2.One * 1.35f, 0.18f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(_vortexRed, "modulate:a", 0.18f, 0.38f)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(_vortexBlue, "modulate:a", 0.18f, 0.38f)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(_vortexCore, "modulate:a", 0f, 0.28f)
            .SetDelay(0.12f)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(_burstFlash, "modulate:a", 0.95f, 0.06f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(_burstFlash, "scale", Vector2.One * 2.2f, 0.32f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(_burstFlash, "modulate:a", 0f, 0.24f)
            .SetDelay(0.08f)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(_thunder, "modulate:a", 0.86f, 0.06f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(_thunder, "scale", Vector2.One * 1.55f, 0.26f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(_thunder, "modulate:a", 0f, 0.2f)
            .SetDelay(0.08f)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Quad);

        await tween.AwaitFinished(this);
    }

    private async Task SpinVortex(float duration, float speedMultiplier) {
        float elapsed = 0f;
        while (elapsed < duration && GodotObject.IsInstanceValid(this) && IsInsideTree()) {
            await this.AwaitProcessFrame();
            float delta = (float)GetProcessDeltaTime();
            elapsed += delta;
            float speed = Mathf.Lerp(2.2f, 8.0f, Mathf.Clamp(elapsed / duration, 0f, 1f)) * speedMultiplier;
            _vortexRed.Rotation += delta * speed;
            _vortexBlue.Rotation -= delta * speed * 1.18f;
            _vortexCore.Rotation += delta * speed * 0.62f;
        }
    }

    private void EmitFusionSparks(int count) {
        Texture2D texture = _burstFlash.Texture;
        CanvasItemMaterial addMaterial = _burstFlash.Material as CanvasItemMaterial ?? new CanvasItemMaterial {
            BlendMode = CanvasItemMaterial.BlendModeEnum.Add
        };

        for (int i = 0; i < count; i++) {
            Sprite2D spark = new() {
                Texture = texture,
                Material = addMaterial,
                Centered = true,
                ZIndex = 20,
                ZAsRelative = false,
                Rotation = (float)GD.RandRange(-Mathf.Pi, Mathf.Pi),
                Scale = Vector2.One * (float)GD.RandRange(0.05f, 0.16f),
                Modulate = new Color(i % 2 == 0 ? FusionRed : FusionBlue, (float)GD.RandRange(0.42f, 0.9f))
            };
            _particleLayer.AddChild(spark);

            float angle = (float)GD.RandRange(-Mathf.Pi, Mathf.Pi);
            float distance = (float)GD.RandRange(180f, 650f);
            Vector2 target = Vector2.FromAngle(angle) * distance;
            float duration = (float)GD.RandRange(0.28f, 0.52f);

            Tween tween = spark.CreateTween().SetParallel();
            tween.TweenProperty(spark, "position", target, duration)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
            tween.TweenProperty(spark, "scale", spark.Scale * (float)GD.RandRange(0.2f, 0.55f), duration)
                .SetEase(Tween.EaseType.In)
                .SetTrans(Tween.TransitionType.Quad);
            tween.TweenProperty(spark, "modulate:a", 0f, duration)
                .SetEase(Tween.EaseType.In)
                .SetTrans(Tween.TransitionType.Quad);
            tween.Finished += () => {
                if (GodotObject.IsInstanceValid(spark)) {
                    spark.QueueFree();
                }
            };
        }
    }
}
