using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Core.Hooks;
using VYgo.Core.Summon;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Relics.Starters;

[RegisterRelic(typeof(ZaneTruesdaleRelicPool))]
[RegisterCharacterStarterRelic(typeof(ZaneTruesdaleCharacter))]
public class CyberCoreRelic : BaseYgoRelic, IMonsterSummonHookListener {
    private bool _hasTriggeredThisCombat;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];

    protected override bool IncludeEnergyHoverTip => true;

    public override RelicRarity Rarity => RelicRarity.Starter;

    private bool HasTriggeredThisCombat {
        get => _hasTriggeredThisCombat;
        set {
            AssertMutable();
            _hasTriggeredThisCombat = value;
        }
    }

    public async Task AfterMonsterSummon(
        PlayerChoiceContext choiceContext,
        BaseMonsterCard card,
        CardPlay cardPlay,
        Creature summonedCreature,
        SummonContext summonContext) {
        if (HasTriggeredThisCombat
            || !CombatManager.Instance.IsInProgress
            || cardPlay.Player != Owner
            || !card.ContainArchetype(YgoArchetypes.CyberDragon)) {
            return;
        }

        HasTriggeredThisCombat = true;
        Flash();
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    }

    public override Task AfterCombatEnd(CombatRoom room) {
        HasTriggeredThisCombat = false;
        return Task.CompletedTask;
    }
}
