using Godot;
using VYgo.Scripts;
using VYgo.Utils;

namespace VYgo.Core.Effects;

public partial class NLinkSummonManager: Node3D {
	private Dictionary<int, NLinkTrailManager> LinkTrails = new();

	private Sprite3D _shineSquare;

	private Tween? _shineTween;

	private AnimationPlayer _mainAnimationPlayer;

	private AnimationPlayer _lineAnimationPlayer;
	

	[Export] public AudioStreamOggVorbis _postSound1;
	[Export] public AudioStreamOggVorbis _postSound2;

	public override void _Ready() {
		base._Ready();
		_shineSquare = GetNode<Sprite3D>("%GateSquare2");
		_mainAnimationPlayer = GetNode<AnimationPlayer>("%MainAnim");
		_lineAnimationPlayer = GetNode<AnimationPlayer>("PostLinkFX/LineEffect/LineEffectPlayer");
		for (var i = 1; i <= 8; i++) {
			var node = GetNode<NLinkTrailManager>((NodePath) "%LinkTrail" + i);
			if (node == null) {
				Entry.Logger.Warn("CannotFindLinkTrail:" + i);
				return;
			}
			node._parent = this;
			LinkTrails.Add(i, node);
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
	
	public void onHit() {
		ShineSquare(1f);
	}

	public void PlaySfx(string path) {
		SFXUtil.Play(path);
	}

	public async Task PlayAnimMain() {
		await _mainAnimationPlayer.PlayAsync("main");
	}

	public async Task PlayLinks(List<int> links) {
		List<Task> anim = new();
		SFXUtil.Play("event:/vygo/sfx/link_summon_02");
		SFXUtil.PlayAfter("event:/vygo/sfx/link_summon_03", .5f);
		foreach (var linkIndex in links) {
			var trailManager = LinkTrails[linkIndex];
			trailManager.Visible = true;
			anim.Add(trailManager.PlayAsync());
		}
		await Task.WhenAll(anim);
	}
	
	
	public void PlayPostEffect() {
		_lineAnimationPlayer.Play("line");
	}
}
