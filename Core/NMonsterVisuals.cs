using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using VYgo.Utils;

namespace VYgo.Core;

[ScriptPath("res://Core/NMonsterVisuals.cs")]
public partial class NMonsterVisuals: NCreatureVisuals {
	private const float MaterialVfxDuration = 1.3f;
	private const float MaterialVfxCleanupDelay = 0.8f;
	private const float MaterialCompressDuration = 0.15f;
	private const float MaterialFlyDuration = 0.20f;

	private const string MaterialShaderCode = """
		shader_type canvas_item;

		uniform float whiteness : hint_range(0.0, 1.0) = 0.0;

		void fragment() {
			vec4 texture_color = texture(TEXTURE, UV);
			vec3 white_silhouette = mix(texture_color.rgb, vec3(1.0), whiteness);
			COLOR = vec4(white_silhouette, texture_color.a) * COLOR;
		}
		""";

	protected virtual void OnSummon() {}

	protected Sprite2D mainSprite;
	public override void _Ready() {
		base._Ready();
		mainSprite = GetNode<Sprite2D>("./Visuals/Image");
	}

	public const string MATERIAL_VFX_PATH = "res://VYgo/scenes/vfx/summon/vfx_link_summon_material.tscn";

	public async Task PlayMaterialVfx() {
		var node = VFXUtil.PlaySimple(
			MATERIAL_VFX_PATH,
			VfxSpawnPosition.GlobalPosition,
			MaterialVfxDuration + MaterialVfxCleanupDelay
		);
		if (node is null) return;

		await VFXUtil.Wait(MaterialVfxDuration, ignoreCombatEnd: true);
		if (!GodotObject.IsInstanceValid(node)) return;

		foreach (var child in node.GetChildren()) {
			if (child is GpuParticles2D particles) {
				particles.Emitting = false;
			}
		}
	}

	public async Task PlayMaterialExitAnimation() {
		if (!GodotObject.IsInstanceValid(mainSprite)) return;

		var shader = new Shader {
			Code = MaterialShaderCode
		};
		var material = new ShaderMaterial {
			Shader = shader
		};
		material.SetShaderParameter("whiteness", 0f);
		mainSprite.Material = material;

		var originalScale = mainSprite.Scale;
		var compressedScale = new Vector2(originalScale.X * 0.035f, originalScale.Y * 1.12f);
		var compressTween = mainSprite.CreateTween().SetParallel();
		compressTween.TweenProperty(mainSprite, "scale", compressedScale, MaterialCompressDuration)
			.SetTrans(Tween.TransitionType.Expo)
			.SetEase(Tween.EaseType.In);
		compressTween.TweenMethod(
				Callable.From<float>(value => material.SetShaderParameter("whiteness", value)),
				0f,
				1f,
				MaterialCompressDuration * 0.75f
			)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.In);
		await mainSprite.ToSignal(compressTween, Tween.SignalName.Finished);

		if (!GodotObject.IsInstanceValid(mainSprite)) return;

		var viewportHeight = mainSprite.GetViewportRect().Size.Y;
		var targetPosition = new Vector2(
			mainSprite.GlobalPosition.X,
			-Mathf.Max(160f, viewportHeight * 0.15f)
		);
		var flyTween = mainSprite.CreateTween().SetParallel();
		flyTween.TweenProperty(mainSprite, "global_position", targetPosition, MaterialFlyDuration)
			.SetTrans(Tween.TransitionType.Expo)
			.SetEase(Tween.EaseType.In);
		flyTween.TweenProperty(
				mainSprite,
				"scale",
				new Vector2(compressedScale.X, compressedScale.Y * 1.9f),
				MaterialFlyDuration
			)
			.SetTrans(Tween.TransitionType.Expo)
			.SetEase(Tween.EaseType.In);
		flyTween.TweenProperty(mainSprite, "modulate:a", 0f, MaterialFlyDuration * 0.35f)
			.SetDelay(MaterialFlyDuration * 0.65f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.In);
		await mainSprite.ToSignal(flyTween, Tween.SignalName.Finished);
	}
}
