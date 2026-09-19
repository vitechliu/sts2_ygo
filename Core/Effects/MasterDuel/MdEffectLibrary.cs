using Godot;
using System.Text.Json;

namespace VYgo.Core.Effects.MasterDuel;

public static class MdEffectLibrary {
    public const string Root = "res://VYgo/scenes/vfx/master_duel/";
    public static string[] Ids {
        get {
            using var document=JsonDocument.Parse(GD.Load<MdSourceData>(Root+"catalog.tres").DataJson);
            return document.RootElement.EnumerateArray().Select(e=>e.GetProperty("id").GetString()!).ToArray();
        }
    }
    public static NMdEffect2D Create(string id) {
        if (!Ids.Contains(id)) throw new ArgumentException("未知的已迁移特效。", nameof(id));
        return GD.Load<PackedScene>(Root + id + ".tscn").Instantiate<NMdEffect2D>();
    }

}
