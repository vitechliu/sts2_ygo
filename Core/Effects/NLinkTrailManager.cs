using Godot;
using VYgo.Utils;

namespace VYgo.Core.Effects;

public partial class NLinkTrailManager: Node3D {
	private AnimationPlayer _animationPlayer;

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
	}

	public void play() {
		_animationPlayer.Play("link_trail");
	}

	public async Task PlayAsync() {
		CreateTween().TweenCallback(Callable.From(QueueFree)).SetDelay(1.6f);
		_animationPlayer.Play("link_trail");
		await _animationPlayer.ToSignal(this, SignalName.MidpointReached);
	}
	
	
	
	public void PlaySfx(string path) {
		SFXUtil.Play(path);
	}

	public void hit() {
		if (_parent != null) {
			_parent.onHit();
		}
	}
}
