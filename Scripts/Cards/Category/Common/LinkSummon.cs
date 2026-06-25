using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
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
[RegisterCharacterStarterCard(typeof(RedhatCharacter), 3)]
public class LinkSummon() : BaseSummonCard(0, CardType.Skill, CardRarity.Basic, TargetType.None) {

    public const string LINK_SUMMON_2D_ASSETS = "res://VYgo/scenes/summon/link/link_summon_2d.tscn";
    
    
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
                if (Godot.GodotObject.IsInstanceValid(nCreature)) {
                    nCreature.QueueFreeSafely();
                }
            }
        }

        var deathAnimationTask = TaskHelper.RunSafely(PlayDeathAnimation());
        nCreature.DeathAnimationTask = deathAnimationTask;
        await CreatureCmd.Kill(material, true);
        await deathAnimationTask;
    }

    private NCard? _node;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        var pile = Entry.ExtraPile.GetPile(Owner);
        if (pile.Cards.Count <= 0) return;
        
        //todo linkSelect
        if ((await CardSelectCmd.FromCombatPile(
                prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 1),
                context: choiceContext,
                pile: pile, player: Owner))
            .FirstOrDefault() is not BaseExtraLinkCard cardModel) return;
        var coreCard = cardModel.YgoGetCore();
        if (coreCard is null) {
            Entry.Logger.Error("Failed to get core card: " + cardModel.CardId);
            return;
        }

        // TrySelectLinkMaterials currently returns a deferred LINQ iterator over Pets.
        // MaterialSacrifice removes creatures from that collection, so take a snapshot
        // before starting any sacrifice task.
        var materials = TrySelectLinkMaterials(coreCard)?.ToList();
        if (materials is null || materials.Count <= 0) return;

        if (!TestMode.IsOn && NCombatRoom.Instance != null) {
            //第零步，隐藏自己
            _node = NCard.FindOnTable(this);
            if (_node == null || !GodotObject.IsInstanceValid(_node) || !_node.IsInsideTree()) {
                return;
            }
            _node.PlayPileTween?.FastForwardToCompletion();
            _node.Visible = false;
            
            //第一步，为素材播放选中动画,并处死
            List<Task> anim = new();
            foreach (var material in materials) {
                anim.Add(TaskHelper.RunSafely(MaterialSacrifice(material)));
            }
            SFXUtil.Play("event:/vygo/sfx/material_shine");
            SFXUtil.PlayAfter("event:/vygo/sfx/material_01", 1.2f);
            await Task.WhenAll(anim);

            //第二步，播放卡片动画，并素材进入墓地
            List<CardModel> cardModels = [];
            foreach (var material in materials) {
                if (material.Monster is BaseMonster bm && bm.YgoGetCard() != null) {
                    cardModels.Add(bm.YgoGetCard());
                }
            }

            SFXUtil.Play("event:/vygo/sfx/link_summon_00");
            await PlaySummonPreviewAnimation(cardModels);

            //第三步，展开连接圆盘并播放圆盘动画
            var screenCenterPos = NGame.Instance.GetViewportRect().Size * 0.5f;
            var mainAnim2D = VFXUtil.GenVFXNode<NLinkSummon2D>(LINK_SUMMON_2D_ASSETS);
            NCombatRoom.Instance.CombatVfxContainer.AddChild(mainAnim2D);
            mainAnim2D.GlobalPosition = screenCenterPos;

            await mainAnim2D.manager.PlayAnimMain();
            
            //第四步，计算连接箭头并播放连接箭头动画
            int linkMarkers = coreCard.Def.Value;
            int linkCount = coreCard.LinkCount.Value;

            (var trailAnim1, linkMarkers) = resolveLink(linkMarkers, linkCount > 5 ? 2 : 1);
            await mainAnim2D.manager.PlayLinks(trailAnim1);
            if (linkCount > 1) {
                (var trailAnim2, linkMarkers) = resolveLink(linkMarkers, linkCount > 4 ? 2 : 1);
                await mainAnim2D.manager.PlayLinks(trailAnim2);
            }
            if (linkCount > 2) {
                var (trailAnim3, _) = resolveLink(linkMarkers, linkCount > 3 ? 2 : 1);
                await mainAnim2D.manager.PlayLinks(trailAnim3);
            }
            
            //第五步，弹出链接目标并播放粒子
            
            
            //第六步，生成

            if (_node != null) {
                _node.Visible = true;
                _node = null;
            }
            await VFXUtil.Wait(2f);
            mainAnim2D.QueueFreeSafely();
        }
        
    }
    
    async Task PlaySummonPreviewAnimation(List<CardModel> cardModels) {
       
        Player owner = Owner;
        ICombatState? combatState = owner?.Creature?.CombatState;
        if (owner == null || combatState == null) {
            return;
        }

        try {
            await Card3DEffectUtil.RunMultipleCard3DEffect(
                cardModels,
                AnimateSummonPreview,
                NGame.Instance.GetViewportRect().Size * 0.5f,
                scaleMultiplier: 0.8f,
                horizontalSpacing: 380f,
                initialOpacity: 0f
            );
        }
        finally {
  
        }
    }

    static async Task AnimateSummonPreview(IReadOnlyList<Card3DEffectContext> ctxs, Vector2 centerPos) {
        if (ctxs.Count < 1) return;

        Color magenta = new("ff00ff");
        foreach (var ctx in ctxs) {
            ConfigureCardEffect(ctx, magenta);
        }

        const float HoverDuration = 1f;
        const float FlyDuration = 0.15f;
        const float FlyDistance = 1600f;
        const float FlyZ = -1200f;

        float[] yaws = DistributeYaws(ctxs.Count);
        List<Tween> hoverTweens = new(ctxs.Count);
        for (int i = 0; i < ctxs.Count; i++) {
            hoverTweens.Add(CreateHoverTween(ctxs[i], yaws[i], -8f, HoverDuration));
        }
        await Task.WhenAll(Enumerable.Range(0, ctxs.Count).Select(i => hoverTweens[i].AwaitFinished(ctxs[i].Pivot)));

        Tween fly = ctxs[0].Pivot.CreateTween().SetParallel();
        foreach (var ctx in ctxs) {
            AddFlyTween(fly, ctx, FlyDistance, FlyZ, FlyDuration);
        }
        await fly.AwaitFinished(ctxs[0].Pivot);
    }

    static float[] DistributeYaws(int count) {
        if (count == 1) return new[] { 0f };
        float[] yaws = new float[count];
        for (int i = 0; i < count; i++) {
            float t = (float)i / (count - 1);
            yaws[i] = Mathf.Lerp(15f, -15f, t);
        }
        return yaws;
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
    
    (List<int>, int) resolveLink(int linkMarkers, int need) {
        int foundMarker = 0;
        int foundMarkerCount = 0;
        List<int> res = [];
        foreach (var (trail, marker) in markers) {
            if (foundMarkerCount < need && (linkMarkers & (int)marker) > 0) {
                foundMarkerCount++;
                foundMarker += (int)marker;
                res.Add(trail);
            }
        }
        return (res, linkMarkers - foundMarker);
    }

    private static readonly List<(int, CardLinkMarker)> markers = new() {
        (2, CardLinkMarker.Top),
        (1, CardLinkMarker.TopLeft),
        (4, CardLinkMarker.Left),
        (6, CardLinkMarker.BottomLeft),
        (7, CardLinkMarker.Bottom),
        (8, CardLinkMarker.BottomRight),
        (5, CardLinkMarker.Right),
        (3, CardLinkMarker.TopRight),
    };
}
