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

        var cardNode = NCard.FindOnTable(card, PileType.Play);
        var duplicatedCardNode = false;
        if (cardNode == null) {
            cardNode = NCard.Create(card);
            duplicatedCardNode = true;
        }

        if (cardNode == null) return;

        if (duplicatedCardNode) {
            var tween = cardNode.CreateTween();
            tween.Parallel().TweenProperty(cardNode, "scale", Vector2.One, 0.1f)
                .From(Vector2.Zero)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);

            NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(cardNode);
            cardNode.GlobalPosition = PileType.Play.GetTargetPosition(cardNode);
            cardNode.UpdateVisuals(PileType.Play, CardPreviewMode.Normal);
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
