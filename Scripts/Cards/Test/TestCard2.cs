using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core.Effects;
using VYgo.Scripts.Cards.Category.CyberDragon;
using VYgo.Scripts.Cards.Placeholders;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Test;

[RegisterCard(typeof(RedhatCardPool))]
[RegisterCharacterStarterCard(typeof(RedhatCharacter), 1)]
public class TestCard2() : BasePlaceholder(CardType.Skill, CardRarity.Common) {
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        await base.OnPlay(choiceContext, cardPlay);
        await PlaySummonPreviewAnimation();
    }

    async Task PlaySummonPreviewAnimation() {
        NCard? node = NCard.FindOnTable(this);
        if (node == null || !GodotObject.IsInstanceValid(node) || !node.IsInsideTree()) {
            return;
        }
        node.PlayPileTween?.FastForwardToCompletion();
        node.Visible = false;

        Player owner = Owner;
        ICombatState? combatState = owner?.Creature?.CombatState;
        if (owner == null || combatState == null) {
            node.Visible = true;
            return;
        }

        try {
            await Card3DEffectUtil.RunMultipleCard3DEffect(
                [ModelDb.Card<CyberDragon>(), ModelDb.Card<ProtoCyberDragon>()],
                AnimateSummonPreview,
                node.GetViewportRect().Size * 0.5f,
                scaleMultiplier: 0.7f,
                horizontalSpacing: 420f,
                initialOpacity: 0f
            );
        }
        finally {
            node.Visible = true;
        }

        await AddSpawnedCardsToDiscardPile(owner, combatState);
    }

    static async Task AnimateSummonPreview(IReadOnlyList<Card3DEffectContext> ctxs, Vector2 centerPos) {
        if (ctxs.Count < 2) return;

        Card3DEffectContext left = ctxs[0];
        Card3DEffectContext right = ctxs[1];
        Color magenta = new("ff00ff");
        ConfigureCardEffect(left, magenta);
        ConfigureCardEffect(right, magenta);

        const float HoverDuration = 2.0f;
        const float FlyDuration = 0.7f;
        const float FlyDistance = 1600f;
        const float FlyZ = -1200f;

        Tween leftHover = CreateHoverTween(left, 15f, -8f, HoverDuration);
        Tween rightHover = CreateHoverTween(right, -15f, -8f, HoverDuration);
        await leftHover.AwaitFinished(left.Pivot);

        Tween fly = left.Pivot.CreateTween().SetParallel();
        AddFlyTween(fly, left, FlyDistance, FlyZ, FlyDuration);
        AddFlyTween(fly, right, FlyDistance, FlyZ, FlyDuration);
        await fly.AwaitFinished(left.Pivot);
    }

    static void ConfigureCardEffect(Card3DEffectContext ctx, Color glowColor) {
        ctx.CardMaterial.SetShaderParameter("glow_color", glowColor);
        ctx.CardMaterial.SetShaderParameter("outline_strength", 0f);
        ctx.CardMaterial.SetShaderParameter("pulse_amount", 0f);

        ctx.GlowMaterial.SetShaderParameter("glow_color", glowColor);
        ctx.GlowMaterial.SetShaderParameter("glow_intensity", 1.2f);
        ctx.GlowMaterial.SetShaderParameter("glow_opacity", 0f);
        ctx.GlowMaterial.SetShaderParameter("pulse_amount", 0f);
        ctx.GlowMaterial.SetShaderParameter("vertical_blur", 0f);
    }

    static Tween CreateHoverTween(Card3DEffectContext ctx, float yawDeg, float pitchDeg, float duration) {
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
        TweenShaderFloat(tween, ctx.GlowMaterial, "glow_opacity", 0f, 0.72f, 0.45f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        return tween;
    }

    static void AddFlyTween(Tween tween, Card3DEffectContext ctx, float distance, float zOffset, float duration) {
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

    static MethodTweener TweenShaderFloat(
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

    static async Task AddSpawnedCardsToDiscardPile(Player owner, ICombatState combatState) {
        CardModel cyberDragon = combatState.CreateCard<CyberDragon>(owner);
        CardModel protoCyberDragon = combatState.CreateCard<ProtoCyberDragon>(owner);

        foreach (CardModel card in new[] { cyberDragon, protoCyberDragon }) {
            if (card.Pile == null) {
                await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Discard, owner);
            }
        }
    }
}
