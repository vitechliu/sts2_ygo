using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.Patches;
using VYgo.Scripts.Cards.Category.ZaneTruesdale;

namespace VYgo.Scripts.Powers;

// 沿用原版临时减力量，在受影响敌人的回合结束时恢复。
[RegisterPower]
public sealed class ChimeratechRampageDragonStrengthPower : TemporaryStrengthPower, IModPowerAssetOverrides {
    public override AbstractModel OriginModel => ModelDb.Card<ChimeratechRampageDragon>();
    protected override bool IsPositive => false;
    public PowerAssetProfile AssetProfile => new(IconPath: CustomIconPath, BigIconPath: CustomBigIconPath);
    public string? CustomIconPath => "res://images/powers/strength_power.png";
    public string? CustomBigIconPath => CustomIconPath;
}
