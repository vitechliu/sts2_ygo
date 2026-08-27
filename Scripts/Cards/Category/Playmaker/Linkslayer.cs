using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Core.Summon;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Var;
using VYgo.Utils;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
[RegisterCharacterStarterCard(typeof(PlaymakerCharacter), 1)]
public class Linkslayer() : BaseMonsterCard(2, CardType.Attack, CardRarity.Basic, TargetType.None) {
    public override int CardId => 35595518;

    public override int BaseAttackVar => 5;
    public override int BaseLifeVar => 1;
    public int Damage => DynamicVars.Damage.IntValue;
    public int Weak => DynamicVars["WeakPower"].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new DamageVar(9m, ValueProp.Move),
        new PowerVar<WeakPower>(2m)
    ];

    protected override bool ShouldGlowGoldInternal => CanSpecialSummon;

    private bool CanSpecialSummon => Owner.MinionCount() == 0;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.SpecialSummon(),
        YgoHoverTipConst.Action()
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay
    ) {
        await SummonMonster(
            choiceContext,
            cardPlay,
            new SummonContext(IsSpecialSummon: CanSpecialSummon)
        );
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
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
