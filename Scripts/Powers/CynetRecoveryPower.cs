using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using VYgo.Core;
using VYgo.Core.Hooks;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.Playmaker;
using VYgo.Scripts.Monsters;

namespace VYgo.Scripts.Powers;

[RegisterPower]
public class CynetRecoveryPower : ModPowerTemplate, IMonsterBattleDestroyedHookListener {
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://VYgo/images/cards/73558460.png",
        BigIconPath: "res://VYgo/images/cards/73558460.png");

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<CynetRecovery>(),
        YgoHoverTipConst.BattleDestroyed()
    ];

    public async Task AfterMonsterBattleDestroyed(
        PlayerChoiceContext choiceContext,
        Creature destroyedCreature,
        Creature source) {
        if (Owner.Player is not { } player
            || destroyedCreature.PetOwner != player
            || destroyedCreature.Monster is not BaseMonster { SourceCard: BaseMonsterCard card }
            || !card.YgoGetCore().IsRace(YgoRace.Cyberse)) {
            return;
        }

        Flash();
        await PowerCmd.Apply<EnergyNextTurnPower>(
            choiceContext,
            Owner,
            Amount,
            Owner,
            card);
    }
}
