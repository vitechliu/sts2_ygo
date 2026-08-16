using Godot;

namespace VYgo.Scripts.Pools;

public class PlaymakerCardPool : BaseYgoCharacterCardPool {
    public override string Title => "vygo_playmaker";

    public override string? TextEnergyIconPath => "res://VYgo/images/playmaker/energy_icon.png";
    public override string? BigEnergyIconPath => "res://VYgo/images/playmaker/energy_icon_big.png";
    public override string EnergyColorName => "ygo_playmaker";

    public override Color EnergyOutlineColor => new(0.1f, 0.24f, 0.4f);
}
