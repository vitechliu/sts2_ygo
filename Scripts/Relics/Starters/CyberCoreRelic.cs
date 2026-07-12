using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Relics.Starters;

[RegisterRelic(typeof(ZaneTruesdaleRelicPool))]
[RegisterCharacterStarterRelic(typeof(ZaneTruesdaleCharacter))]
public class CyberCoreRelic: BaseYgoRelic {
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];
    public override RelicRarity Rarity => RelicRarity.Starter;
}