using Godot;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace VYgo.Scripts.Pools;

public abstract class BaseYgoCardPool : TypeListCardPoolModel {
    public override string EnergyColorName => Title;
    // 描述中使用的能量图标。大小为24x24。
    public override string? TextEnergyIconPath => "res://VYgo/images/energy_icon.png";
    // // tooltip和卡牌左上角的能量图标。大小为74x74。
    public override string? BigEnergyIconPath => "res://VYgo/images/energy_star_big.png";

    // 卡池的主题色。
    public override Color DeckEntryCardColor => new(1, 1, 1f);
    // 能量表盘文字轮廓颜色
    public override Color EnergyOutlineColor => new(0.1f, 0.1f, 0.5f);

    private static readonly Material? _poolFrameMaterial = MaterialUtils.CreateUnmodulatedHsvShaderMaterial();
    public override Material? PoolFrameMaterial => _poolFrameMaterial;

    // 卡池是否是无色。例如事件、状态等卡池就是无色的。
    public override bool IsColorless => false;
}