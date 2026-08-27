using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;
using VYgo.Core.Summon;
using VYgo.Scripts.Monsters;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class LinkInfraFlier() : BaseMonsterCard(1, CardType.Skill, CardRarity.Common, TargetType.None) {
    public override int CardId => 65100616;

    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 4;
    public override int UpgradeLifeVar => 2;

    protected override bool ShouldGlowGoldInternal => CanSpecialSummon;

    private bool CanSpecialSummon => Owner.Creature.Pets.Any(
        pet => pet.Monster is BaseMonster { SourceCard: BaseExtraLinkCard });

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.SpecialSummon()
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
}
