using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
namespace VYgo.Scripts.Cards;

/// <summary>主卡组仪式怪兽，不能通常打出；仪式和其他特殊召唤均使用原有来源卡生命周期。</summary>
public abstract class BaseRitualMonsterCard(int cost, CardRarity rarity)
    : BaseMonsterCard(cost, CardType.Attack, rarity, TargetType.None)
{
    private bool _ritualResolution;
    protected override VYgo.Core.YgoType CardYgoType => VYgo.Core.YgoType.ritual;
    protected override bool IsPlayable => false;
    internal async Task<Creature?> ResolveRitual(PlayerChoiceContext context)
    {
        if (_ritualResolution) throw new InvalidOperationException("同一仪式怪兽正在结算。");
        _ritualResolution = true;
        var hand = NCombatRoom.Instance?.Ui.Hand;
        var holder = hand?.GetCardHolder(this);
        var cardNode = holder?.CardNode;
        bool wasVisible = holder?.Visible == true;
        if (GodotObject.IsInstanceValid(holder)) holder!.Visible = false;
        try
        {
            return await AutoPlayAndCaptureSummonedCreature(context, null, skipCardPileVisuals: true,
            playSummonCardFly: false, playMonsterSummonVfx: false);
        }
        finally
        {
            // 跳过牌堆演出会同时静默移除手牌模型；只清理本张卡的 Holder，避免留下可悬起的残影。
            if (GodotObject.IsInstanceValid(holder))
            {
                if (Pile?.Type == PileType.Hand) holder!.Visible = wasVisible;
                else
                {
                    if (GodotObject.IsInstanceValid(hand)) hand!.RemoveCardHolder(holder!);
                    else holder!.QueueFree();
                    if (GodotObject.IsInstanceValid(cardNode)) cardNode!.QueueFree();
                }
            }
            _ritualResolution = false;
        }
    }
}
