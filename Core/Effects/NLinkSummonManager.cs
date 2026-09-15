using Godot;
using VYgo.Scripts;
using VYgo.Utils;

namespace VYgo.Core.Effects;

public partial class NLinkSummonManager: Node3D {
	private Dictionary<int, NLinkTrailManager> LinkTrails = new();

	private Sprite3D _shineSquare = null!;

	private Tween? _shineTween;

	private AnimationPlayer _mainAnimationPlayer = null!;

	private AnimationPlayer _lineAnimationPlayer = null!;

	public override void _Ready() {
		base._Ready();
		_shineSquare = GetNode<Sprite3D>("%GateSquare2");
		_shineSquare.Position += Vector3.Up * 0.2f;
		_shineSquare.MaterialOverride = new ShaderMaterial {
			Shader = GD.Load<Shader>("res://VYgo/scenes/summon/link/link_gate_flash.gdshader")
		};
		((ShaderMaterial)_shineSquare.MaterialOverride).SetShaderParameter("main_tex",_shineSquare.Texture);
		_mainAnimationPlayer = GetNode<AnimationPlayer>("%MainAnim");
		_lineAnimationPlayer = GetNode<AnimationPlayer>("PostLinkFX/LineEffect/LineEffectPlayer");
		var environment = new Godot.Environment {
			BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color(0,0,0,0),
			GlowEnabled = true, GlowIntensity = 0.7f, GlowStrength = 0.9f, GlowBloom = 0,
			GlowHdrThreshold = 1f, GlowBlendMode = Godot.Environment.GlowBlendModeEnum.Additive
		};
		environment.SetGlowLevel(0,0.8f); environment.SetGlowLevel(1,0.4f);
		AddChild(new WorldEnvironment { Environment = environment });
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

	public void ShineFinal() {
		if (_shineTween != null) {_shineTween.Kill();}
		_shineTween = CreateTween();
		_shineTween.TweenProperty(_shineSquare, "modulate:a", 1f, 0.2f)
			.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
	}

	public void StartMain() {
		Visible = true;
		_mainAnimationPlayer.Play("main");
	}
	
	public void onHit() {
		ShineSquare(0.5f);
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
