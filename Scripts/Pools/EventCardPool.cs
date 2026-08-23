using STS2RitsuLib.Interop.AutoRegistration;

namespace VYgo.Scripts.Pools;

[RegisterSharedCardPool]
public class EventCardPool : BaseYgoCommonCardPool {
    public override string Title => "vygo_event";

    public override bool IsColorless => true;
}