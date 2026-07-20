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

    private static readonly Color FusionRed = new("ff315e");
    private static readonly Color FusionBlue = new("3fb4ff");
    private static readonly Color FusionViolet = new("bd4cff");
    private static readonly Color LinkMagenta = new("ff00ff");

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
                        ctx.DisplaySprite.Modulate = startModulate with {
                            A = Mathf.Lerp(startModulate.A, 0f, Mathf.Clamp((t - 0.58f) / 0.42f, 0f, 1f))
                        };
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
}
