using MegaCrit.Sts2.Core.Entities.Relics;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Relics.Starters;

[RegisterRelic(typeof(ZaneTruesdaleRelicPool))]
[RegisterCharacterStarterRelic(typeof(ZaneTruesdaleCharacter))]
public class CyberCoreRelic: BaseYgoRelic {
    public override RelicRarity Rarity => RelicRarity.Starter;
}