using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Core.Summon;
using VYgo.Scripts.Cards.Category.Fusion;
using VYgo.Scripts.Pools;
using VYgo.Utils;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberJormungardr() : BaseMonsterCard(2, CardType.Skill, CardRarity.Rare, TargetType.None) {
    public override int CardId => 19715246;

    public override int BaseAttackVar => 3;
    public override int BaseLifeVar => 7;
    public override int UpgradeAttackVar => 3;

    protected override bool ShouldGlowGoldInternal => CanSpecialSummon;

    private bool CanSpecialSummon => Owner.MinionCount() == 0;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.SpecialSummon(),
        YgoHoverTipConst.EnterField(),
        YgoHoverTipConst.Action(),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.FromCard<Polymerization>()
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay
    ) {
        Creature? summonedCreature = await SummonMonster(
            choiceContext,
            cardPlay,
            new SummonContext(IsSpecialSummon: CanSpecialSummon)
        );
        if (summonedCreature is not { IsAlive: true }
            || Owner.MinionCount() >= Owner.GetMaxMinionCount()) {
            return;
        }

        CardModel? selectedCard = (await CardSelectCmd.FromCombatPile(
                prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
                context: choiceContext,
                pile: PileType.Draw.GetPile(Owner),
                player: Owner,
                filter: IsCyberDragonMonster))
            .FirstOrDefault();
        if (selectedCard != null) {
            await CardCmd.AutoPlay(choiceContext, selectedCard, null);
        }
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

    private static bool IsCyberDragonMonster(CardModel card) {
        return card is BaseMonsterCard monsterCard
            && !monsterCard.IsExtra
            && monsterCard.ContainArchetype(YgoArchetypes.CyberDragon);
    }
}
