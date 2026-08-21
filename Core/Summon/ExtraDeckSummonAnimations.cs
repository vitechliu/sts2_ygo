using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using VYgo.Core.Cards;
using VYgo.Core.Effects;
using VYgo.Scripts;
using VYgo.Scripts.Cards;
using VYgo.Utils;

namespace VYgo.Core;

internal static class ExtraDeckSummonAnimations {
    public const string FusionSummon2DAssets = "res://VYgo/scenes/summon/fusion/fusion_summon_2d.tscn";
    public const string LinkSummon2DAssets = "res://VYgo/scenes/summon/link/link_summon_2d.tscn";
    public const string XyzSummon2DAssets = "res://VYgo/scenes/summon/xyz/xyz_summon_2d.tscn";
    public const string SynchroSummon2DAssets = "res://VYgo/scenes/summon/synchro/synchro_summon_2d.tscn";

    private static readonly Color FusionRed = new("ff315e");
    private static readonly Color FusionBlue = new("3fb4ff");
    private static readonly Color FusionViolet = new("bd4cff");
    private static readonly Color LinkMagenta = new("ff00ff");
    private static readonly Color XyzBlue = new("55bfff");
    private static readonly Color XyzViolet = new("8a63ff");
    private static readonly Color SynchroCyan = new("39eeff");
    private static readonly Color SynchroGreen = new("74ff8d");

    private static readonly List<(int Trail, CardLinkMarker Marker)> LinkMarkers = new() {
        (2, CardLinkMarker.Top),
        (1, CardLinkMarker.TopLeft),
        (4, CardLinkMarker.Left),
        (6, CardLinkMarker.BottomLeft),
        (7, CardLinkMarker.Bottom),
        (8, CardLinkMarker.BottomRight),
        (5, CardLinkMarker.Right),
        (3, CardLinkMarker.TopRight),
    };

    internal static async Task PlayFusionSummonAnimation(SummonAnimationContext context) {
        var fusionAnim2D = VFXUtil.GenVFXNode<NFusionSummon2D>(FusionSummon2DAssets);
        NCombatRoom.Instance.CombatVfxContainer.AddChild(fusionAnim2D);
        fusionAnim2D.GlobalPosition = context.ScreenCenterPos;

        try {
            SFXUtil.Play("event:/vygo/sfx/link_summon_00");
            await PlayFusionPreviewAnimation(context.MaterialCards, fusionAnim2D, context.ScreenCenterPos);

            SFXUtil.Play("event:/vygo/sfx/link_summon_05");
            await PlayFusionResultCard(context.FinalCard, context.ScreenCenterPos);
        }
        finally {
            if (GodotObject.IsInstanceValid(fusionAnim2D)) {
                fusionAnim2D.QueueFreeSafely();
            }
        }
    }

    private static async Task PlayFusionPreviewAnimation(
        IReadOnlyList<CardModel> cardModels,
        NFusionSummon2D fusionAnim2D,
        Vector2 screenCenterPos
    ) {
        if (cardModels.Count <= 0) return;

        try {
            await Card3DEffectUtil.RunMultipleCard3DEffect(
                cardModels,
                async (ctxs, centerPos) => await AnimateFusionMaterials(ctxs, centerPos, fusionAnim2D),
                screenCenterPos,
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

        foreach (Card3DEffectContext ctx in ctxs) {
            ConfigureCardEffect(ctx, FusionViolet, FusionBlue);
        }

        const float HoverDuration = 0.82f;
        float[] yaws = DistributeYaws(ctxs.Count);
        List<Tween> hoverTweens = new(ctxs.Count);
        for (int i = 0; i < ctxs.Count; i++) {
            hoverTweens.Add(CreateHoverTween(ctxs[i], yaws[i], -7f, HoverDuration, 0.74f));
        }

        await Task.WhenAll(Enumerable.Range(0, ctxs.Count).Select(i => hoverTweens[i].AwaitFinished(ctxs[i].Pivot)));

        Task introTask = fusionAnim2D.Manager.PlayIntro();
        await SpiralCardsIntoFusion(ctxs, centerPos);
        await introTask;
        await fusionAnim2D.Manager.PlayBurst();
    }

    private static async Task SpiralCardsIntoFusion(IReadOnlyList<Card3DEffectContext> ctxs, Vector2 centerPos) {
        const float SpiralDuration = 0.95f;
        const float StaggerDelay = 0.04f;
        const float SpiralTurns = 1.15f;
        Tween tween = ctxs[0].DisplaySprite.CreateTween().SetParallel();

        for (int i = 0; i < ctxs.Count; i++) {
            Card3DEffectContext ctx = ctxs[i];
            Vector2 start = ctx.DisplaySprite.GlobalPosition;
            float startRadius = start.DistanceTo(centerPos);
            float startAngle = (start - centerPos).Angle();
            Vector2 startScale = ctx.DisplaySprite.Scale;
            Color startModulate = ctx.DisplaySprite.Modulate;
            float startRotation = ctx.DisplaySprite.Rotation;
            Vector3 startPivotRotation = ctx.Pivot.RotationDegrees;

            tween.TweenMethod(
                    Callable.From<float>(t => {
                        float orbitProgress = SmoothStep(t);
                        float collapseT = Mathf.Clamp((t - 0.18f) / 0.82f, 0f, 1f);
                        float collapseProgress = Mathf.Pow(collapseT, 2.2f);
                        float angle = startAngle + Mathf.Tau * SpiralTurns * orbitProgress;
                        float radius = startRadius * (1f - collapseProgress);
                        Vector2 position = centerPos + Vector2.FromAngle(angle) * radius;
                        ctx.DisplaySprite.GlobalPosition = position;
                        ctx.DisplaySprite.Rotation = startRotation + Mathf.Tau * 0.7f * orbitProgress;

                        float suctionT = Mathf.Clamp((t - 0.45f) / 0.55f, 0f, 1f);
                        float suctionProgress = SmoothStep(suctionT);
                        Vector2 inwardDirection = centerPos - position;
                        if (inwardDirection.LengthSquared() > 0.0001f) {
                            Vector2 localDirection =
                                inwardDirection.Normalized().Rotated(-ctx.DisplaySprite.Rotation);
                            if (ctx.DisplaySprite.FlipH) localDirection.X = -localDirection.X;
                            if (ctx.DisplaySprite.FlipV) localDirection.Y = -localDirection.Y;
                            ctx.CardMaterial.SetShaderParameter("suction_direction", localDirection);
                        }
                        ctx.CardMaterial.SetShaderParameter("suction_progress", suctionProgress);

                        float scaleT = SmoothStep(Mathf.Clamp((t - 0.55f) / 0.45f, 0f, 1f));
                        float fadeT = SmoothStep(Mathf.Clamp((t - 0.7f) / 0.3f, 0f, 1f));
                        ctx.DisplaySprite.Scale = startScale * Mathf.Lerp(1f, 0.08f, scaleT);
                        ctx.DisplaySprite.Modulate = startModulate with {
                            A = Mathf.Lerp(startModulate.A, 0f, fadeT)
                        };
                        ctx.Pivot.RotationDegrees = new Vector3(
                            Mathf.Lerp(startPivotRotation.X, -28f, orbitProgress),
                            Mathf.Lerp(startPivotRotation.Y, startPivotRotation.Y + 75f, orbitProgress),
                            Mathf.Lerp(startPivotRotation.Z, 24f, orbitProgress)
                        );
                        ctx.CardMaterial.SetShaderParameter(
                            "outline_strength",
                            Mathf.Lerp(1.8f, 4.2f, suctionProgress)
                        );
                        ctx.GlowMaterial.SetShaderParameter(
                            "glow_intensity",
                            Mathf.Lerp(1.4f, 3.5f, suctionProgress)
                        );
                        ctx.GlowMaterial.SetShaderParameter(
                            "glow_radius",
                            Mathf.Lerp(14f, 34f, suctionProgress)
                        );
                    }),
                    0f,
                    1f,
                    SpiralDuration)
                .SetDelay(i * StaggerDelay);
        }

        SFXUtil.PlayAfter("event:/vygo/sfx/link_summon_03", 0.45f);
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

    internal static async Task PlayXyzSummonAnimation(SummonAnimationContext context) {
        IReadOnlyList<CardModel> visibleMaterials = context.MaterialCards.Take(6).ToList();
        if (visibleMaterials.Count == 0) return;

        var xyzAnim2D = VFXUtil.GenVFXNode<NXyzSummon2D>(XyzSummon2DAssets);
        NCombatRoom.Instance.CombatVfxContainer.AddChild(xyzAnim2D);
        xyzAnim2D.GlobalPosition = context.ScreenCenterPos;

        try {
            SFXUtil.Play("event:/vygo/sfx/xyz_01");
            await Card3DEffectUtil.RunMultipleCard3DEffect(
                visibleMaterials,
                async (ctxs, centerPos) => await AnimateXyzMaterials(
                    ctxs,
                    centerPos,
                    xyzAnim2D,
                    context.Materials.Count
                ),
                context.ScreenCenterPos,
                scaleMultiplier: 1.02f,
                horizontalSpacing: 300f,
                initialOpacity: 0f
            );

            SFXUtil.Play("event:/vygo/sfx/xyz_03");
            await PlayXyzResultCard(
                context.FinalCard,
                context.ScreenCenterPos,
                xyzAnim2D
            );
            await xyzAnim2D.Manager.FadeOut();
        }
        catch (Exception ex) {
            Entry.Logger.Warn("PlayXyzSummonAnimation exception: " + ex);
        }
        finally {
            if (GodotObject.IsInstanceValid(xyzAnim2D)) {
                xyzAnim2D.QueueFreeSafely();
            }
        }
    }

    internal static async Task PlaySynchroSummonAnimation(SummonAnimationContext context) {
        if (context.FinalCard is not BaseExtraSynchroCard synchroCard) return;
        CoreCard? coreCard = synchroCard.YgoGetCore();
        int? targetLevel = coreCard == null
            ? null
            : synchroCard.GetSynchroTargetLevel(coreCard);
        if (coreCard == null || targetLevel is not > 0) {
            Entry.Logger.Error("Failed to get Synchro animation level data: " + synchroCard.CardId);
            return;
        }

        List<SummonMaterial> visibleMaterials = context.Materials.Take(6).ToList();
        int tunerLevel = context.Materials
            .Where(material => synchroCard.IsSynchroTuner(coreCard, material))
            .Sum(material => synchroCard.GetSynchroMaterialLevel(coreCard, material) ?? 0);
        int nonTunerLevel = context.Materials
            .Where(material => !synchroCard.IsSynchroTuner(coreCard, material))
            .Sum(material => synchroCard.GetSynchroMaterialLevel(coreCard, material) ?? 0);

        var synchroAnim = VFXUtil.GenVFXNode<NSynchroSummon2D>(SynchroSummon2DAssets);
        NCombatRoom.Instance.CombatVfxContainer.AddChild(synchroAnim);
        synchroAnim.GlobalPosition = context.ScreenCenterPos;
        Task? mainTask = null;
        try {
            await Card3DEffectUtil.RunMultipleCard3DEffect(
                visibleMaterials.Select(material => material.Card!).ToList(),
                async (ctxs, center) => {
                    mainTask = synchroAnim.Manager.PlayMain(
                        targetLevel.Value,
                        tunerLevel,
                        nonTunerLevel
                    );
                    SFXUtil.Play(ctxs.Count <= 4
                        ? "event:/vygo/sfx/synchro_card_01"
                        : "event:/vygo/sfx/synchro_card_02");
                    await AnimateSynchroMaterials(ctxs, visibleMaterials, synchroCard, coreCard, center);
                },
                context.ScreenCenterPos,
                scaleMultiplier: 0.92f,
                horizontalSpacing: 275f,
                initialOpacity: 0f
            );

            await VFXUtil.Wait(Math.Max(
                0f,
                NSynchroSummonManager.PostStart - synchroAnim.Manager.TimelineElapsed
            ));
            Task foregroundTask = synchroAnim.ForegroundManager.PlayForegroundPost();
            await PlaySynchroResultCard(
                context.FinalCard,
                context.ScreenCenterPos,
                synchroAnim.Manager
            );
            await Task.WhenAll(mainTask ?? Task.CompletedTask, foregroundTask);
        }
        finally {
            if (GodotObject.IsInstanceValid(synchroAnim)) synchroAnim.QueueFreeSafely();
        }
    }

    private static async Task AnimateSynchroMaterials(
        IReadOnlyList<Card3DEffectContext> contexts,
        IReadOnlyList<SummonMaterial> materials,
        BaseExtraSynchroCard synchroCard,
        CoreCard coreCard,
        Vector2 center
    ) {
        if (contexts.Count == 0) return;
        Tween tween = contexts[0].DisplaySprite.CreateTween().SetParallel();
        float totalWidth = (contexts.Count - 1) * 275f;
        float exitDistance = Math.Max(
            420f,
            contexts[0].DisplaySprite.GetViewportRect().Size.Y * 0.46f
        );
        for (int i = 0; i < contexts.Count; i++) {
            Card3DEffectContext ctx = contexts[i];
            bool tuner = synchroCard.IsSynchroTuner(coreCard, materials[i]);
            ConfigureCardEffect(
                ctx,
                tuner ? SynchroCyan : SynchroGreen,
                tuner ? SynchroCyan : SynchroGreen,
                2.1f,
                0.12f
            );
            ctx.DisplaySprite.ZIndex = 1005;
            ctx.DisplaySprite.GlobalPosition = center + new Vector2(i * 275f - totalWidth * 0.5f, 20f);
            ctx.Pivot.RotationDegrees = new Vector3(-9f, (i - (contexts.Count - 1) * 0.5f) * -7f, 0f);
            tween.TweenProperty(ctx.DisplaySprite, "modulate:a", 1f, 0.3f)
                .SetDelay(i * 0.035f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
            tween.TweenProperty(ctx.DisplaySprite, "global_position:y", center.Y - 28f, 0.72f)
                .SetDelay(i * 0.035f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Back);
            // Unity/实机素材卡是向屏幕上方的同步光源汇聚，而不是缩入画面深处。
            // 保留横向间隔的一小部分，避免多素材完全重叠成一个黑点。
            float exitX = (i - (contexts.Count - 1) * 0.5f) * 42f;
            Vector2 exitPoint = center + new Vector2(exitX, -exitDistance);
            tween.TweenProperty(ctx.DisplaySprite, "global_position", exitPoint, 0.48f)
                .SetDelay(0.72f)
                .SetEase(Tween.EaseType.In)
                .SetTrans(Tween.TransitionType.Quart);
            tween.TweenProperty(ctx.DisplaySprite, "scale", Vector2.One * 0.38f, 0.48f)
                .SetDelay(0.72f)
                .SetEase(Tween.EaseType.In)
                .SetTrans(Tween.TransitionType.Cubic);
            tween.TweenProperty(
                    ctx.Pivot,
                    "rotation_degrees",
                    new Vector3(-18f, exitX * 0.12f, 0f),
                    0.48f
                )
                .SetDelay(0.72f)
                .SetEase(Tween.EaseType.In)
                .SetTrans(Tween.TransitionType.Cubic);
            tween.TweenProperty(ctx.DisplaySprite, "modulate:a", 0f, 0.16f)
                .SetDelay(1.04f);
        }
        await tween.AwaitFinished(contexts[0].DisplaySprite);
    }

    private static async Task PlaySynchroResultCard(
        CardModel finalCard,
        Vector2 center,
        NSynchroSummonManager manager
    ) {
        await Card3DEffectUtil.RunMultipleCard3DEffect(
            [finalCard],
            async (contexts, target) => {
                if (contexts.Count == 0) return;
                Card3DEffectContext ctx = contexts[0];
                ConfigureCardEffect(ctx, SynchroCyan, Colors.White, 2.8f, 0.08f);
                ctx.DisplaySprite.GlobalPosition = target;
                ctx.DisplaySprite.ZIndex = 1050;
                ctx.Pivot.Position = new Vector3(0f, 0f, -2200f);
                ctx.Pivot.RotationDegrees = new Vector3(-24f, 92f, -12f);

                await VFXUtil.Wait(Math.Max(
                    0f,
                    NSynchroSummonManager.StrongSummon - manager.TimelineElapsed
                ));
                float revealDuration = Math.Max(
                    0.05f,
                    NSynchroSummonManager.StartCard - manager.TimelineElapsed
                );
                Tween strong = ctx.Pivot.CreateTween().SetParallel();
                strong.TweenProperty(ctx.DisplaySprite, "modulate:a", 1f, 0.16f)
                    .SetEase(Tween.EaseType.Out)
                    .SetTrans(Tween.TransitionType.Cubic);
                // 提前开始后保留到 StartCard 的完整时长；三次方 InOut 让首尾更稳、
                // 中段斜率更陡，卡牌从纵深飞出的 3D 过程更容易被看清。
                strong.TweenProperty(ctx.Pivot, "position", Vector3.Zero, revealDuration)
                    .SetEase(Tween.EaseType.InOut)
                    .SetTrans(Tween.TransitionType.Cubic);
                strong.TweenProperty(ctx.Pivot, "rotation_degrees", new Vector3(0f, -4f, 0f), revealDuration)
                    .SetEase(Tween.EaseType.InOut)
                    .SetTrans(Tween.TransitionType.Cubic);
                TweenShaderFloat(strong, ctx.CardMaterial, "outline_strength", 0.5f, 4.6f, 0.22f);
                TweenShaderFloat(strong, ctx.GlowMaterial, "glow_opacity", 0f, 0.95f, 0.25f);
                await strong.AwaitFinished(ctx.Pivot);

                float settleDuration = Math.Max(
                    0.05f,
                    NSynchroSummonManager.MainDuration - manager.TimelineElapsed
                );
                Tween settle = ctx.Pivot.CreateTween().SetParallel();
                settle.TweenProperty(ctx.Pivot, "rotation_degrees", new Vector3(0f, 3f, 0f), settleDuration)
                    .SetEase(Tween.EaseType.InOut)
                    .SetTrans(Tween.TransitionType.Sine);
                TweenShaderFloat(
                    settle,
                    ctx.GlowMaterial,
                    "glow_opacity",
                    0.95f,
                    0.55f,
                    settleDuration
                );
                await settle.AwaitFinished(ctx.Pivot);
            },
            center,
            scaleMultiplier: 1.34f,
            horizontalSpacing: 0f,
            initialOpacity: 0f
        );
    }

    private static async Task AnimateXyzMaterials(
        IReadOnlyList<Card3DEffectContext> ctxs,
        Vector2 centerPos,
        NXyzSummon2D xyzAnim2D,
        int actualMaterialCount
    ) {
        if (ctxs.Count == 0) return;

        foreach (Card3DEffectContext ctx in ctxs) {
            ConfigureCardEffect(ctx, XyzBlue, XyzViolet, 1.75f, 0.12f);
            ctx.DisplaySprite.ZIndex = 1005;
        }

        Vector2[] positions = BuildXyzMaterialLayout(ctxs.Count, centerPos);
        float[] yaws = DistributeYaws(ctxs.Count);
        List<Tween> revealTweens = new(ctxs.Count);
        for (int i = 0; i < ctxs.Count; i++) {
            Card3DEffectContext ctx = ctxs[i];
            ctx.DisplaySprite.GlobalPosition = positions[i];
            ctx.Pivot.RotationDegrees = new Vector3(-7f, yaws[i] * 1.15f, (i - (ctxs.Count - 1) * 0.5f) * 2.5f);
            Tween reveal = CreateHoverTween(ctx, yaws[i] * 0.45f, -6f, 0.68f, 0.78f);
            reveal.TweenProperty(ctx.DisplaySprite, "global_position:y", positions[i].Y - 24f, 0.68f)
                .SetEase(Tween.EaseType.InOut)
                .SetTrans(Tween.TransitionType.Sine);
            revealTweens.Add(reveal);
        }

        Task mainStageTask = xyzAnim2D.Manager.PlayMain(ctxs.Count);
        await Task.WhenAll(Enumerable.Range(0, ctxs.Count)
            .Select(i => revealTweens[i].AwaitFinished(ctxs[i].Pivot)));
        await VFXUtil.Wait(0.17f);

        SFXUtil.Play(actualMaterialCount switch {
            1 => "event:/vygo/sfx/xyz_02_01",
            2 => "event:/vygo/sfx/xyz_02_02",
            _ => "event:/vygo/sfx/xyz_02_03"
        });
        SFXUtil.Play("event:/vygo/sfx/xyz_material");

        await SpiralCardsIntoXyz(ctxs, centerPos);
        await mainStageTask;
    }

    private static Vector2[] BuildXyzMaterialLayout(int count, Vector2 centerPos) {
        Vector2[] positions = new Vector2[count];
        if (count == 1) {
            positions[0] = centerPos;
            return positions;
        }

        float radiusX = count <= 3 ? 260f : 355f;
        float radiusY = count <= 3 ? 90f : 185f;
        float startAngle = -Mathf.Pi * 0.88f;
        float endAngle = -Mathf.Pi * 0.12f;
        for (int i = 0; i < count; i++) {
            float t = (float)i / (count - 1);
            float angle = Mathf.Lerp(startAngle, endAngle, t);
            positions[i] = centerPos + new Vector2(
                Mathf.Cos(angle) * radiusX,
                Mathf.Sin(angle) * radiusY - 30f
            );
        }
        return positions;
    }

    private static async Task SpiralCardsIntoXyz(
        IReadOnlyList<Card3DEffectContext> ctxs,
        Vector2 centerPos
    ) {
        const float Duration = 1.15f;
        const float Stagger = 0.035f;
        Tween tween = ctxs[0].DisplaySprite.CreateTween().SetParallel();

        for (int i = 0; i < ctxs.Count; i++) {
            Card3DEffectContext ctx = ctxs[i];
            int materialIndex = i;
            Vector2 start = ctx.DisplaySprite.GlobalPosition;
            float startRadius = Math.Max(1f, start.DistanceTo(centerPos));
            float startAngle = (start - centerPos).Angle();
            Vector2 startScale = ctx.DisplaySprite.Scale;
            Color startModulate = ctx.DisplaySprite.Modulate;
            float startRotation = ctx.DisplaySprite.Rotation;
            Vector3 startPivotRotation = ctx.Pivot.RotationDegrees;

            tween.TweenMethod(
                    Callable.From<float>(value => {
                        float progress = SmoothStep(value);
                        float angle = startAngle
                            + Mathf.Tau * (1.25f + materialIndex * 0.06f) * progress;
                        float radius = startRadius * Mathf.Pow(1f - progress, 1.65f);
                        ctx.DisplaySprite.GlobalPosition =
                            centerPos + Vector2.FromAngle(angle) * radius;
                        ctx.DisplaySprite.Rotation =
                            startRotation + Mathf.Tau * 0.85f * progress;
                        ctx.DisplaySprite.Scale =
                            startScale * Mathf.Lerp(1f, 0.06f, Mathf.Pow(progress, 1.8f));
                        ctx.DisplaySprite.Modulate = startModulate with {
                            A = Mathf.Lerp(startModulate.A, 0f, Mathf.Pow(progress, 3f))
                        };
                        ctx.Pivot.RotationDegrees = new Vector3(
                            Mathf.Lerp(startPivotRotation.X, -38f, progress),
                            Mathf.Lerp(startPivotRotation.Y, 110f, progress),
                            Mathf.Lerp(startPivotRotation.Z, 35f, progress)
                        );
                        ctx.CardMaterial.SetShaderParameter(
                            "outline_strength",
                            Mathf.Lerp(1.8f, 4.8f, progress)
                        );
                        ctx.GlowMaterial.SetShaderParameter(
                            "glow_intensity",
                            Mathf.Lerp(1.75f, 4.2f, progress)
                        );
                    }),
                    0f,
                    1f,
                    Duration)
                .SetDelay(i * Stagger);
        }

        await tween.AwaitFinished(ctxs[0].DisplaySprite);
    }

    private static async Task PlayXyzResultCard(
        CardModel finalCard,
        Vector2 centerPos,
        NXyzSummon2D xyzAnim2D
    ) {
        await Card3DEffectUtil.RunMultipleCard3DEffect(
            new[] { finalCard },
            async (ctxs, target) => {
                if (ctxs.Count == 0) return;
                Card3DEffectContext ctx = ctxs[0];
                ConfigureCardEffect(ctx, XyzBlue, XyzViolet, 2.2f, 0.1f);
                ctx.DisplaySprite.GlobalPosition = target;
                ctx.DisplaySprite.ZIndex = 1015;
                ctx.Pivot.Position = new Vector3(0f, 0f, -1650f);
                ctx.Pivot.RotationDegrees = new Vector3(-28f, 84f, -14f);

                Task explosionTask = xyzAnim2D.Manager.PlayExplosion();
                Tween reveal = ctx.Pivot.CreateTween().SetParallel();
                reveal.TweenProperty(ctx.DisplaySprite, "modulate:a", 1f, 0.05f)
                    .SetEase(Tween.EaseType.Out)
                    .SetTrans(Tween.TransitionType.Quad);
                reveal.TweenProperty(ctx.Pivot, "position", Vector3.Zero, 0.5f)
                    .SetEase(Tween.EaseType.Out)
                    .SetTrans(Tween.TransitionType.Expo);
                reveal.TweenProperty(ctx.Pivot, "rotation_degrees", new Vector3(0f, -5f, 0f), 0.55f)
                    .SetEase(Tween.EaseType.Out)
                    .SetTrans(Tween.TransitionType.Back);
                TweenShaderFloat(reveal, ctx.CardMaterial, "outline_strength", 0.8f, 4.2f, 0.18f)
                    .SetEase(Tween.EaseType.Out)
                    .SetTrans(Tween.TransitionType.Cubic);
                TweenShaderFloat(reveal, ctx.GlowMaterial, "glow_opacity", 0f, 0.92f, 0.2f)
                    .SetEase(Tween.EaseType.Out)
                    .SetTrans(Tween.TransitionType.Cubic);
                await Task.WhenAll(
                    reveal.AwaitFinished(ctx.Pivot),
                    explosionTask
                );

                SFXUtil.Play("event:/vygo/sfx/xyz_04");
                Task postStageTask = xyzAnim2D.Manager.PlayPostXyz();
                Task foregroundPostTask =
                    xyzAnim2D.ForegroundManager.PlayPostForeground();
                Tween settle = ctx.Pivot.CreateTween().SetParallel();
                settle.TweenProperty(ctx.Pivot, "rotation_degrees", new Vector3(0f, 4f, 0f), 0.14f)
                    .SetEase(Tween.EaseType.InOut)
                    .SetTrans(Tween.TransitionType.Sine);
                TweenShaderFloat(settle, ctx.GlowMaterial, "glow_opacity", 0.92f, 0.55f, 0.14f)
                    .SetEase(Tween.EaseType.Out)
                    .SetTrans(Tween.TransitionType.Sine);
                await Task.WhenAll(
                    settle.AwaitFinished(ctx.Pivot),
                    postStageTask,
                    foregroundPostTask
                );
                await VFXUtil.Wait(0.03f);
            },
            centerPos,
            scaleMultiplier: 1.34f,
            horizontalSpacing: 0f,
            initialOpacity: 0f
        );
    }

    internal static async Task PlayLinkSummonAnimation(SummonAnimationContext context) {
        if (context.FinalCard is not BaseExtraLinkCard linkCard) return;

        CoreCard? coreCard = linkCard.YgoGetCore();
        if (coreCard?.Def == null || coreCard.LinkCount == null) {
            Entry.Logger.Error("Failed to get link marker data: " + linkCard.CardId);
            return;
        }

        SFXUtil.Play("event:/vygo/sfx/link_summon_00");
        await PlayLinkPreviewAnimation(context.MaterialCards, context.ScreenCenterPos);

        var mainAnim2D = VFXUtil.GenVFXNode<NLinkSummon2D>(LinkSummon2DAssets);
        NCombatRoom.Instance.CombatVfxContainer.AddChild(mainAnim2D);
        mainAnim2D.GlobalPosition = context.ScreenCenterPos;

        try {
            await mainAnim2D.manager.PlayAnimMain();
            await PlayLinkMarkers(mainAnim2D, coreCard.Def.Value, coreCard.LinkCount.Value);
            await VFXUtil.Wait(0.5f);
            
            mainAnim2D.manager.ShineFinal();
            SFXUtil.Play("event:/vygo/sfx/link_summon_04");
            await VFXUtil.Wait(0.1f);
            SFXUtil.Play("event:/vygo/sfx/link_summon_05");
            mainAnim2D.manager.PlayPostEffect();
            await NLinkPostLinkCardVfx.Play(context.FinalCard, context.ScreenCenterPos);
        }
        finally {
            if (GodotObject.IsInstanceValid(mainAnim2D)) {
                mainAnim2D.QueueFreeSafely();
            }
        }
    }

    private static async Task PlayLinkPreviewAnimation(IReadOnlyList<CardModel> cardModels, Vector2 screenCenterPos) {
        if (cardModels.Count <= 0) return;

        try {
            await Card3DEffectUtil.RunMultipleCard3DEffect(
                cardModels,
                AnimateLinkSummonPreview,
                screenCenterPos,
                scaleMultiplier: 1.1f,
                horizontalSpacing: 380f,
                initialOpacity: 0f
            );
        }
        catch (Exception ex) {
            Entry.Logger.Warn("PlaySummonPreviewAnimation exception: " + ex);
        }
    }

    private static async Task AnimateLinkSummonPreview(IReadOnlyList<Card3DEffectContext> ctxs, Vector2 centerPos) {
        if (ctxs.Count < 1) return;

        foreach (Card3DEffectContext ctx in ctxs) {
            ConfigureCardEffect(ctx, LinkMagenta, LinkMagenta, 1.2f, 0f);
        }

        const float HoverDuration = 1f;
        const float FlyDuration = 0.15f;
        const float FlyDistance = 1600f;
        const float FlyZ = -1200f;

        float[] yaws = DistributeYaws(ctxs.Count);
        List<Tween> hoverTweens = new(ctxs.Count);
        for (int i = 0; i < ctxs.Count; i++) {
            hoverTweens.Add(CreateHoverTween(ctxs[i], yaws[i], -8f, HoverDuration, 0.72f));
        }

        await Task.WhenAll(Enumerable.Range(0, ctxs.Count).Select(i => hoverTweens[i].AwaitFinished(ctxs[i].Pivot)));

        Tween fly = ctxs[0].Pivot.CreateTween().SetParallel();
        foreach (Card3DEffectContext ctx in ctxs) {
            AddFlyTween(fly, ctx, FlyDistance, FlyZ, FlyDuration);
        }

        await fly.AwaitFinished(ctxs[0].Pivot);
    }

    private static async Task PlayLinkMarkers(NLinkSummon2D mainAnim2D, int linkMarkers, int linkCount) {
        (List<int> trailAnim1, linkMarkers) = ResolveLink(linkMarkers, linkCount > 5 ? 2 : 1);
        await mainAnim2D.manager.PlayLinks(trailAnim1);
        if (linkCount > 1) {
            (List<int> trailAnim2, linkMarkers) = ResolveLink(linkMarkers, linkCount > 4 ? 2 : 1);
            await mainAnim2D.manager.PlayLinks(trailAnim2);
        }

        if (linkCount > 2) {
            (List<int> trailAnim3, _) = ResolveLink(linkMarkers, linkCount > 3 ? 2 : 1);
            await mainAnim2D.manager.PlayLinks(trailAnim3);
        }
    }

    private static (List<int>, int) ResolveLink(int linkMarkers, int need) {
        int foundMarker = 0;
        int foundMarkerCount = 0;
        List<int> result = [];
        foreach ((int trail, CardLinkMarker marker) in LinkMarkers) {
            if (foundMarkerCount < need && (linkMarkers & (int)marker) > 0) {
                foundMarkerCount++;
                foundMarker += (int)marker;
                result.Add(trail);
            }
        }

        return (result, linkMarkers - foundMarker);
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

    private static void ConfigureCardEffect(
        Card3DEffectContext ctx,
        Color outlineColor,
        Color glowColor,
        float glowIntensity = 1.4f,
        float pulseAmount = 0.18f
    ) {
        ctx.CardMaterial.SetShaderParameter("glow_color", outlineColor);
        ctx.CardMaterial.SetShaderParameter("outline_strength", 0f);
        ctx.CardMaterial.SetShaderParameter("pulse_amount", 0f);
        ctx.CardMaterial.SetShaderParameter("suction_progress", 0f);
        ctx.CardMaterial.SetShaderParameter("suction_direction", Vector2.Up);

        ctx.GlowMaterial.SetShaderParameter("glow_color", glowColor);
        ctx.GlowMaterial.SetShaderParameter("glow_intensity", glowIntensity);
        ctx.GlowMaterial.SetShaderParameter("glow_opacity", 0f);
        ctx.GlowMaterial.SetShaderParameter("pulse_amount", pulseAmount);
        ctx.GlowMaterial.SetShaderParameter("pulse_speed", 7.5f);
        ctx.GlowMaterial.SetShaderParameter("vertical_blur", 0f);
    }

    private static Tween CreateHoverTween(
        Card3DEffectContext ctx,
        float yawDeg,
        float pitchDeg,
        float duration,
        float glowOpacity
    ) {
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
        TweenShaderFloat(tween, ctx.GlowMaterial, "glow_opacity", 0f, glowOpacity, 0.45f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        return tween;
    }

    private static void AddFlyTween(Tween tween, Card3DEffectContext ctx, float distance, float zOffset, float duration) {
        Vector3 start = ctx.Pivot.Position;
        tween.TweenProperty(ctx.Pivot, "position", new Vector3(start.X, start.Y, start.Z + zOffset), duration)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(ctx.DisplaySprite, "global_position:y", ctx.DisplaySprite.GlobalPosition.Y - distance, duration)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(ctx.Pivot, "rotation:x", Mathf.DegToRad(-35f), duration)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Quad);
        TweenShaderFloat(tween, ctx.CardMaterial, "outline_strength", 1.8f, 3.4f, duration * 0.35f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        TweenShaderFloat(tween, ctx.GlowMaterial, "glow_intensity", 1.2f, 2.6f, duration * 0.35f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        TweenShaderFloat(tween, ctx.GlowMaterial, "glow_radius", 14f, 22f, duration)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Quad);
        TweenShaderFloat(tween, ctx.GlowMaterial, "vertical_blur", 0f, 1f, duration * 0.25f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        TweenShaderFloat(tween, ctx.GlowMaterial, "vertical_blur_length", 90f, 150f, duration)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Quad);
        TweenShaderFloat(tween, ctx.GlowMaterial, "glow_opacity", 0.72f, 0f, duration * 0.5f)
            .SetDelay(duration * 0.5f)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Quad);
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

    private static float SmoothStep(float value) {
        value = Mathf.Clamp(value, 0f, 1f);
        return value * value * (3f - 2f * value);
    }
}
