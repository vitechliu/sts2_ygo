using STS2RitsuLib.Interop.AutoRegistration;

namespace VYgo.Scripts.Pools;

[RegisterSharedCardPool]
public class MachineCardPool : BaseYgoCommonCardPool {
    public override string Title => "vygo_race_machine";

    public override bool IsColorless => true;
}