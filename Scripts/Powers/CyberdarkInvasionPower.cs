using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using VYgo.Core;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.ZaneTruesdale;

namespace VYgo.Scripts.Powers;

[RegisterPower]
public class CyberdarkInvasionPower : ModPowerTemplate {
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://VYgo/images/powers/cyberdark_invasion_power.png",
        BigIconPath: "res://VYgo/images/powers/cyberdark_invasion_power.png"
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<CyberdarkInvasion>(),
        YgoHoverTipConst.Equip(),
    ];

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants) {
        if (side != Owner.Side
            || !participants.Contains(Owner)
            || Owner.Player is not { } player) {
            return;
        }

        List<CardModel> equipment = EquipCmd.GetAllEquipment(player).ToList();
        if (equipment.Count > 0) {
            CardModel? randomEquipment = player.RunState.Rng.CombatCardSelection
                .NextItem(equipment);
            Creature? randomEnemy = player.RunState.Rng.CombatTargets
                .NextItem(Owner.CombatState.HittableEnemies);
            if (randomEquipment == null
                || randomEnemy == null
                || !await EquipCmd.SendToGraveyard(choiceContext, randomEquipment)) {
                return;
            }

            Flash();
            if (randomEnemy.IsAlive) {
                await CreatureCmd.Damage(
                    choiceContext,
                    randomEnemy,
                    Amount,
                    ValueProp.Unpowered,
                    Owner);
            }
            return;
        }

        CardModel? randomCyberdark = player.RunState.Rng.CombatCardSelection.NextItem(
            PileType.Draw.GetPile(player).Cards
                .OfType<BaseMonsterCard>()
                .Where(card => card.ContainArchetype(YgoArchetypes.Cyberdark))
                .ToList());
        if (randomCyberdark == null) return;

        Creature? randomMonster = player.RunState.Rng.CombatTargets.NextItem(
            player.Creature.Pets
                .Where(monster => EquipCmd.CanEquip(randomCyberdark, monster))
                .ToList());
        if (randomMonster == null) return;

        if (await EquipCmd.EquipFromPile(choiceContext, randomCyberdark, randomMonster)) {
            Flash();
        }
    }
}
