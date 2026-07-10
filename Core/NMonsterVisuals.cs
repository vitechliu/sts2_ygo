using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using VYgo.Scripts;
using VYgo.Utils;

namespace VYgo.Core;

[ScriptPath("res://Core/NMonsterVisuals.cs")]
public partial class NMonsterVisuals: NCreatureVisuals {
	private const float MaterialVfxDuration = 1.3f;
	private const float MaterialVfxCleanupDelay = 3f;
	private const float MaterialCompressDuration = 0.15f;
	private const float MaterialFlyDuration = 0.20f;
	private const string ActionReadyIconPath = "res://VYgo/images/intents/intent_attack_2.png";

	private const string MaterialShaderCode = """
		shader_type canvas_item;

		uniform float whiteness : hint_range(0.0, 1.0) = 0.0;

		void fragment() {
			vec4 texture_color = texture(TEXTURE, UV);
			vec3 white_silhouette = mix(texture_color.rgb, vec3(1.0), whiteness);
			COLOR = vec4(white_silhouette, texture_color.a) * COLOR;
		}
		""";

	public virtual void OnSummon() {
		PlaySummonVfx();
		//下一帧
		// ExecuteOnNextFrame();
	}

	// protected async void ExecuteOnNextFrame() {
	// 	// 1. 等待 SceneTree 触发 process_frame 信号（即下一帧）
	// 	await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	// 	// 2. 这里写你要在下一帧执行的代码
	// 	PlaySummonVfx();
	// }

	protected void PlaySummonVfx() {
		var node = VFXUtil.PlaySimple(SUMMON_VFX_PATH, VfxSpawnPosition.GlobalPosition, 3);
		if (node != null) {
			Entry.Logger.Info("Play NMonsterSummon VFX: " + VfxSpawnPosition.GlobalPosition);
			VFXUtil.ReplayAllParticles(node);
		}
	}
	
	protected Sprite2D mainSprite;
	private Sprite2D? actionReadyIcon;
	private Tween? actionReadyTween;

	public override void _Ready() {
		base._Ready();
		mainSprite = GetNode<Sprite2D>("./Visuals/Image");
		CreateActionReadyIcon();
	}

	public void SetActionReadyIndicatorVisible(bool visible) {
		if (actionReadyIcon == null)
			CreateActionReadyIcon();
		if (actionReadyIcon == null) return;

		if (actionReadyIcon.Visible == visible) return;

		actionReadyIcon.Visible = visible;
		actionReadyTween?.Kill();
		actionReadyTween = null;

		if (!visible) return;

		actionReadyIcon.Modulate = Colors.White;
		actionReadyIcon.Scale = Vector2.One * 0.28f;
		actionReadyTween = actionReadyIcon.CreateTween().SetLoops();
		actionReadyTween.TweenProperty(actionReadyIcon, "scale", Vector2.One * 0.34f, 0.45f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		actionReadyTween.TweenProperty(actionReadyIcon, "scale", Vector2.One * 0.28f, 0.45f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
	}

	private void CreateActionReadyIcon() {
		if (actionReadyIcon != null || IntentPosition == null) return;

		actionReadyIcon = new Sprite2D {
			Name = "ActionReadyIcon",
			Texture = ResourceLoader.Load<Texture2D>(ActionReadyIconPath),
			Centered = true,
			Visible = false,
			ZIndex = 1,
			Scale = Vector2.One * 0.28f
		};
		IntentPosition.AddChild(actionReadyIcon);
	}

	public const string MATERIAL_VFX_PATH = "res://VYgo/scenes/vfx/summon/vfx_link_summon_material.tscn";
	public const string SUMMON_VFX_PATH = "res://VYgo/scenes/vfx/summon/vfx_summon_1.tscn";
	
	public async Task PlayMaterialVfx() {
		float totalLifeTime = MaterialVfxDuration + (float)GD.RandRange(0.1f, 1f);
		var node = VFXUtil.PlaySimple(
			MATERIAL_VFX_PATH,
			VfxSpawnPosition.GlobalPosition,
			totalLifeTime + MaterialVfxCleanupDelay
		);
		if (node is null) return;

		await VFXUtil.Wait(totalLifeTime, ignoreCombatEnd: true);
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

		SFXUtil.Play("event:/vygo/sfx/material_01");
		CustomOriginalVFX.PlayLinkSummon(VfxSpawnPosition.GlobalPosition);
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
