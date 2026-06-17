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
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Placeholders;

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
        Vector2 screenCenter = node.GetViewportRect().Size * 0.5f;

        Player owner = Owner;
        ICombatState? combatState = owner?.Creature?.CombatState;
        if (owner == null || combatState == null) {
            return;
        }

        CardModel cyberDragon = combatState.CreateCard<CyberDragon>(owner);
        CardModel protoCyberDragon = combatState.CreateCard<ProtoCyberDragon>(owner);

        List<CardModel> spawnedCards = new() { cyberDragon, protoCyberDragon };

        await Card3DEffectUtil.RunMultipleCard3DEffect(
            spawnedCards,
            async (ctxs, centerPos) => {
                if (ctxs.Count < 2) return;

                Card3DEffectContext left = ctxs[0];
                Card3DEffectContext right = ctxs[1];

                float hoverDuration = 2.0f;
                float flyDuration = 0.7f;

                Tween hover = left.Pivot.CreateTween().SetParallel();

                hover.TweenProperty(left.Pivot, "rotation:y", Mathf.DegToRad(15f), hoverDuration)
                    .SetEase(Tween.EaseType.InOut)
                    .SetTrans(Tween.TransitionType.Sine);
                hover.TweenProperty(left.Pivot, "rotation:x", Mathf.DegToRad(-8f), hoverDuration * 0.7f)
                    .SetEase(Tween.EaseType.InOut)
                    .SetTrans(Tween.TransitionType.Sine);

                Tween hover2 = right.Pivot.CreateTween().SetParallel();
                hover2.TweenProperty(right.Pivot, "rotation:y", Mathf.DegToRad(-15f), hoverDuration)
                    .SetEase(Tween.EaseType.InOut)
                    .SetTrans(Tween.TransitionType.Sine);
                hover2.TweenProperty(right.Pivot, "rotation:x", Mathf.DegToRad(-8f), hoverDuration * 0.7f)
                    .SetEase(Tween.EaseType.InOut)
                    .SetTrans(Tween.TransitionType.Sine);

                await hover.AwaitFinished(left.Pivot);

                Tween fly = left.Pivot.CreateTween().SetParallel();
                Vector3 leftStart = left.Pivot.Position;
                Vector3 rightStart = right.Pivot.Position;
                float flyDistance = 1600f;
                float flyZ = -1200f;

                fly.TweenProperty(left.Pivot, "position", new Vector3(leftStart.X, leftStart.Y - flyDistance, leftStart.Z + flyZ), flyDuration)
                    .SetEase(Tween.EaseType.In)
                    .SetTrans(Tween.TransitionType.Quad);
                fly.TweenProperty(left.Pivot, "rotation:x", Mathf.DegToRad(-35f), flyDuration)
                    .SetEase(Tween.EaseType.In)
                    .SetTrans(Tween.TransitionType.Quad);

                fly.TweenProperty(right.Pivot, "position", new Vector3(rightStart.X, rightStart.Y - flyDistance, rightStart.Z + flyZ), flyDuration)
                    .SetEase(Tween.EaseType.In)
                    .SetTrans(Tween.TransitionType.Quad);
                fly.TweenProperty(right.Pivot, "rotation:x", Mathf.DegToRad(-35f), flyDuration)
                    .SetEase(Tween.EaseType.In)
                    .SetTrans(Tween.TransitionType.Quad);

                await fly.AwaitFinished(left.Pivot);
            },
            screenCenter,
            scaleMultiplier: 1.3f,
            horizontalSpacing: 420f
        );

        foreach (CardModel card in spawnedCards) {
            if (card.Pile == null) {
                await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Discard, owner);
            }
        }
    }
}
