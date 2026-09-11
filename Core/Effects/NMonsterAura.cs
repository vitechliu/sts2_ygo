using Godot;

namespace VYgo.Core.Effects;

public enum MonsterAuraState { Inactive, NoAction, Available, SelectingTarget, Exhausted, Unavailable }

/// <summary>每只怪兽独立持有的常驻光环；仅改变显示，不参与行动结算。</summary>
[ScriptPath("res://Core/Effects/NMonsterAura.cs")]
public partial class NMonsterAura : Node2D {
    public const string ScenePath = "res://VYgo/scenes/vfx/monster_aura.tscn";

    private ShaderMaterial? _material;
    private MonsterAuraState _state = MonsterAuraState.NoAction;
    [Export] public Color RestColor { get; set; } = new(0.66f, 0.7f, 0.68f);
    [Export] public Color ActiveColor { get; set; } = new(0.60f, 0.95f, 0.72f);
    [Export] public float TransitionSeconds { get; set; } = 1.2f;
    // 宽度表示主环横向直径，统一缩放两个轴，椭圆比例始终为 4:1。
    [Export] public float AuraWidth { get; set; } = 192f;
    [Export] public float SizeMultiplier { get; set; } = 1.1f;
    [Export] public float HoverAmplitude { get; set; } = 0.035f;
    [Export] public float HoverPeriodSeconds { get; set; } = 1.4f;

    private float _brightness = 0.1f;
    private Color _color;
    private float _rotationSpeed = 0.2f;
    private float _phase;
    private float _pulse;
    private Vector2 _fullScale;
    private Tween? _lifecycleTween;
    private bool _departing;
    private Control _ring = null!;
    private bool _hovered;
    private float _hoverBlend;
    private float _hoverPhase;

    public override void _Ready() {
        // 显式复制材质，避免缓存场景的不同实例共享亮度、旋转和闪光参数。
        ColorRect ring = GetNode<ColorRect>("Ring");
        _ring = ring;
        _ring.PivotOffset = _ring.Size * 0.5f;
        _material = (ShaderMaterial)ring.Material.Duplicate();
        ring.Material = _material;
        _phase = (GetInstanceId() % 6283UL) * 0.001f;
        _color = RestColor;
        _fullScale = Vector2.One * (AuraWidth * SizeMultiplier / (256f * 0.72f));
        Scale = _fullScale;
    }

    public void SetState(MonsterAuraState state) {
        if (_departing) return;
        _state = state;
        Visible = state != MonsterAuraState.Inactive;
        if (!Visible) _pulse = 0f;
    }

    public void ResetPresentation() {
        _lifecycleTween?.Kill();
        _lifecycleTween = null;
        _departing = false;
        Scale = _fullScale;
        Modulate = Colors.White;
        Visible = _state != MonsterAuraState.Inactive;
        _pulse = 0f;
    }

    public void PlaySummonFeedback() {
        ResetPresentation();
        Visible = true;
        Scale = _fullScale * 0.01f;
        Modulate = new Color(1f, 1f, 1f, 0f);
        _pulse = 1f;
        _lifecycleTween = CreateTween().SetParallel();
        _lifecycleTween.TweenProperty(this, "scale", _fullScale, 0.8f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        _lifecycleTween.TweenProperty(this, "modulate:a", 1f, 0.65f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
    }

    public void SetHovered(bool hovered) => _hovered = !_departing && hovered;

    public void PlayDeathFeedback(bool freeWhenFinished = false) {
        if (_departing) return;
        _departing = true;
        _pulse = 0f;
        _hovered = false;
        _lifecycleTween?.Kill();
        _lifecycleTween = CreateTween();
        // 先保持大小并变淡，再向中心收缩；收缩时仍保留少量可见度，末尾完全消失。
        _lifecycleTween.TweenProperty(this, "modulate:a", Mathf.Min(Modulate.A, 0.25f), 0.65f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        _lifecycleTween.TweenProperty(this, "scale", Scale * 0.01f, 0.5f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        _lifecycleTween.Parallel().TweenProperty(this, "modulate:a", 0f, 0.5f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
        _lifecycleTween.TweenCallback(Callable.From(() => {
            Visible = false;
            if (freeWhenFinished) QueueFree();
        }));
    }

    public override void _ExitTree() {
        _lifecycleTween?.Kill();
        _lifecycleTween = null;
    }

    public void PlayActionFeedback() => _pulse = Mathf.Max(_pulse, 0.55f);

    public override void _Process(double delta) {
        if (_material == null || !Visible) return;
        // 悬停只缩放绘制子节点，生命周期只缩放根节点，两者不会争抢同一个属性。
        _hoverBlend = Mathf.Lerp(_hoverBlend, _hovered ? 1f : 0f, 1f - Mathf.Exp(-(float)delta * 9f));
        _hoverPhase = Mathf.PosMod(_hoverPhase + (float)delta * Mathf.Tau / Mathf.Max(0.2f, HoverPeriodSeconds), Mathf.Tau);
        _ring.Scale = Vector2.One * (1f + Mathf.Sin(_hoverPhase) * HoverAmplitude * _hoverBlend);
        bool available = _state is MonsterAuraState.Available or MonsterAuraState.SelectingTarget;
        float targetBrightness = _state switch {
            MonsterAuraState.SelectingTarget => 1.15f,
            MonsterAuraState.Available => 0.85f,
            _ => 0.1f
        };
        float step = 1f - Mathf.Exp(-(float)delta * 3f / Mathf.Max(0.1f, TransitionSeconds));
        _brightness = Mathf.Lerp(_brightness, targetBrightness, step);
        _color = _color.Lerp(available ? ActiveColor : RestColor, step);
        _rotationSpeed = Mathf.Lerp(_rotationSpeed, available ? 0.65f : 0.2f, step);
        _phase = Mathf.PosMod(_phase + _rotationSpeed * (float)delta, Mathf.Tau);
        _pulse = Mathf.MoveToward(_pulse, 0f, (float)delta * 1.6f);
        _material.SetShaderParameter("phase", _phase);
        _material.SetShaderParameter("brightness", _brightness);
        _material.SetShaderParameter("pulse", _pulse);
        _material.SetShaderParameter("aura_color", _color);
    }
}
