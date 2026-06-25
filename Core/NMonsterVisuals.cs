using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace VYgo.Core;

[ScriptPath("res://Core/NMonsterVisuals.cs")]
public partial class NMonsterVisuals: NCreatureVisuals {
	protected virtual void OnSummon() {}

	protected Sprite2D mainSprite;
	public override void _Ready() {
		base._Ready();
		mainSprite = GetNode<Sprite2D>("Monster/Visuals/Image");
	}
}
