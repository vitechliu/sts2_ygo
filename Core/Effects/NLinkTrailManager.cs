using Godot;

namespace VYgo.Core.Effects;

public partial class NLinkTrailManager: Node3D {
	private AnimationPlayer _animationPlayer;

	public NLinkSummonManager? _parent = null!;

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

	public void hit() {
		if (_parent != null) {
			_parent.onHit();
		}
	}
}
