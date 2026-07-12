using STS2RitsuLib.Interop.AutoRegistration;

namespace VYgo.Scripts.Pools;

[RegisterSharedCardPool]
public class CommonCardPool : BaseYgoCommonCardPool {
    public override string Title => "vygo_common";

    public override bool IsColorless => true;
}