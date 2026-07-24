using STS2RitsuLib.Scaffolding.Content;

namespace VYgo.Scripts.Pools;

public abstract class BaseYgoRelicPool : TypeListRelicPoolModel {
    public override string? TextEnergyIconPath => "res://VYgo/images/energy_icon.png";
    public override string? BigEnergyIconPath => "res://VYgo/images/energy_test_big.png";
    public override string EnergyColorName => "ygo";
}