using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;
using VYgo.Scripts.Powers;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class TherionKingRegulus()
    : BaseMonsterCard(2, CardRarity.Rare, TargetType.None) {
    public override int CardId => 10604644;

    public override int BaseAttackVar => 8;
    public override int BaseLifeVar => 4;
    public override int UpgradeAttackVar => 3;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new PowerVar<NegatingPower>(1m),
        new EnergyVar(2)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.EnterField(),
        HoverTipFactory.FromPower<NegatingPower>(),
        EnergyHoverTip
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) {
        var summonedCreature = await SummonMonster(choiceContext, cardPlay);
        if (summonedCreature == null) return;

        CardModel? selectedCard = (await CardSelectCmd.FromCombatPile(
                prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
                context: choiceContext,
                pile: PileType.Discard.GetPile(Owner),
                player: Owner,
                filter: IsMachineMonster))
            .FirstOrDefault();
        if (selectedCard == null
            || !await EquipCmd.EquipFromPile(
                choiceContext,
                selectedCard,
                summonedCreature)) {
            return;
        }

        await PowerCmd.Apply<NegatingPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["NegatingPower"].BaseValue,
            summonedCreature,
            this);
        await PlayerCmd.GainEnergy(
            DynamicVars.Energy.IntValue,
            Owner);
    }

    private static bool IsMachineMonster(CardModel card) {
        return card is BaseMonsterCard monsterCard
            && monsterCard.YgoGetCore()?.Race == "机械族";
    }
}
