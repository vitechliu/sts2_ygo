using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core.Effects;
using VYgo.Scripts.Cards.Placeholders;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Test;

[RegisterCard(typeof(RedhatCardPool))]
// [RegisterCharacterStarterCard(typeof(RedhatCharacter), 1)]
public class Test3DCard() : BasePlaceholder(CardType.Skill, CardRarity.Common) {
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        await base.OnPlay(choiceContext, cardPlay);
        await Play3DCenterFlipAnimation();
    }

    async Task Play3DCenterFlipAnimation() {
        NCard? node = NCard.FindOnTable(this);
        if (node == null || !GodotObject.IsInstanceValid(node) || !node.IsInsideTree()) {
            return;
        }
        node.PlayPileTween?.FastForwardToCompletion();
        Vector2 screenCenter = node.GetViewportRect().Size * 0.5f;

        await Card3DEffectUtil.RunCard3DEffect(
            node,
            async ctx => {
                Tween tween = ctx.Pivot.CreateTween();
                tween.TweenProperty(ctx.Pivot, "rotation:y", Mathf.DegToRad(360f * 2), 1.4f)
                    .SetEase(Tween.EaseType.InOut)
                    .SetTrans(Tween.TransitionType.Sine);
                await tween.AwaitFinished(ctx.Pivot);
            },
            screenCenter,
            scaleMultiplier: 1f
        );
    }
}
