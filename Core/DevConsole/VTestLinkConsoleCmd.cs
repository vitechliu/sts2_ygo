using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Debug;
using MegaCrit.Sts2.Core.Runs;
using VYgo.Core.Effects;
using VYgo.Core.Settings;
using VYgo.Scripts;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.Playmaker;
using VYgo.Scripts.Cards.Category.ZaneTruesdale;
using VYgo.Scripts.Monsters;
using VYgo.Scripts.Powers;
using VYgo.Utils;

namespace VYgo.Core.DevConsole;

public sealed partial class VTestConsoleCmd {
    private static readonly string[] LinkCommands = ["link-single", "link-sieger", "link-sieger-slow", "link-sieger-upgraded", "link-minimal", "link-none", "link-prepare"];

    private CmdResult ProcessLink(Player? player,string command) {
        if (!LinkCommands.Contains(command)) return new CmdResult(false,"未知连接测试参数，请查看 vtest help。");
        string? error=ValidateLinkTest(player,command); if(error!=null)return new CmdResult(false,error);
        var run=RunManager.Instance;
        if(_pendingAction!=null&&!_pendingActionStarted&&ReferenceEquals(run.ActionExecutor.CurrentlyRunningAction,_pendingAction)) {
            _pendingActionStarted=true;
            return new CmdResult(ExecuteLink(player!,_pendingAction,command),true,"正在执行正式连接召唤测试。");
        }
        if(_pendingAction!=null||!run.ActionQueueSet.IsEmpty||run.ActionExecutor.IsRunning)
            return new CmdResult(false,"请等待当前动作和选卡完成。");
        var action=new ConsoleCmdGameAction(player!,"vtest "+command,inCombat:true);
        _pendingAction=action;_pendingActionStarted=false;
        try { run.ActionQueueSynchronizer.RequestEnqueue(action);return new CmdResult(ObserveCompletion(action),true,"连接测试已排队。"); }
        catch { _pendingAction=null;throw; }
    }

    private static string? ValidateLinkTest(Player? player,string command) {
        string? error=ValidateBattle(player);if(error!=null)return error;
        int count=command=="link-single"?1:2;
        if(player!.GetMaxMinionCount()-player.MinionCount()<count)return $"测试准备需要 {count} 个随从空位。";
        if(PileType.Hand.GetPile(player).Cards.Count>=CardPile.MaxCardsInHand)return "测试准备需要一个手牌空位。";
        return null;
    }

    private static async Task ExecuteLink(Player player,ConsoleCmdGameAction action,string command) {
        try {
            await Task.Yield();string? error=ValidateLinkTest(player,command);if(error!=null){Report(false,error);return;}
            bool single=command=="link-single",upgraded=command=="link-sieger-upgraded";
            BaseMonsterCard[] canonical=single ? [ModelDb.Card<LinkSpider>(),ModelDb.Card<Bitron>()]
                : [ModelDb.Card<CyberDragonSieger>(),ModelDb.Card<CyberDragon>(),ModelDb.Card<ProtoCyberDragon>()];
            foreach(var card in canonical) {
                if(card.YgoGetCore()==null||card.YgoGetMonster()==null) { Report(false,"测试卡数据或随从模型缺失。");return; }
                foreach(string path in new[]{$"res://VYgo/images/cards/{card.CardId}.png",$"res://VYgo/scenes/monsters/{card.CardId}.tscn"})
                    if(!ResourceLoader.Exists(path)){Report(false,"缺少资源，请发布后重启："+path);return;}
            }
            if(!ResourceLoader.Exists(ExtraDeckSummonAnimations.LinkSummon2DAssets)||!Godot.FileAccess.FileExists(LinkMaterialPreview.SourcePath)) {
                Report(false,"缺少连接演出资源，请发布并重启游戏。");return;
            }
            NDevConsole.Instance?.HideConsole();
            var mode=command=="link-none"?EffectMode.none:command=="link-minimal"?EffectMode.minimal:EffectMode.full;
            await VYgoModSettings.RunWithAnimationForTest(player,mode,async()=>{
                var choice=new GameActionPlayerChoiceContext(action);var combat=player.Creature.CombatState!;
                int before=player.MinionCount();
                BaseExtraLinkCard target=single?combat.CreateCard<LinkSpider>(player):combat.CreateCard<CyberDragonSieger>(player);
                BaseMonsterCard[] materials=single?[combat.CreateCard<Bitron>(player)]:[combat.CreateCard<CyberDragon>(player),combat.CreateCard<ProtoCyberDragon>(player)];
                if(upgraded){target.UpgradeInternal();foreach(var material in materials)material.UpgradeInternal();}
                if(!(await CardPileCmd.Add(target,Entry.ExtraPile.GetPile(player),skipVisuals:true)).success){Report(false,"目标加入额外卡组失败。");return;}
                foreach(var material in materials) {
                    if(!(await CardPileCmd.Add(material,PileType.Hand.GetPile(player),skipVisuals:true)).success){Report(false,"素材加入手牌失败。");return;}
                    var creature=await material.AutoPlayAndCaptureSummonedCreature(choice,null,skipCardPileVisuals:true,playSummonCardFly:false,playMonsterSummonVfx:false);
                    if(creature is not {IsAlive:true}){Report(false,"素材未能登场，已发生的结算保留。");return;}
                }
                var spec=target.CreateDirectExtraDeckSummonSpec(player)!;
                SummonMaterialSelectionSpec Build()=>SummonUtil.BuildLinkMaterialSelection(target,player,
                    (_,_)=>SummonUtil.GetFieldMonsterMaterials(player,m=>materials.Contains(m.Card)));
                var selection=Build();var selected=selection.ResolveMaterials(materials);
                if(!selection.IsValidSelection(selected)){Report(false,"样例素材不满足目标的正式连接规则。");return;}
                if(command=="link-prepare") {
                    Report(true,"已准备电子龙·凯旋及场上电子龙、原始电子龙。请打开额外卡组，点击凯旋后手动选择素材。手动演出服从原动画设置。");return;
                }
                bool animationCompleted=false;
                var result=await SummonUtil.ExecuteSelectedExtraDeckSummon(new SelectedExtraDeckSummonRequest(
                    SelectedExtraCard:target,Owner:player,ChoiceContext:choice,BuildMaterialSelection:Build,
                    SummonType:spec.SummonType,PlayAnimation:async animation=>{
                        await ExtraDeckSummonAnimations.PlayLinkSummonAnimation(animation,command=="link-sieger-slow");animationCompleted=true;
                    },ConsumeMaterials:spec.ConsumeMaterials,AfterAutoPlay:spec.AfterAutoPlay,
                    OnSummonFailedAfterConsumption:spec.OnSummonFailedAfterConsumption,FinalWaitSeconds:spec.FinalWaitSeconds),materials);
                bool discarded=materials.All(c=>c.Pile?.Type==PileType.Discard);
                bool source=result.SummonedCreature?.Monster is BaseMonster monster&&monster.SourceCard==target&&target.Pile?.Type==Entry.MonsterPile;
                bool stats=result.SummonedCreature?.CurrentHp==target.Life&&result.SummonedCreature.GetPower<AttackPower>()?.Amount==target.Attack;
                bool count=player.MinionCount()==before+1;
                Report(result.Success&&discarded&&source&&stats&&count&&(mode!=EffectMode.full||animationCompleted),
                    $"连接模式={mode}；目标={target.Id.Entry}；升级={upgraded}；Link={target.YgoGetCore()!.LinkCount}；标记={target.YgoGetCore()!.Def}；素材={materials.Length}；素材入弃牌={discarded}；来源牌堆正确={source}；攻击={result.SummonedCreature?.GetPower<AttackPower>()?.Amount}/{target.Attack}；生命={result.SummonedCreature?.CurrentHp}/{target.Life}；随从数={player.MinionCount()}（预期{before+1}）；演出调用完成={animationCompleted}。视觉需另行核对。");
            });
        }
        catch(Exception ex){Entry.Logger.Error("连接测试异常："+ex);Report(false,"连接测试异常，临时模式已恢复；已发生的结算保留，详情见日志。");}
    }
}
