using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using VYgo.Scripts;

namespace VYgo.Core.Effects;

public partial class NLinkSummonManager: Node3D {
	private List<NLinkTrailManager> linkTrails = new();

	private Sprite3D _shineSquare;

	private Tween? _shineTween;

	private AnimationPlayer _lineAnimationPlayer;
	
	private AudioStreamPlayer _audioStreamPlayer;

	[Export] public AudioStreamOggVorbis _postSound1;
	[Export] public AudioStreamOggVorbis _postSound2;

	public override void _Ready() {
		base._Ready();
		_audioStreamPlayer = GetNode<AudioStreamPlayer>("%L1Audio");
		_shineSquare = GetNode<Sprite3D>("%GateSquare2");
		_lineAnimationPlayer = GetNode<AnimationPlayer>("PostLinkFX/LineEffect/LineEffectPlayer");
		for (var i = 1; i <= 8; i++) {
			var node = GetNode<NLinkTrailManager>((NodePath) "%LinkTrail" + i);
			if (node == null) {
				Entry.Logger.Warn("CannotFindLinkTrail:" + i);
				return;
			}
			node._parent = this;
			linkTrails.Add(node);
		}
	}

	public void ShineSquare(float time) {
		_shineSquare.Modulate = _shineSquare.Modulate with { A = 1f };
		if (_shineTween != null) {_shineTween.Kill();}
		_shineTween = CreateTween();
		_shineTween.TweenProperty(_shineSquare, "modulate:a", 0f, time)
			.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
		// GD.Print("ShineSquare");
	}
	public void PlayLink() {
		TaskHelper.RunSafely(play());
	}
	public void onHit() {
		ShineSquare(1f);
	}
	async Task play() {
		foreach (var trails in linkTrails) {
			trails.SetVisible(true);
			trails.play();
			await Cmd.Wait(.3f);
			// trails.SetVisible(false);
			// trails.QueueFreeSafely();
		}
		await Cmd.Wait(.6f);
		PlayPostEffect();
	}

	public void PlayPostEffect() {
		_lineAnimationPlayer.Play("line");
	}
}
