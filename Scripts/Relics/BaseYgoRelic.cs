using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Scaffolding.Content;

namespace VYgo.Scripts.Relics;

public class BaseYgoRelic: ModRelicTemplate {
    public override RelicRarity Rarity => RelicRarity.Starter;
    
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