using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using VYgo.Scripts.Cards;

namespace VYgo.Core.Effects;

public static class MonsterCardVfx {
    public static async Task PlaySummonCardFly(BaseMonsterCard card, Creature summonedCreature) {
        if (TestMode.IsOn || NCombatRoom.Instance == null) return;

        var targetNode = NCombatRoom.Instance.GetCreatureNode(summonedCreature);
        if (targetNode == null) return;

        var sourceNode = NCard.FindOnTable(card, PileType.Play);
        var cardNode = NCard.Create(card);
        if (cardNode == null) return;

        if (sourceNode != null && GodotObject.IsInstanceValid(sourceNode)) {
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

        if (sourceNode == null) {
            var tween = cardNode.CreateTween();
            tween.Parallel().TweenProperty(cardNode, "scale", Vector2.One, 0.1f)
                .From(Vector2.Zero)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);

            await Cmd.CustomScaledWait(0.1f, 0.8f);
        }

        var targetPosition = targetNode.Visuals.VfxSpawnPosition.GlobalPosition;
        var vfx = NCardFlySummonVfx.Create(cardNode, targetPosition);
        NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(vfx);
        _ = TaskHelper.RunSafely(vfx.PlayAnim());

        var duration = vfx.GetDuration();
        await Cmd.CustomScaledWait(duration * 0.2f, duration);
    }
}
