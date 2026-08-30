using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Minion;
using VYgo.Scripts.Cards.Category.YgoEvent;

namespace VYgo.Scripts.Monsters.YGO;

public class InvokedCocytusMinion : BaseMonster {
    public override int CardId => 85908279;

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay
    ) {
        return target == Creature && amount > 0m ? 0.5m : 1m;
    }

    public override async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options
    ) {
        if (options.Source is not InvokedCocytus sourceCard) return;

        await PowerCmd.Apply<ThornsPower>(
            choiceContext,
            Creature,
            sourceCard.Thorns,
            owner.Creature,
            sourceCard);
    }
}
