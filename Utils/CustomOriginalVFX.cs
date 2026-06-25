
using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace VYgo.Utils;

public static class CustomOriginalVFX {
    public static void PlayLinkSummon(Vector2 pos) {
        var original = NPowerAppliedBuffVfx.Create(pos);
        if (original == null) return;
        foreach (var p in original._particles) {
            if (p.Name != "vfx_common_glow") {
                p.SelfModulate = new Color(1.791f, 0.555f, 1.247f);
            }
        }
        NCombatRoom.Instance?.CombatVfxContainer.AddChild(original);
    }
}
