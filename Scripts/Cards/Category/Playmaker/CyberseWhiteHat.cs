using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using VYgo.Core;
using VYgo.Core.Summon;
using VYgo.Scripts.Monsters;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class CyberseWhiteHat() : BaseMonsterCard(2, CardRarity.Uncommon, TargetType.None) {
    public override int CardId => 46104361;

    public override int BaseAttackVar => 5;
    public override int BaseLifeVar => 7;

    public int Weak => DynamicVars["WeakPower"].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new PowerVar<WeakPower>(2m)
    ];

    protected override bool ShouldGlowGoldInternal => CanSpecialSummon;

    private bool CanSpecialSummon => Owner.Creature.Pets
        .Select(pet => pet.Monster as BaseMonster)
        .Where(monster => monster?.SourceCard is BaseMonsterCard)
        .Select(monster => monster!.YgoGetCore().Race)
        .Where(race => !string.IsNullOrWhiteSpace(race))
        .GroupBy(race => race)
        .Any(group => group.Count() >= 2);

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.SpecialSummon(),
        HoverTipFactory.FromPower<WeakPower>()
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay
    ) {
        await SummonMonster(
            choiceContext,
            cardPlay,
            new SummonContext(IsSpecialSummon: CanSpecialSummon));
    }

    public override bool TryModifyEnergyCostInCombat(
        CardModel card,
        decimal originalCost,
        out decimal modifiedCost
    ) {
        modifiedCost = originalCost;
        if (card != this || !CanSpecialSummon) return false;
        modifiedCost = 0m;
        return true;
    }

    protected override void OnUpgrade() {
        base.OnUpgrade();
        DynamicVars["WeakPower"].UpgradeValueBy(1m);
    }
}
