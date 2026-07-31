using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;
using VYgo.Core.Hooks;
using VYgo.Core.History;
using VYgo.Core.Summon;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberDinosaur()
    : BaseMonsterCard(5, CardRarity.Common, TargetType.None), IMonsterSummonHookListener {
    public override int CardId => 39439590;

    public override int BaseAttackVar => 7;
    public override int BaseLifeVar => 5;
    public override int UpgradeAttackVar => 2;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.SpecialSummon()
    ];

    public override Task AfterCardEnteredCombat(CardModel card) {
        if (card != this || IsClone) return Task.CompletedTask;

        int specialSummonCount = CombatManager.Instance.History.Entries
            .OfType<SpecialSummonEntry>()
            .Count(entry => entry.Player == Owner);
        EnergyCost.AddThisCombat(-specialSummonCount);
        return Task.CompletedTask;
    }

    public Task AfterMonsterSummon(
        PlayerChoiceContext choiceContext,
        BaseMonsterCard card,
        CardPlay cardPlay,
        Creature summonedCreature,
        SummonContext summonContext) {
        if (summonContext.IsSpecialSummon && cardPlay.Player == Owner) {
            EnergyCost.AddThisCombat(-1);
        }
        return Task.CompletedTask;
    }
}
