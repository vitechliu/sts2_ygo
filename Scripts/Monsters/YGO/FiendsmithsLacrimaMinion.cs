using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Minion;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.Common;
using VYgo.Utils;

namespace VYgo.Scripts.Monsters.YGO;

public class FiendsmithsLacrimaMinion: BaseMonster {
    public override int CardId => 46640168;

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options
    ) {
        if (options.Source is not FiendsmithsLacrima sourceCard
            || owner.MinionCount() >= owner.GetMaxMinionCount()) {
            return;
        }

        BaseMonsterCard? selected = (await CardSelectCmd.FromCombatPile(
                choiceContext,
                PileType.Discard.GetPile(owner),
                owner,
                new CardSelectorPrefs(sourceCard.SelectionScreenPrompt, 1),
                FiendsmithUtil.IsLightFiendMonster))
            .OfType<BaseMonsterCard>()
            .FirstOrDefault();
        if (selected != null && owner.MinionCount() < owner.GetMaxMinionCount()) {
            await selected.AutoPlayAndCaptureSummonedCreature(choiceContext, null);
        }
    }

    protected override async Task OnSendToGraveyard(
        PlayerChoiceContext choiceContext,
        Creature creature,
        Player owner
    ) {
        if (SourceCard is not FiendsmithsLacrima sourceCard) return;

        await CreatureCmd.Damage(
            choiceContext,
            creature.CombatState.Creatures.Where(target => !target.IsPet).ToList(),
            sourceCard.GraveyardDamage,
            ValueProp.Unpowered,
            creature,
            sourceCard,
            null);
    }
}
