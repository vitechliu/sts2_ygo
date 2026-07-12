using Godot;

namespace VYgo.Scripts.Pools;

public class ZaneTruesdaleCardPool : BaseYgoCharacterCardPool {
    // 卡池的ID。必须唯一防撞车。
    public override string Title => "vygo_zane_truesdale";
    
    public override string? TextEnergyIconPath => "res://VYgo/images/zane_truesdale/energy_icon.png";
    public override string? BigEnergyIconPath => "res://VYgo/images/zane_truesdale/energy_icon_big.png";
    public override string EnergyColorName => "ygo_zane_truesdale";

    // 能量表盘文字轮廓颜色
    public override Color EnergyOutlineColor => new(0.1f, 0.3f, 0.5f);
}