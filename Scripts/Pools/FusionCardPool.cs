using STS2RitsuLib.Interop.AutoRegistration;

namespace VYgo.Scripts.Pools;

[RegisterSharedCardPool]
public class FusionCardPool : BaseYgoCommonCardPool {
    // 卡池的ID。必须唯一防撞车。
    public override string Title => "vygo_fusion";
}