using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using VYgo.Scripts;

namespace VYgo.Core.Effects;

[GlobalClass]
public partial class NLinkSummonManager: Node3D {
	private List<AnimationPlayer> linkTrailPlayers = new();

	public override void _Ready() {
		GD.Print("NLinkSummonManager: Ready");
		base._Ready();
		for (var i = 1; i <= 8; i++) {
			var node = GetNode<Node3D>((NodePath) "%LinkTrail" + i);
			if (node == null) {
				Entry.Logger.Warn("CannotFindLinkTrail:" + i);
				return;
			}

			// 关键：让每个 LinkTrail 实例的材质独立，避免动画互相影响
			var meshInstance = node.GetNode<MeshInstance3D>("MeshInstance3D");
			if (meshInstance != null && meshInstance.MaterialOverride != null) {
				meshInstance.MaterialOverride = (Material)meshInstance.MaterialOverride.Duplicate();
			}

			var animationPlayer = node.GetNode<AnimationPlayer>("AnimationPlayer");
			if (animationPlayer == null) {
				GD.Print("CannotFindLinkTrailAnimationPlayer:" + i);
				return;
			}
			linkTrailPlayers.Add(animationPlayer);
		}

		// TaskHelper.RunSafely(play());
	}

	public void PlayLink() {
		TaskHelper.RunSafely(play());
	}
	public async Task play() {
		foreach (var player in linkTrailPlayers) {
			if (player.GetParent() is not Node3D pNode) continue;
			pNode.SetVisible(true);
			player.Play("link_trail");
			await Cmd.Wait(1f);
			pNode.SetVisible(false);
			pNode.QueueFreeSafely();
		}
	}
}
