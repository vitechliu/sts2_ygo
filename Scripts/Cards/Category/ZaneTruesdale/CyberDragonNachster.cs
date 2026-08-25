using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;
using VYgo.Utils;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberDragonNachster() : BaseMonsterCard(1, CardRarity.Uncommon, TargetType.None) {
    public override int CardId => 1142880;

    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 1;
    public override int UpgradeAttackVar => 1;
    public override int UpgradeLifeVar => 1;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.EnterField(),
        YgoHoverTipConst.SpecialSummon(),
        YgoHoverTipConst.NameAs(YgoMaterialNames.电子龙)
    ];
    
    public override YgoMaterialNames? MaterialCardName => YgoMaterialNames.电子龙;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        await base.OnPlay(choiceContext, cardPlay);

        if (Owner.MinionCount() >= MinionUtil.MaxMinionCount) {
            return;
        }

        CardModel? selectedCard = (await CardSelectCmd.FromCombatPile(
                prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
                context: choiceContext,
                pile: PileType.Discard.GetPile(Owner),
                player: Owner,
                filter: IsMachineMonster))
            .FirstOrDefault();
        if (selectedCard != null) {
            await CardCmd.AutoPlay(choiceContext, selectedCard, null);
        }
    }

    private static bool IsMachineMonster(CardModel card) {
        return card is BaseMonsterCard monsterCard
            && monsterCard.YgoGetCore().IsRace(YgoRace.Machine);
    }
}
