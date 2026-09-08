using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Cards.Category.Playmaker;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Relics.Starters;

[RegisterRelic(typeof(PlaymakerRelicPool))]
[RegisterCharacterStarterRelic(typeof(PlaymakerCharacter))]
public class StormAccessRelic : BaseYgoRelic {
    private bool _usedThisCombat;
    private bool UsedThisCombat {
        get => _usedThisCombat;
        set {
            AssertMutable();
            _usedThisCombat = value;
        }
    }

    public override RelicRarity Rarity => RelicRarity.Starter;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1), new DynamicVar("HpThreshold", 50)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<StormAccess>()];

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource) {
        if (UsedThisCombat || !CombatManager.Instance.IsInProgress
            || target != Owner.Creature || !target.IsAlive || result.UnblockedDamage <= 0
            || target.CurrentHp * 100m >= target.MaxHp * DynamicVars["HpThreshold"].BaseValue) return;

        // 在生成卡牌前消耗次数，避免后续结算重入导致重复触发。
        UsedThisCombat = true;
        Flash();
        for (int i = 0; i < DynamicVars.Cards.BaseValue; i++) {
            var card = Owner.Creature.CombatState!.CreateCard<StormAccess>(Owner);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
        }
    }

    public override Task AfterCombatEnd(CombatRoom room) {
        UsedThisCombat = false;
        return Task.CompletedTask;
    }
}
