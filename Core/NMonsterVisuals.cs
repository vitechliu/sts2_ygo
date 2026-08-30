using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using VYgo.Scripts;
using VYgo.Scripts.Actions;
using VYgo.Utils;

namespace VYgo.Core;

[ScriptPath("res://Core/NMonsterVisuals.cs")]
public partial class NMonsterVisuals: NCreatureVisuals {
	private const float MaterialVfxDuration = 1.3f;
	private const float MaterialVfxCleanupDelay = 3f;
	private const float MaterialCompressDuration = 0.15f;
	private const float MaterialFlyDuration = 0.20f;
	private const float QuickMaterialVfxDuration = 0.45f;
	private const float QuickMaterialCompressDuration = 0.12f;
	private const float QuickMaterialFlyDuration = 0.16f;
	private const float ActionIntentSize = 64f;
	private const float ActionIntentViewportMargin = 40f;
	private const float ActionIntentHeadClearance = 32f;
	private const float ActionIntentBobDistance = 10f;
	private const float ActionIntentBobOffset = 8f;
	private const double ActionIntentRefreshInterval = 0.1;
	private const string NativeIntentFontPath = "res://themes/kreon_bold_glyph_space_one.tres";

	private const string MaterialShaderCode = """
		shader_type canvas_item;

		uniform float whiteness : hint_range(0.0, 1.0) = 0.0;
		uniform vec4 flash_color : source_color = vec4(1.0);

		void fragment() {
			vec4 texture_color = texture(TEXTURE, UV);
			vec3 white_silhouette = mix(texture_color.rgb, flash_color.rgb, whiteness);
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
	private Control? actionIntentOverlay;
	private Control? actionIntentRoot;
	private TextureRect? actionIntentIcon;
	private Label? actionIntentDamage;
	private Label? actionIntentRemainingUses;
	private Label? actionIntentAreaBadge;
	private Tween? actionIntentTween;
	private string? actionIntentIconPath;
	private float actionIntentBobPhase;
	private double actionIntentRefreshElapsed;
	private MonsterActionIntentState actionIntentState = MonsterActionIntentState.Hidden;

	public override void _Ready() {
		base._Ready();
		_mainSprite = GetNode<Sprite2D>("./Visuals/Image");
		actionIntentBobPhase = (float)(GetInstanceId() % 6283UL) * 0.001f;
		CreateActionIntentOverlay();
		if (GetParent() is NCreature creatureNode) {
			BasePerTurnMonsterAction.RefreshActionIntent(creatureNode.Entity);
		}
	}

	public override void _Process(double delta) {
		base._Process(delta);
		SyncActionIntentPosition();
		actionIntentRefreshElapsed += delta;
		if (actionIntentRefreshElapsed < ActionIntentRefreshInterval) return;

		actionIntentRefreshElapsed = 0;
		if (GetParent() is NCreature creatureNode) {
			BasePerTurnMonsterAction.RefreshActionIntent(creatureNode.Entity);
		}
	}

	public override void _ExitTree() {
		actionIntentTween?.Kill();
		actionIntentTween = null;
		if (GodotObject.IsInstanceValid(actionIntentOverlay)) {
			actionIntentOverlay!.QueueFree();
		}
		actionIntentOverlay = null;
		actionIntentRoot = null;
		base._ExitTree();
	}

	public void SetActionIntentState(MonsterActionIntentState state) {
		if (actionIntentRoot == null || actionIntentIcon == null) {
			CreateActionIntentOverlay();
		}
		if (actionIntentRoot == null || actionIntentIcon == null
			|| actionIntentDamage == null || actionIntentRemainingUses == null
			|| actionIntentAreaBadge == null) return;

		bool iconChanged = state.Visible && actionIntentIconPath != state.IconPath;
		if (iconChanged) {
			actionIntentIcon.Texture = ResourceLoader.Load<Texture2D>(state.IconPath);
			actionIntentIconPath = state.IconPath;
		}

		bool visible = state.Visible && actionIntentIcon.Texture != null;
		MonsterActionIntentState renderedState = state with { Visible = visible };
		if (renderedState == actionIntentState && !iconChanged) return;
		actionIntentState = renderedState;

		actionIntentDamage.Visible = visible && state.Damage is > 0;
		actionIntentDamage.Text = state.Damage?.ToString() ?? string.Empty;
		actionIntentRemainingUses.Visible = visible
			&& state.MaxUses > 1
			&& state.RemainingUses > 0;
		actionIntentRemainingUses.Text = state.RemainingUses.ToString();
		actionIntentAreaBadge.Visible = visible && state.IsAreaAttack;

		actionIntentTween?.Kill();
		actionIntentTween = null;
		actionIntentRoot.Visible = visible;
		if (GetParent() is NCreature creatureNode) {
			creatureNode.Hitbox.MouseDefaultCursorShape = visible
				? Control.CursorShape.PointingHand
				: Control.CursorShape.Arrow;
		}
		if (!visible) return;

		actionIntentRoot.Modulate = state.IsSelectingTarget
			? new Color(1f, 0.86f, 0.36f, 1f)
			: Colors.White;
		actionIntentRoot.Scale = state.IsSelectingTarget
			? Vector2.One * 1.12f
			: Vector2.One;
		if (state.IsSelectingTarget) return;

		actionIntentTween = actionIntentRoot.CreateTween().SetLoops();
		actionIntentTween.TweenProperty(actionIntentRoot, "scale", Vector2.One * 1.1f, 0.45f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		actionIntentTween.TweenProperty(actionIntentRoot, "scale", Vector2.One, 0.45f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
	}

	public void PlayActionIntentConfirmFeedback() {
		if (actionIntentOverlay == null || actionIntentRoot == null
			|| actionIntentIcon?.Texture == null || !actionIntentRoot.Visible) return;

		var flash = new TextureRect {
			Name = "ActionIntentConfirmFlash",
			Texture = actionIntentIcon.Texture,
			Position = actionIntentRoot.Position,
			Size = Vector2.One * ActionIntentSize,
			PivotOffset = Vector2.One * (ActionIntentSize * 0.5f),
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			ZIndex = 1
		};
		actionIntentOverlay.AddChild(flash);
		Tween flashTween = flash.CreateTween().SetParallel();
		flashTween.TweenProperty(flash, "scale", Vector2.One * 1.38f, 0.16f)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.Out);
		flashTween.TweenProperty(flash, "modulate:a", 0f, 0.16f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.In);
		flashTween.Finished += flash.QueueFree;
	}

	private void CreateActionIntentOverlay() {
		if (actionIntentOverlay != null || IntentPosition == null) return;
		NCombatRoom? combatRoom = NCombatRoom.Instance;
		if (combatRoom == null) return;

		// 作为战斗房间最后的子节点显示在血条上方，同时保持低于 Run/GlobalUi 的全屏覆盖层。
		actionIntentOverlay = new Control {
			Name = "MonsterActionIntentOverlay",
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		combatRoom.AddChild(actionIntentOverlay);
		actionIntentOverlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		actionIntentRoot = new Control {
			Name = "MonsterActionIntent",
			Visible = false,
			Size = Vector2.One * ActionIntentSize,
			PivotOffset = Vector2.One * (ActionIntentSize * 0.5f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		actionIntentOverlay.AddChild(actionIntentRoot);

		actionIntentIcon = new TextureRect {
			Name = "Icon",
			Position = new Vector2(-2f, -1f),
			Size = Vector2.One * ActionIntentSize,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
		};
		actionIntentRoot.AddChild(actionIntentIcon);

		actionIntentDamage = CreateActionIntentDamageLabel();
		actionIntentRoot.AddChild(actionIntentDamage);

		actionIntentRemainingUses = CreateActionIntentLabel(
			"RemainingUses",
			new Vector2(43f, 0f),
			new Vector2(21f, 23f),
			16
		);
		actionIntentRoot.AddChild(actionIntentRemainingUses);

		actionIntentAreaBadge = CreateActionIntentLabel(
			"AreaAttack",
			new Vector2(-4f, 2f),
			new Vector2(30f, 19f),
			12
		);
		actionIntentAreaBadge.Text = "全";
		actionIntentRoot.AddChild(actionIntentAreaBadge);
		SyncActionIntentPosition();
	}

	private static Label CreateActionIntentDamageLabel() {
		var label = new Label {
			Name = "Damage",
			Position = new Vector2(2f, 40f),
			Size = new Vector2(62f, 23f),
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		if (ResourceLoader.Exists(NativeIntentFontPath)) {
			Font? intentFont = ResourceLoader.Load<Font>(NativeIntentFontPath);
			if (intentFont != null) {
				label.AddThemeFontOverride("font", intentFont);
			}
		}
		label.AddThemeFontSizeOverride("font_size", 22);
		label.AddThemeColorOverride(
			"font_color",
			new Color(1f, 0.964706f, 0.886275f, 1f)
		);
		label.AddThemeColorOverride(
			"font_outline_color",
			new Color(0f, 0f, 0f, 0.501961f)
		);
		label.AddThemeConstantOverride("outline_size", 12);
		return label;
	}

	private static Label CreateActionIntentLabel(
		string name,
		Vector2 position,
		Vector2 size,
		int fontSize
	) {
		var label = new Label {
			Name = name,
			Position = position,
			Size = size,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride("font_color", Colors.White);
		label.AddThemeColorOverride("font_outline_color", Colors.Black);
		label.AddThemeConstantOverride("outline_size", 7);
		return label;
	}

	private void SyncActionIntentPosition() {
		if (actionIntentRoot == null || IntentPosition == null || !IsInsideTree()) return;

		Vector2 viewportSize = GetViewportRect().Size;
		Vector2 anchor = IntentPosition.GetGlobalTransformWithCanvas().Origin;
		float bob = Mathf.Sin(
			(float)Time.GetTicksMsec() * 0.001f * Mathf.Pi + actionIntentBobPhase
		) * ActionIntentBobDistance + ActionIntentBobOffset;
		anchor += Vector2.Up * (ActionIntentHeadClearance + bob);
		float horizontalMargin = Mathf.Min(
			ActionIntentViewportMargin,
			viewportSize.X * 0.5f
		);
		float verticalMargin = Mathf.Min(
			ActionIntentViewportMargin,
			viewportSize.Y * 0.5f
		);
		anchor.X = Mathf.Clamp(
			anchor.X,
			horizontalMargin,
			viewportSize.X - horizontalMargin
		);
		anchor.Y = Mathf.Clamp(
			anchor.Y,
			verticalMargin,
			viewportSize.Y - verticalMargin
		);
		Vector2 localAnchor = actionIntentOverlay
			.GetGlobalTransformWithCanvas()
			.AffineInverse() * anchor;
		actionIntentRoot.Position = localAnchor - Vector2.One * (ActionIntentSize * 0.5f);
	}

	public const string MATERIAL_VFX_PATH = "res://VYgo/scenes/vfx/summon/vfx_link_summon_material.tscn";
	public const string SUMMON_VFX_PATH = "res://VYgo/scenes/vfx/summon/vfx_summon_1.tscn";
	
	public async Task PlayMaterialVfx(float? duration = null) {
		float totalLifeTime = duration
			?? MaterialVfxDuration + (float)GD.RandRange(0.1f, 1f);
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

	public async Task PlayMaterialExitAnimation(
		float compressDuration = MaterialCompressDuration,
		float flyDuration = MaterialFlyDuration,
		Color? flashColor = null
	) {
		if (!GodotObject.IsInstanceValid(_mainSprite)) return;

		var shader = new Shader {
			Code = MaterialShaderCode
		};
		var material = new ShaderMaterial {
			Shader = shader
		};
		material.SetShaderParameter("whiteness", 0f);
		material.SetShaderParameter("flash_color", flashColor ?? Colors.White);
		_mainSprite.Material = material;

		var originalScale = _mainSprite.Scale;
		var compressedScale = new Vector2(originalScale.X * 0.035f, originalScale.Y * 1.12f);
		var compressTween = _mainSprite.CreateTween().SetParallel();
		compressTween.TweenProperty(_mainSprite, "scale", compressedScale, compressDuration)
			.SetTrans(Tween.TransitionType.Expo)
			.SetEase(Tween.EaseType.In);
		compressTween.TweenMethod(
				Callable.From<float>(value => material.SetShaderParameter("whiteness", value)),
				0f,
				1f,
				compressDuration * 0.75f
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
		flyTween.TweenProperty(_mainSprite, "global_position", targetPosition, flyDuration)
			.SetTrans(Tween.TransitionType.Expo)
			.SetEase(Tween.EaseType.In);
		flyTween.TweenProperty(
				_mainSprite,
				"scale",
				new Vector2(compressedScale.X, compressedScale.Y * 1.9f),
				flyDuration
			)
			.SetTrans(Tween.TransitionType.Expo)
			.SetEase(Tween.EaseType.In);
		flyTween.TweenProperty(_mainSprite, "modulate:a", 0f, flyDuration * 0.35f)
			.SetDelay(flyDuration * 0.65f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.In);
		await _mainSprite.ToSignal(flyTween, Tween.SignalName.Finished);
	}

	public async Task PlayQuickMaterialAnimation(Color accentColor) {
		await PlayMaterialVfx(QuickMaterialVfxDuration);
		await PlayMaterialExitAnimation(
			QuickMaterialCompressDuration,
			QuickMaterialFlyDuration,
			accentColor
		);
	}
}
