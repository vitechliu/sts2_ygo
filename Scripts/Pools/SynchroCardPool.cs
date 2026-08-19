using STS2RitsuLib.Interop.AutoRegistration;

namespace VYgo.Scripts.Pools;

[RegisterSharedCardPool]
public class SynchroCardPool : BaseYgoCommonCardPool {
    public override string Title => "vygo_synchro";
}