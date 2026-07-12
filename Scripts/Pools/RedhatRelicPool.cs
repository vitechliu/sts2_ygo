using STS2RitsuLib.Scaffolding.Content;

namespace VYgo.Scripts.Pools;

public class RedhatRelicPool : TypeListRelicPoolModel
{
    public override string? TextEnergyIconPath => "res://VYgo/images/energy.png";
    public override string? BigEnergyIconPath => "res://VYgo/images/energy_big.png";
    public override string EnergyColorName => "ygo";
}