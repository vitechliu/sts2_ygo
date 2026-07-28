using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberdarkHorn() : BaseMonsterCard(1, CardRarity.Token, TargetType.None) {
    public override int CardId => 41230939;

    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 2;
    public override int UpgradeLifeVar => 2;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        YgoKeywords.Piercing
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.EnterField(),
        YgoHoverTipConst.Equip()
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) {
        var summonedCreature = await SummonMonster(choiceContext, cardPlay);
        if (summonedCreature == null) return;

        BaseMonsterCard? selectedMonster = (await CardSelectCmd.FromCombatPile(
                prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
                context: choiceContext,
                pile: PileType.Discard.GetPile(Owner),
                player: Owner,
                filter: IsMachineMonster))
            .OfType<BaseMonsterCard>()
            .FirstOrDefault();
        if (selectedMonster == null
            || !await EquipCmd.EquipFromPile(
                choiceContext,
                selectedMonster,
                summonedCreature)) {
            return;
        }

        if (selectedMonster.Attack > 0) {
            await PowerCmd.Apply<AttackPower>(
                choiceContext,
                summonedCreature,
                selectedMonster.Attack,
                summonedCreature,
                selectedMonster);
        }
    }

    private static bool IsMachineMonster(CardModel card) {
        return card is BaseMonsterCard monsterCard
            && monsterCard.YgoGetCore()?.Race == "机械族";
    }
}
