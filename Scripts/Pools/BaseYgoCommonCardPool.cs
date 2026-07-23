using Godot;
using STS2RitsuLib.Utils;

namespace VYgo.Scripts.Pools;

//通用的卡池，如融合通用辅助，连接通用等，多个角色可共用的
public abstract class BaseYgoCommonCardPool : BaseYgoCardPool {
    public override bool IsColorless => true;
}