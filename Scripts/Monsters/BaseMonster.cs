using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MinionLib.Minion;
using MinionLib.Powers;
using VYgo.Core;
using VYgo.RitsuAdapters;
using VYgo.Scripts.Actions;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Monsters;

public abstract class BaseMonster: ModMinionTemplate, IYgoId
{
    public override int MinInitialHp => 1; // 作为敌方方怪物生成时的血量，通常无需在意
    public override int MaxInitialHp => 1; // 作为敌方方怪物生成时的血量，通常无需在意
    public override string? CustomVisualsPath => $"res://VYgo/scenes/monsters/{CardId}.tscn";

    protected bool _upgraded;

    public CardModel? SourceCard;

    public bool Upgraded => _upgraded;
    public virtual void SetUpgraded() {
        _upgraded = true;
        Entry.Logger.Info("SetUpgraded:" + Title);
    }

    protected NMonsterVisuals? Visuals => Creature?.GetCreatureNode()?.Visuals as NMonsterVisuals;
    
    //防止多次死亡结算
    protected bool PileSent;
    public virtual bool IsGuardian {
        get;
        set;
    } = true;
    
    public virtual bool BasicAttackAction {
        get;
        set;
    } = true;


    public virtual async Task OnSummonYgo(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options) {
        await Task.CompletedTask;
    }
    // 召唤时执行的代码，通常用来设置血量、应用初始能力等，options 是在召唤随从时传入的参数
    public override async Task OnSummon(
        PlayerChoiceContext choiceContext,
        Player owner,
        MinionSummonOptions options) // 注意使用 self 而非 this
    {
        SourceCard = options.Source;
        PileSent = false;
        if (options.MaxHp is { } maxHp)
            await CreatureCmd.SetMaxAndCurrentHp(Creature, maxHp); // 设置血量
        Visuals?.OnSummon();
        var card = this.YgoGetCard();
        var power = await PowerCmd.Apply<YgoPower>(choiceContext, Creature, 1m, owner.Creature, options.Source, true);
        if (power != null && card != null) {
            power.Card = card;
            power.InitInfo();
        }
        if (IsGuardian)
            await PowerCmd.Apply<MinionGuardianPower>(choiceContext, Creature, 1m, owner.Creature, options.Source, true);
        if (options.PrimaryStatAmount is { } strength && strength > 0m)
            await PowerCmd.Apply<AttackPower>(choiceContext, Creature, strength, owner.Creature, options.Source);
        if (BasicAttackAction) {
            await PowerCmd.Apply<TargetingAttackAction>(choiceContext, Creature, 1m, owner.Creature, options.Source, true);
        }
        await OnSummonYgo(choiceContext, owner, options);
    }

    public abstract int CardId { get; }

    protected virtual async Task OnSendToGraveyard(
        PlayerChoiceContext choiceContext, 
        Creature creature,
        Player owner
        ) {}

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength) {
        //怪兽死亡后，对应的怪兽卡置入弃牌堆
        if (!PileSent && creature == Creature) {
            PileSent = true;
            // Entry.Logger.Info("AfterDeath:" + GetType().Name);
            var card = this.YgoGetCard();
            if (card != null) {
                var owner = creature.PetOwner;
                if (owner != null) {
                    await ReturnCard(owner, choiceContext);
                    await OnSendToGraveyard(choiceContext, creature, owner);
                }
            }
            else {
                Entry.Logger.Error("ReturnCardError: No Card found for " + GetType().Name);
            }
        }
        await base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
    }

    public virtual async Task AfterAttack(PlayerChoiceContext choiceContext) { }

    private async Task ReturnCard(Player player, PlayerChoiceContext choiceContext) {
        if (SourceCard == null) return;
        if (CombatManager.Instance.IsOverOrEnding) return;
        var discardPile = PileType.Discard.GetPile(player);
        await CardPileCmd.Add(SourceCard, discardPile);
        discardPile.InvokeContentsChanged();
    }
}
