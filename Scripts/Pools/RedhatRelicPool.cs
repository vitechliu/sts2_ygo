using STS2RitsuLib.Scaffolding.Content;

namespace VYgo.Scripts.Pools;

public class RedhatRelicPool : TypeListRelicPoolModel
{
    // 描述中使用的能量图标。大小为24x24。
    public override string? TextEnergyIconPath => "res://VYgo/images/energy.png";
    // tooltip和卡牌左上角的能量图标。大小为74x74。
    public override string? BigEnergyIconPath => "res://VYgo/images/energy_big.png";

    public override string EnergyColorName => "test";
}