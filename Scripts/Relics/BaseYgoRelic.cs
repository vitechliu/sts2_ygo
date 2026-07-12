using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Scaffolding.Content;

namespace VYgo.Scripts.Relics;

public abstract class BaseYgoRelic: ModRelicTemplate {
    
    // 遗物的数值。这里会替换本地化中的{Cards}。
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    public override RelicAssetProfile AssetProfile => new(
        // 小图标（原版85x85）
        IconPath: $"res://VYgo/images/relics/{GetType().Name}.png",
        // 轮廓图标（原版85x85）
        IconOutlinePath: $"res://VYgo/images/relics/{GetType().Name}.png",
        // 大图标（原版256x256）
        BigIconPath: $"res://VYgo/images/relics/{GetType().Name}.png"
    );
}