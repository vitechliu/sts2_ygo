using System.Threading;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace VYgo.Core.Effects;

[ScriptPath("res://Core/Effects/NCardFlySummonVfx.cs")]
public partial class NCardFlySummonVfx : Node2D {
    private const float Duration = 0.45f;
    private const float ArcHeight = 220f;
    private const float ScaleOutStart = 0.75f;

    private readonly CancellationTokenSource _cancelToken = new();

    private NCardTrailVfx? _trailVfx;
    private Tween? _scaleTween;
    private Vector2 _startPosition;
    private Vector2 _targetPosition;

    public NCard CardNode { get; private set; } = null!;

    public static NCardFlySummonVfx Create(NCard cardNode, Vector2 targetPosition) {
        var vfx = new NCardFlySummonVfx {
            CardNode = cardNode,
            _startPosition = cardNode.GlobalPosition,
            _targetPosition = targetPosition
        };
        return vfx;
    }

    public override void _Ready() {
        base.GlobalPosition = Vector2.Zero;

        _trailVfx = NCardTrailVfx.Create(CardNode, CardNode.Model.Owner.Character.TrailPath);
        if (_trailVfx != null) {
            this.AddChildSafely(_trailVfx);
        }
    }

    public override void _ExitTree() {
        _scaleTween?.Kill();
        _cancelToken.Cancel();
        base._ExitTree();
    }

    public float GetDuration() {
        return Duration + 0.05f;
    }

    public async Task PlayAnim() {
        SfxCmd.Play("event:/sfx/ui/cards/card_movement_B_power");

        _scaleTween = CardNode.CreateTween();
        _scaleTween.TweenProperty(CardNode, "scale", Vector2.One * 0.1f, Duration);

        var controlPoint = (_startPosition + _targetPosition) * 0.5f + Vector2.Up * ArcHeight;
        var time = 0f;
        var scalingOut = false;

        while (time < Duration) {
            await this.AwaitProcessFrame();
            if (_cancelToken.IsCancellationRequested) break;

            time += (float)GetProcessDeltaTime();
            var progress = Mathf.Clamp(time / Duration, 0f, 1f);
            var easedProgress = Ease.QuadIn(progress);

            var current = MathHelper.BezierCurve(_startPosition, _targetPosition, controlPoint, easedProgress);
            var next = MathHelper.BezierCurve(_startPosition, _targetPosition, controlPoint, Mathf.Min(easedProgress + 0.03f, 1f));
            CardNode.GlobalPosition = current;
            CardNode.Rotation = Mathf.LerpAngle(CardNode.Rotation, (next - current).Angle() + Mathf.Pi / 2f, 0.25f);

            if (!scalingOut && progress >= ScaleOutStart) {
                scalingOut = true;
                _scaleTween?.Kill();
                _scaleTween = CardNode.CreateTween();
                _scaleTween.TweenProperty(CardNode, "scale", Vector2.Zero, Math.Max(Duration - time, 0.05f));
            }
        }

        CardNode.GlobalPosition = _targetPosition;
        // NGame.Instance.ScreenShake(ShakeStrength.Medium, ShakeDuration.Short);

        if (_trailVfx != null) {
            await _trailVfx.FadeOut();
        }

        CardNode.QueueFreeSafely();
        this.QueueFreeSafely();
    }
}
