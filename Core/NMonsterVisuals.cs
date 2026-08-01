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
	private const float IntentTextureSize = 144f;

	public static readonly Vector2 BaseIntentScale = new Vector2(-1f, 1f) * 0.35f;

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
			// Entry.Logger.Info("Play NMonsterSummon VFX: " + VfxSpawnPosition.GlobalPosition);
			VFXUtil.ReplayAllParticles(node);
		}
	}
	
	private Sprite2D _mainSprite = null!;
	private Sprite2D? actionReadyIcon;
	private Tween? actionReadyTween;
	private string? actionReadyIconPath;

	public override void _Ready() {
		base._Ready();
		_mainSprite = GetNode<Sprite2D>("./Visuals/Image");
		CreateActionReadyIcon();
	}

	public override void _ExitTree() {
		actionReadyTween?.Kill();
		actionReadyTween = null;
		base._ExitTree();
	}

	public void SetActionReadyIndicator(string? iconPath) {
		if (actionReadyIcon == null)
			CreateActionReadyIcon();
		if (actionReadyIcon == null) return;

		bool visible = !string.IsNullOrEmpty(iconPath);
		bool iconChanged = visible && actionReadyIconPath != iconPath;
		if (iconChanged) {
			actionReadyIcon.Texture = ResourceLoader.Load<Texture2D>(iconPath);
			actionReadyIconPath = iconPath;
		}

		if (actionReadyIcon.Visible == visible && !iconChanged) return;

		actionReadyIcon.Visible = visible;
		actionReadyTween?.Kill();
		actionReadyTween = null;

		if (!visible) return;

		Vector2 baseScale = GetIntentBaseScale(actionReadyIcon.Texture);
		actionReadyIcon.Modulate = Colors.White;
		actionReadyIcon.Scale = baseScale;
		actionReadyTween = actionReadyIcon.CreateTween().SetLoops();
		actionReadyTween.TweenProperty(actionReadyIcon, "scale", baseScale * 1.2f, 0.45f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		actionReadyTween.TweenProperty(actionReadyIcon, "scale", baseScale, 0.45f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
	}

	private static Vector2 GetIntentBaseScale(Texture2D? texture) {
		if (texture == null) return BaseIntentScale;

		Vector2 textureSize = texture.GetSize();
		if (Mathf.IsZeroApprox(textureSize.X) || Mathf.IsZeroApprox(textureSize.Y))
			return BaseIntentScale;

		return BaseIntentScale * new Vector2(
			IntentTextureSize / textureSize.X,
			IntentTextureSize / textureSize.Y
		);
	}

	private void CreateActionReadyIcon() {
		if (actionReadyIcon != null || IntentPosition == null) return;

		actionReadyIcon = new Sprite2D {
			Name = "ActionReadyIcon",
			Centered = true,
			Visible = false,
			ZIndex = 0,
			Scale = BaseIntentScale
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
		if (!GodotObject.IsInstanceValid(_mainSprite)) return;

		var shader = new Shader {
			Code = MaterialShaderCode
		};
		var material = new ShaderMaterial {
			Shader = shader
		};
		material.SetShaderParameter("whiteness", 0f);
		_mainSprite.Material = material;

		var originalScale = _mainSprite.Scale;
		var compressedScale = new Vector2(originalScale.X * 0.035f, originalScale.Y * 1.12f);
		var compressTween = _mainSprite.CreateTween().SetParallel();
		compressTween.TweenProperty(_mainSprite, "scale", compressedScale, MaterialCompressDuration)
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
		await _mainSprite.ToSignal(compressTween, Tween.SignalName.Finished);

		if (!GodotObject.IsInstanceValid(_mainSprite)) return;

		SFXUtil.Play("event:/vygo/sfx/material_01");
		CustomOriginalVFX.PlayLinkSummon(VfxSpawnPosition.GlobalPosition);
		var viewportHeight = _mainSprite.GetViewportRect().Size.Y;
		var targetPosition = new Vector2(
			_mainSprite.GlobalPosition.X,
			-Mathf.Max(160f, viewportHeight * 0.15f)
		);
		var flyTween = _mainSprite.CreateTween().SetParallel();
		flyTween.TweenProperty(_mainSprite, "global_position", targetPosition, MaterialFlyDuration)
			.SetTrans(Tween.TransitionType.Expo)
			.SetEase(Tween.EaseType.In);
		flyTween.TweenProperty(
				_mainSprite,
				"scale",
				new Vector2(compressedScale.X, compressedScale.Y * 1.9f),
				MaterialFlyDuration
			)
			.SetTrans(Tween.TransitionType.Expo)
			.SetEase(Tween.EaseType.In);
		flyTween.TweenProperty(_mainSprite, "modulate:a", 0f, MaterialFlyDuration * 0.35f)
			.SetDelay(MaterialFlyDuration * 0.65f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.In);
		await _mainSprite.ToSignal(flyTween, Tween.SignalName.Finished);
	}
}
