using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;
using VYgo.Core.Summon;
using VYgo.Scripts.Monsters;
using VYgo.Utils;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
[RegisterCharacterStarterCard(typeof(PlaymakerCharacter), 1)]
public class BackupSecretary() : BaseMonsterCard(1, CardType.Attack, CardRarity.Basic, TargetType.None) {
    public override int CardId => 63528891;

    public override int BaseAttackVar => 3;
    public override int BaseLifeVar => 2;
    public override int UpgradeAttackVar => 2;

    protected override bool ShouldGlowGoldInternal => CanSpecialSummon;

    private bool CanSpecialSummon => Owner.Creature.Pets.Any(
        pet => pet.Monster is BaseMonster monster && monster.YgoGetCore().IsRace(YgoRace.Cyberse)
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.SpecialSummon(),
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
}
