using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MinionLib.Minion;
using MinionLib.Powers;
using VYgo.Core;
using VYgo.RitsuAdapters;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.CyberDragon;

namespace VYgo.Scripts.Monsters;

public abstract class BaseMonster: ModMinionTemplate, IYgoId
{
    public override int MinInitialHp => 1; // 作为敌方方怪物生成时的血量，通常无需在意
    public override int MaxInitialHp => 1; // 作为敌方方怪物生成时的血量，通常无需在意
    public override string? CustomVisualsPath => $"res://VYgo/scenes/monsters/{CardId}.tscn";

    //防止多次死亡结算
    protected bool PileSent;
    public virtual bool IsGuardian {
        get;
        set;
    } = true;

    // 召唤时执行的代码，通常用来设置血量、应用初始能力等，options 是在召唤随从时传入的参数
    public override async Task OnSummon(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options) // 注意使用 self 而非 this
    {
        PileSent = false;
        if (options.MaxHp is { } maxHp)
            await CreatureCmd.SetMaxAndCurrentHp(Creature, maxHp); // 设置血量
        if (IsGuardian)
            await PowerCmd.Apply<MinionGuardianPower>(choiceContext, Creature, 1m, owner.Creature, options.Source);
        if (options.PrimaryStatAmount is { } strength && strength > 0m)
            await PowerCmd.Apply<StrengthPower>(choiceContext, Creature, strength, owner.Creature, options.Source);
    }

    public abstract int CardId { get; }

    public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength) {
        //怪兽死亡后，对应的怪兽卡置入弃牌堆
        if (!PileSent && creature == Creature) {
            Entry.Logger.Info("AfterDeath:" + GetType().Name);
            var card = this.YgoGetCard();
            if (card != null) {
                var owner = creature.PetOwner;
                if (owner != null) {
                    TaskHelper.RunSafely(ReturnCard(owner, card));
                }
            }
            else {
                Entry.Logger.Error("ReturnCardError: No Card found for " + GetType().Name);
            }
            PileSent = true;
        }
        return base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
    }

    private async Task ReturnCard(Player player, BaseVYgoCard card) {
        await CardPileCmd.AddGeneratedCardToCombat(CombatState.CreateCard(card, player), PileType.Discard,  player);
        // CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(CombatState.CreateCard(card, player), PileType.Discard,  player), 0f);
    }
}
