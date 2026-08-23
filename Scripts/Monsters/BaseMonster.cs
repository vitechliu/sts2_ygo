using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MinionLib.Minion;
using VYgo.Core;
using VYgo.Core.Hooks;
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

    public CardModel? SourceCard { get; private set; }

    public bool Upgraded => _upgraded;
    public virtual void SetUpgraded() {
        _upgraded = true;
        Entry.Logger.Info("SetUpgraded:" + Title);
    }

    public int? Level {
        get {
            if (this.YgoGetCore() is not { HasLevel: true } coreCard) return null;
            return Creature?.GetPower<MonsterLevelPower>()?.Amount ?? coreCard.Level;
        }
    }

    protected NMonsterVisuals? Visuals => Creature?.GetCreatureNode()?.Visuals as NMonsterVisuals;
    
    //防止多次死亡结算
    protected bool PileSent { get; private set; }

    internal bool TryReserveSourceCardAsSummonMaterial(CardModel card) {
        if (PileSent || SourceCard != card || Creature is not { IsAlive: true }) return false;
        PileSent = true;
        return true;
    }

    internal void CancelSourceCardMaterialReservation(CardModel card) {
        if (SourceCard == card && Creature is { IsAlive: true }) {
            PileSent = false;
        }
    }

    internal async Task<bool> SendReservedSourceCardAsSummonMaterial(
        PlayerChoiceContext choiceContext,
        Player owner,
        PileType destination
    ) {
        if (!PileSent || SourceCard is not { } card || card.Owner != owner) return false;

        if (destination == PileType.Exhaust) {
            await CardCmd.Exhaust(choiceContext, card);
        }
        else {
            CardPile destinationPile = destination.GetPile(owner);
            CardPileAddResult result = await CardPileCmd.Add(card, destinationPile);
            if (!result.success) return false;
            destinationPile.InvokeContentsChanged();
        }

        if (card.Pile?.Type != destination) return false;
        if (destination == PileType.Discard && Creature is { } creature) {
            await OnSendToGraveyard(choiceContext, creature, owner);
        }

        return true;
    }

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
        if (power != null) {
            power.IsGuardian = IsGuardian;
            if (card != null) {
                power.Card = card;
                power.InitInfo();
            }
        }
        if (options.PrimaryStatAmount is { } strength && strength > 0m)
            await PowerCmd.Apply<AttackPower>(choiceContext, Creature, strength, owner.Creature, options.Source);
        if (BasicAttackAction) {
            await PowerCmd.Apply<TargetingAttackAction>(choiceContext, Creature, 1m, owner.Creature, options.Source, true);
        }
        await OnSummonYgo(choiceContext, owner, options);
        if (card?.ContainArchetype(YgoArchetypes.CyberDragon) == true
            && !owner.Creature.HasPower<CyberDragonSummonedThisTurnPower>()) {
            await PowerCmd.Apply<CyberDragonSummonedThisTurnPower>(
                choiceContext,
                owner.Creature,
                1m,
                Creature,
                options.Source,
                true);
        }
    }

    public abstract int CardId { get; }

    protected virtual async Task OnSendToGraveyard(
        PlayerChoiceContext choiceContext, 
        Creature creature,
        Player owner
        ) {}

    //作为连接素材送墓后触发（连接召唤成功时，由 SummonUtil 统一调用）
    public virtual async Task OnUsedAsLinkMaterial(
        PlayerChoiceContext choiceContext,
        Player owner,
        IReadOnlyList<SummonMaterial> materials) {
        await Task.CompletedTask;
    }

    //超量素材挂载完成后触发（超量召唤成功时，由 XyzMaterialCmd 统一调用）
    public virtual async Task OnXyzMaterialsAttached(
        PlayerChoiceContext choiceContext,
        Player owner,
        IReadOnlyList<CardModel> materials) {
        await Task.CompletedTask;
    }

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength) {
        //怪兽死亡后，对应的怪兽卡置入弃牌堆
        if (creature == Creature) {
            if (!wasRemovalPrevented && !PileSent) {
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

            await MonsterBattleDestroyedHook.AfterMonsterDeath(
                choiceContext,
                creature,
                wasRemovalPrevented);
        }
        await base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
    }

    public virtual async Task AfterAttack(PlayerChoiceContext choiceContext) { }

    private async Task ReturnCard(Player player, PlayerChoiceContext choiceContext) {
        if (SourceCard == null) return;
        if (CombatManager.Instance?.IsOverOrEnding != false) return;
        var discardPile = PileType.Discard.GetPile(player);
        await CardPileCmd.Add(SourceCard, discardPile);
        discardPile.InvokeContentsChanged();
    }
}
