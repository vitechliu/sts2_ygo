using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Core.Effects;
using VYgo.Scripts.Monsters;
using VYgo.Scripts.Pools;
using VYgo.Utils;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(RedhatCardPool))]
[RegisterCharacterStarterCard(typeof(RedhatCharacter), 1)]
public class Polymerization() 
    : BaseVYgoCard(0, CardType.Skill, CardRarity.Basic, TargetType.None) {
    public const string FUSION_SUMMON_2D_ASSETS = "res://VYgo/scenes/summon/fusion/fusion_summon_2d.tscn";

    protected override YgoType CardYgoType => YgoType.spell;
    private static readonly Color FusionRed = new("ff315e");
    private static readonly Color FusionBlue = new("3fb4ff");
    private static readonly Color FusionViolet = new("bd4cff");

    private NCard? _node;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        var pile = Entry.ExtraPile.GetPile(Owner);
        if (pile.Cards.Count <= 0) return;

        if ((await CardSelectCmd.FromCombatPile(
                prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 1),
                context: choiceContext,
                pile: pile,
                player: Owner,
                filter: cm => cm is BaseExtraFusionCard))
            .FirstOrDefault() is not BaseExtraFusionCard cardModel) return;

        var materials = TrySelectFusionMaterials(cardModel).ToList();
        if (materials.Count < cardModel.FusionMaterialCount) {
            Entry.Logger.Info($"FusionSummon: not enough materials for {cardModel.GetType().Name}");
            return;
        }

        if (!TestMode.IsOn && NCombatRoom.Instance != null) {
            _node = NCard.FindOnTable(this);
            if (_node == null || !GodotObject.IsInstanceValid(_node) || !_node.IsInsideTree()) {
                return;
            }

            _node.PlayPileTween?.FastForwardToCompletion();
            _node.Visible = false;

            List<CardModel> materialCardModels = GetMaterialCardModels(materials);

            List<Task> sacrificeTasks = new();
            foreach (var material in materials) {
                sacrificeTasks.Add(TaskHelper.RunSafely(MaterialSacrifice(material)));
            }
            SFXUtil.Play("event:/vygo/sfx/material_shine");
            await Task.WhenAll(sacrificeTasks);

            Vector2 screenCenterPos = NGame.Instance.GetViewportRect().Size * 0.5f;
            var fusionAnim2D = VFXUtil.GenVFXNode<NFusionSummon2D>(FUSION_SUMMON_2D_ASSETS);
            NCombatRoom.Instance.CombatVfxContainer.AddChild(fusionAnim2D);
            fusionAnim2D.GlobalPosition = screenCenterPos;

            try {
                SFXUtil.Play("event:/vygo/sfx/link_summon_00");
                await PlayFusionPreviewAnimation(materialCardModels, fusionAnim2D);

                var finalCard = cardModel.CreateClone();
                await CardPileCmd.Add(finalCard, PileType.Play);

                SFXUtil.Play("event:/vygo/sfx/link_summon_05");
                await PlayFusionResultCard(finalCard, screenCenterPos);

                if (!finalCard.Owner.Creature.IsDead) {
                    await CardCmd.AutoPlay(choiceContext, finalCard, (Creature)null);
                }
                await VFXUtil.Wait(0.45f);
            }
            finally {
                if (GodotObject.IsInstanceValid(fusionAnim2D)) {
                    fusionAnim2D.QueueFreeSafely();
                }
                if (_node != null) {
                    _node.Visible = true;
                    _node = null;
                }
            }
        }
    }

    private IEnumerable<Creature> TrySelectFusionMaterials(BaseExtraFusionCard fusionCard) {
        return Owner.Creature.Pets
            .Where(c => c.Monster is BaseMonster)
            .Take(fusionCard.FusionMaterialCount);
    }

    private static List<CardModel> GetMaterialCardModels(IEnumerable<Creature> materials) {
        List<CardModel> cardModels = new();
        foreach (var material in materials) {
            if (material.Monster is BaseMonster bm && bm.YgoGetCard() != null) {
                cardModels.Add(bm.YgoGetCard());
            }
        }
        return cardModels;
    }

    private async Task MaterialSacrifice(Creature material) {
        var nCreature = material.GetCreatureNode();
        if (nCreature is null) return;
        var visuals = nCreature.Visuals as NMonsterVisuals;
        if (visuals is null) return;

        nCreature.ToggleIsInteractable(false);
        nCreature.AnimHideIntent();

        async Task PlayDeathAnimation() {
            try {
                await visuals.PlayMaterialVfx();
                await visuals.PlayMaterialExitAnimation();
            }
            finally {
                if (GodotObject.IsInstanceValid(nCreature)) {
                    nCreature.QueueFreeSafely();
                }
            }
        }

        var deathAnimationTask = TaskHelper.RunSafely(PlayDeathAnimation());
        nCreature.DeathAnimationTask = deathAnimationTask;
        await CreatureCmd.Kill(material, true);
        await deathAnimationTask;
    }

    private async Task PlayFusionPreviewAnimation(List<CardModel> cardModels, NFusionSummon2D fusionAnim2D) {
        Player owner = Owner;
        var combatState = owner?.Creature?.CombatState;
        if (owner == null || combatState == null || cardModels.Count <= 0) {
            return;
        }

        try {
            await Card3DEffectUtil.RunMultipleCard3DEffect(
                cardModels,
                async (ctxs, centerPos) => await AnimateFusionMaterials(ctxs, centerPos, fusionAnim2D),
                NGame.Instance.GetViewportRect().Size * 0.5f,
                scaleMultiplier: 1.1f,
                horizontalSpacing: 380f,
                initialOpacity: 0f
            );
        }
        catch (Exception ex) {
            Entry.Logger.Warn("PlayFusionPreviewAnimation exception: " + ex);
        }
    }

    private static async Task AnimateFusionMaterials(
        IReadOnlyList<Card3DEffectContext> ctxs,
        Vector2 centerPos,
        NFusionSummon2D fusionAnim2D
    ) {
        if (ctxs.Count < 1) return;

        foreach (var ctx in ctxs) {
            ConfigureCardEffect(ctx, FusionViolet, FusionBlue);
        }

        const float HoverDuration = 0.82f;
        float[] yaws = DistributeYaws(ctxs.Count);
        List<Tween> hoverTweens = new(ctxs.Count);
        for (int i = 0; i < ctxs.Count; i++) {
            hoverTweens.Add(CreateHoverTween(ctxs[i], yaws[i], -7f, HoverDuration));
        }
        await Task.WhenAll(Enumerable.Range(0, ctxs.Count).Select(i => hoverTweens[i].AwaitFinished(ctxs[i].Pivot)));

        Task introTask = fusionAnim2D.Manager.PlayIntro();
        await SpiralCardsIntoFusion(ctxs, centerPos);
        await introTask;
        await fusionAnim2D.Manager.PlayBurst();
    }

    private static async Task SpiralCardsIntoFusion(IReadOnlyList<Card3DEffectContext> ctxs, Vector2 centerPos) {
        const float SpiralDuration = 0.72f;
        Tween tween = ctxs[0].DisplaySprite.CreateTween().SetParallel();

        for (int i = 0; i < ctxs.Count; i++) {
            Card3DEffectContext ctx = ctxs[i];
            Vector2 start = ctx.DisplaySprite.GlobalPosition;
            float startRadius = start.DistanceTo(centerPos);
            float startAngle = (start - centerPos).Angle();
            float direction = i % 2 == 0 ? 1f : -1f;
            float phase = i * 0.32f;
            Vector2 startScale = ctx.DisplaySprite.Scale;
            Color startModulate = ctx.DisplaySprite.Modulate;

            tween.TweenMethod(
                    Callable.From<float>(t => {
                        float eased = 1f - Mathf.Pow(1f - t, 3f);
                        float angle = startAngle + direction * (Mathf.Pi * 2.35f * eased + phase);
                        float radius = Mathf.Lerp(startRadius, 0f, eased);
                        ctx.DisplaySprite.GlobalPosition = centerPos + Vector2.FromAngle(angle) * radius;
                        ctx.DisplaySprite.Rotation = direction * Mathf.Pi * 5.5f * eased;
                        ctx.DisplaySprite.Scale = startScale * Mathf.Lerp(1f, 0.08f, eased);
                        ctx.DisplaySprite.Modulate = startModulate with { A = Mathf.Lerp(startModulate.A, 0f, Mathf.Clamp((t - 0.58f) / 0.42f, 0f, 1f)) };
                        ctx.Pivot.RotationDegrees = new Vector3(
                            Mathf.Lerp(-7f, -46f, eased),
                            Mathf.Lerp(ctx.Pivot.RotationDegrees.Y, 240f * direction, t),
                            Mathf.Lerp(0f, 90f * direction, eased)
                        );
                        ctx.CardMaterial.SetShaderParameter("outline_strength", Mathf.Lerp(1.8f, 4.2f, eased));
                        ctx.GlowMaterial.SetShaderParameter("glow_intensity", Mathf.Lerp(1.6f, 3.5f, eased));
                        ctx.GlowMaterial.SetShaderParameter("glow_radius", Mathf.Lerp(14f, 34f, eased));
                    }),
                    0f,
                    1f,
                    SpiralDuration)
                .SetEase(Tween.EaseType.InOut)
                .SetTrans(Tween.TransitionType.Sine);
        }

        SFXUtil.PlayAfter("event:/vygo/sfx/link_summon_03", 0.32f);
        await tween.AwaitFinished(ctxs[0].DisplaySprite);
    }

    private static async Task PlayFusionResultCard(CardModel finalCard, Vector2 centerPos) {
        await Card3DEffectUtil.RunMultipleCard3DEffect(
            new[] { finalCard },
            async (ctxs, target) => {
                if (ctxs.Count > 0) {
                    await AnimateFusionResult(ctxs[0], target);
                }
            },
            centerPos,
            scaleMultiplier: 1.32f,
            horizontalSpacing: 0f,
            initialOpacity: 0f
        );
    }

    private static async Task AnimateFusionResult(Card3DEffectContext ctx, Vector2 centerPos) {
        ConfigureCardEffect(ctx, FusionRed, FusionBlue);
        ctx.DisplaySprite.GlobalPosition = centerPos;
        ctx.DisplaySprite.ZIndex = 1010;
        ctx.GlowSprite.ZIndex = -1;
        ctx.Pivot.Position = new Vector3(0f, 0f, -1450f);
        ctx.Pivot.RotationDegrees = new Vector3(-24f, 72f, -12f);

        Tween reveal = ctx.Pivot.CreateTween().SetParallel();
        reveal.TweenProperty(ctx.DisplaySprite, "modulate:a", 1f, 0.08f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        reveal.TweenProperty(ctx.Pivot, "position", Vector3.Zero, 0.52f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
        reveal.TweenProperty(ctx.Pivot, "rotation_degrees", new Vector3(0f, -6f, 0f), 0.52f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        TweenShaderFloat(reveal, ctx.CardMaterial, "outline_strength", 1.2f, 3.6f, 0.22f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        TweenShaderFloat(reveal, ctx.GlowMaterial, "glow_opacity", 0.1f, 0.88f, 0.25f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        TweenShaderFloat(reveal, ctx.GlowMaterial, "vertical_blur", 1f, 0f, 0.5f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);

        await reveal.AwaitFinished(ctx.Pivot);

        Tween settle = ctx.Pivot.CreateTween().SetParallel();
        settle.TweenProperty(ctx.Pivot, "rotation_degrees", new Vector3(0f, 5f, 0f), 0.18f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);
        TweenShaderFloat(settle, ctx.CardMaterial, "outline_strength", 3.6f, 1.8f, 0.18f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);
        TweenShaderFloat(settle, ctx.GlowMaterial, "glow_opacity", 0.88f, 0.52f, 0.18f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);
        await settle.AwaitFinished(ctx.Pivot);
        await VFXUtil.Wait(0.18f);
    }

    private static float[] DistributeYaws(int count) {
        if (count == 1) return new[] { 0f };
        float[] yaws = new float[count];
        for (int i = 0; i < count; i++) {
            float t = (float)i / (count - 1);
            yaws[i] = Mathf.Lerp(15f, -15f, t);
        }
        return yaws;
    }

    private static void ConfigureCardEffect(Card3DEffectContext ctx, Color outlineColor, Color glowColor) {
        ctx.CardMaterial.SetShaderParameter("glow_color", outlineColor);
        ctx.CardMaterial.SetShaderParameter("outline_strength", 0f);
        ctx.CardMaterial.SetShaderParameter("pulse_amount", 0f);

        ctx.GlowMaterial.SetShaderParameter("glow_color", glowColor);
        ctx.GlowMaterial.SetShaderParameter("glow_intensity", 1.4f);
        ctx.GlowMaterial.SetShaderParameter("glow_opacity", 0f);
        ctx.GlowMaterial.SetShaderParameter("pulse_amount", 0.18f);
        ctx.GlowMaterial.SetShaderParameter("pulse_speed", 7.5f);
        ctx.GlowMaterial.SetShaderParameter("vertical_blur", 0f);
    }

    private static Tween CreateHoverTween(Card3DEffectContext ctx, float yawDeg, float pitchDeg, float duration) {
        Tween tween = ctx.Pivot.CreateTween().SetParallel();
        tween.TweenProperty(ctx.Pivot, "rotation:y", Mathf.DegToRad(yawDeg), duration)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(ctx.Pivot, "rotation:x", Mathf.DegToRad(pitchDeg), duration * 0.7f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(ctx.DisplaySprite, "modulate:a", 1f, 0.2f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        TweenShaderFloat(tween, ctx.CardMaterial, "outline_strength", 0f, 1.8f, 0.35f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        TweenShaderFloat(tween, ctx.GlowMaterial, "glow_opacity", 0f, 0.74f, 0.45f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        return tween;
    }

    private static MethodTweener TweenShaderFloat(
        Tween tween,
        ShaderMaterial material,
        StringName parameter,
        float from,
        float to,
        double duration
    ) {
        return tween.TweenMethod(
            Callable.From<float>(value => material.SetShaderParameter(parameter, value)),
            from,
            to,
            duration
        );
    }

    public override int CardId => 24094653;
}
