using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(RedhatCardPool))]
[RegisterCharacterStarterCard(typeof(RedhatCharacter), 1)]
public class MonsterReborn()
    : BaseSpellCard(0, CardType.Skill, CardRarity.Basic, TargetType.None) {
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.Summon()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
        if ((await CardSelectCmd.FromCombatPile(
                prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
                context: choiceContext,
                pile: PileType.Discard.GetPile(Owner),
                player: Owner,
                filter: model => model is BaseMonsterCard ))
            .FirstOrDefault() is not { } selectedExtraCard) {
            return;
        }

        await CardCmd.AutoPlay(choiceContext, selectedExtraCard, null);
    }

    public override int CardId => 83764718;
}
