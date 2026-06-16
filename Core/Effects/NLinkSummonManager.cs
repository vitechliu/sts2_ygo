using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using VYgo.Scripts;

namespace VYgo.Core.Effects;

public partial class NLinkSummonManager: Node3D {
	private List<NLinkTrailManager> linkTrails = new();

	public override void _Ready() {
		base._Ready();
		for (var i = 1; i <= 8; i++) {
			var node = GetNode<NLinkTrailManager>((NodePath) "%LinkTrail" + i);
			if (node == null) {
				Entry.Logger.Warn("CannotFindLinkTrail:" + i);
				return;
			}

			linkTrails.Add(node);
		}
	}

	public void PlayLink() {
		TaskHelper.RunSafely(play());
	}
	public async Task play() {
		foreach (var trails in linkTrails) {
			trails.SetVisible(true);
			trails.play();
			await Cmd.Wait(1f);
			trails.SetVisible(false);
			trails.QueueFreeSafely();
		}
	}
}
