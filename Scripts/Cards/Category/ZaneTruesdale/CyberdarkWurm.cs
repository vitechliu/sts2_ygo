using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;
using VYgo.Core.Summon;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberdarkWurm() : BaseMonsterCard(2, CardRarity.Rare, TargetType.None) {
    public override int CardId => 56100345;

    public override int BaseAttackVar => 3;
    public override int BaseLifeVar => 8;
    public override int UpgradeAttackVar => 1;
    public override int UpgradeLifeVar => 1;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.EnterField(),
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) {
        var summonedCreature = await SummonMonster(
            choiceContext,
            cardPlay,
            new SummonContext(IsSpecialSummon: cardPlay.IsAutoPlay));
        if (summonedCreature == null) return;

        await SelectAddAndUpgrade(
            choiceContext,
            PileType.Draw,
            IsCyberDragonMonster);
        await SelectAddAndUpgrade(
            choiceContext,
            PileType.Discard,
            IsCyberSpellOrTrap);
    }

    private async Task SelectAddAndUpgrade(
        PlayerChoiceContext choiceContext,
        PileType pileType,
        Func<CardModel, bool> filter) {
        CardModel? selectedCard = (await CardSelectCmd.FromCombatPile(
                prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
                context: choiceContext,
                pile: pileType.GetPile(Owner),
                player: Owner,
                filter: filter))
            .FirstOrDefault();
        if (selectedCard == null) return;

        await CardPileCmd.Add(selectedCard, PileType.Hand);
        CardCmd.Upgrade(selectedCard);
    }

    private static bool IsCyberDragonMonster(CardModel card) {
        return card is BaseMonsterCard monsterCard
            && monsterCard.ContainArchetype(YgoArchetypes.CyberDragon);
    }

    private static bool IsCyberSpellOrTrap(CardModel card) {
        return card is BaseVYgoCard ygoCard
            && ygoCard.ContainArchetype(YgoArchetypes.Cyber)
            && ygoCard.YgoCardType is YgoType.spell or YgoType.trap;
    }
}
