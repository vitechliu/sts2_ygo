using Godot;
using VYgo.Utils;

namespace VYgo.Core.Effects;

public partial class NLinkTrailManager: Node3D {
	private AnimationPlayer _animationPlayer = null!;

	public NLinkSummonManager? _parent = null!;

	[Signal]
	public delegate void MidpointReachedEventHandler();
	
	// 动画播放到你设定的那一帧时，会自动触发这个方法
	private void _on_animation_reached_midpoint()
	{
		// 发射信号，通知外面的 await
		EmitSignal(SignalName.MidpointReached); 
	}
	
	public override void _Ready() {
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		var meshInstance = GetNode<MeshInstance3D>("MeshInstance3D");
		if (meshInstance != null && meshInstance.MaterialOverride != null) {
			meshInstance.MaterialOverride = (Material)meshInstance.MaterialOverride.Duplicate();
		}
		// 原粒子移植的轴和枢轴不匹配，改用同一 XZ 平面、尖端锚定的长芒。
		GetNode<Node3D>("Flare1").Visible = false;
		GetNode<Node3D>("Flare2").Visible = false;
	}

	public void play() {
		_animationPlayer.Play("link_trail");
	}

	public async Task PlayAsync() {
		_animationPlayer.Play("link_trail");
		await _animationPlayer.ToSignal(this, SignalName.MidpointReached);
	}
	
	
	
	public void PlaySfx(string path) {
		SFXUtil.Play(path);
	}

	public void hit() {
		EmitImpactRays();
		if (_parent != null) {
			_parent.onHit();
		}
	}

	private void EmitImpactRays() {
		var texture = GD.Load<Texture2D>("res://VYgo/scenes/summon/link/assets/flare007.png");
		var shader = GD.Load<Shader>("res://VYgo/scenes/summon/link/link_impact.gdshader");
		var burst = new Node3D(); AddChild(burst);
		// 每个连接方向各有独立的命中点，方向来自场景变换，不能固定为左、下。
		Vector3 origin = GetNode<Sprite3D>("Marker").GlobalPosition + Vector3.Up * 8f;
		Vector3 outward = new Vector3(origin.X,0,origin.Z).Normalized();
		float angle = Mathf.Atan2(outward.X,outward.Z);
		var random = new RandomNumberGenerator { Seed = (ulong)(Mathf.Abs(origin.X * 97 + origin.Z * 61) + 37) };
		for (int i=0;i<8;i++) {
			float rayAngle = angle + Mathf.DegToRad(-100 + i * (200f/7) + random.RandfRange(-12,12));
			Vector3 direction = new(Mathf.Sin(rayAngle),0,Mathf.Cos(rayAngle));
			float length = random.RandfRange(180,320);
			float width = i%2==0 ? random.RandfRange(45,90) : random.RandfRange(6,20);
			var material = new ShaderMaterial { Shader=shader };
			material.SetShaderParameter("main_tex",texture);
			material.SetShaderParameter("gain",random.RandfRange(1.1f,1.8f));
			material.SetShaderParameter("opacity",0f);
			var ray = new MeshInstance3D { Mesh=new QuadMesh { Size=new Vector2(width,length) },
				MaterialOverride=material, CastShadow=GeometryInstance3D.ShadowCastingSetting.Off };
			burst.AddChild(ray);
			// 纹理下端是尖点，上端展开；正面朝相机，不写深度以免截断邻接光芒。
			ray.GlobalBasis = new Basis(direction.Cross(Vector3.Up),direction,Vector3.Up);
			ray.GlobalPosition = origin + direction * (length*0.5f);
			var tween = ray.CreateTween();
			float delay = random.RandfRange(0,0.045f);
			tween.TweenMethod(Callable.From<float>(a=>material.SetShaderParameter("opacity",a)),0f,1f,0.02f).SetDelay(delay);
			tween.TweenMethod(Callable.From<float>(a=>material.SetShaderParameter("opacity",a)),1f,0f,0.48f-delay)
				.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
		}
		burst.CreateTween().TweenCallback(Callable.From(burst.QueueFree)).SetDelay(0.52f);
	}
}
