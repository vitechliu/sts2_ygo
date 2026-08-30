using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.TestSupport;
using VYgo.Scripts.Cards;
using VYgo.Utils;

namespace VYgo.Core.Effects;

public static class MonsterCardVfx {
    public static Task PlaySummonCardFly(
        BaseMonsterCard card,
        Creature summonedCreature,
        Vector2? startPosition = null,
        Color? revealColor = null,
        bool emphasizeReveal = false) {
        return PlayCardFlyToCreature(
            card,
            summonedCreature,
            startPosition,
            revealColor,
            emphasizeReveal
        );
    }

    public static Task PlayEquipCardFly(
        CardModel card,
        Creature target) {
        return PlayCardFlyToCreature(card, target);
    }

    private static async Task PlayCardFlyToCreature(
        CardModel card,
        Creature target,
        Vector2? startPosition = null,
        Color? revealColor = null,
        bool emphasizeReveal = false) {
        if (TestMode.IsOn || NCombatRoom.Instance == null) return;

        var targetNode = NCombatRoom.Instance.GetCreatureNode(target);
        if (targetNode == null) return;

        var sourceNode = startPosition.HasValue
            ? null
            : NCard.FindOnTable(card, PileType.Play);
        var cardNode = NCard.Create(card);
        if (cardNode == null) return;

        if (!startPosition.HasValue
            && sourceNode != null
            && GodotObject.IsInstanceValid(sourceNode)) {
            cardNode.GlobalPosition = sourceNode.GlobalPosition;
            cardNode.Scale = sourceNode.Scale;
            cardNode.Rotation = sourceNode.Rotation;
            sourceNode.Visible = false;
        }
        else {
            cardNode.GlobalPosition = PileType.Play.GetTargetPosition(cardNode);
        }

        NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(cardNode);
        cardNode.UpdateVisuals(PileType.Play, CardPreviewMode.Normal);

        if (startPosition.HasValue) {
            cardNode.PivotOffset = NCard.defaultSize * 0.5f;
            cardNode.GlobalPosition = startPosition.Value - NCard.defaultSize * 0.5f;
            cardNode.ZIndex = 1015;
        }

        if (sourceNode == null) {
            if (revealColor.HasValue) {
                cardNode.Modulate = revealColor.Value;
            }

            float revealDuration = emphasizeReveal ? 0.20f : 0.1f;
            float holdDuration = emphasizeReveal ? 0.18f : 0f;
            Vector2 initialScale = emphasizeReveal
                ? Vector2.One * 0.55f
                : Vector2.Zero;
            var tween = cardNode.CreateTween().SetParallel();
            tween.TweenProperty(cardNode, "scale", Vector2.One, revealDuration)
                .From(initialScale)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
            if (revealColor.HasValue) {
                tween.TweenProperty(cardNode, "modulate", Colors.White, revealDuration)
                    .SetEase(Tween.EaseType.Out)
                    .SetTrans(Tween.TransitionType.Sine);
            }

            if (emphasizeReveal) {
                await VFXUtil.Wait(revealDuration + holdDuration);
            }
            else {
                await Cmd.CustomScaledWait(0.1f, 0.8f);
            }
        }

        var targetPosition = targetNode.Visuals.VfxSpawnPosition.GlobalPosition;
        var vfx = NCardFlySummonVfx.Create(cardNode, targetPosition);
        NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(vfx);
        Task flyTask = TaskHelper.RunSafely(vfx.PlayAnim());

        if (emphasizeReveal) {
            await flyTask;
            return;
        }

        var duration = vfx.GetDuration();
        await Cmd.CustomScaledWait(duration * 0.2f, duration);
    }
}
