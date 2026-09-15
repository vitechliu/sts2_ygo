using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using VYgo.Core.Effects;
namespace VYgo.Core;

internal static class RitualSummonAnimations
{
    internal const string ScenePath = "res://VYgo/scenes/summon/ritual/ritual_summon_2d.tscn";
    internal static readonly Color Accent = new("419cff");
    internal static async Task Play(CardModel target, IReadOnlyList<CardModel> materials, bool slowExit = false)
    {
        var room = NCombatRoom.Instance;
        if (room == null) throw new InvalidOperationException("仪式演出需要有效战斗房间。");
        var stage = GD.Load<PackedScene>(ScenePath).Instantiate<NRitualSummon2D>();
        room.CombatVfxContainer.AddChild(stage); stage.GlobalPosition = room.GetViewportRect().Size * 0.5f;
        try
        {
            await Card3DEffectUtil.RunMultipleCard3DEffect(materials.Take(6).Append(target),
                (cards, _) => stage.Manager.Play(cards, Math.Min(6, materials.Count), slowExit), stage.GlobalPosition,
                scaleMultiplier: 1.02f, horizontalSpacing: 0, initialOpacity: 0, hideSourceNodes: false, hideCardShadow: true);
        }
        finally { if (GodotObject.IsInstanceValid(stage)) stage.QueueFree(); }
    }
}
