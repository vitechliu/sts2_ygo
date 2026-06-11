using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.Interop;
using VYgo.Scripts.Cards;

namespace VYgo.Scripts;

// 必须要加的属性，用于注册Mod。字符串和初始化函数命名一致。
[ModInitializer(nameof(Initialize))]
public static class Entry {
    public const string ModId = "VYgo";
    public const string ResPath = $"res://{ModId}";

    public static Logger Logger { get; private set; } = null!;
    public static PileType ExtraPile;
    
    public static void Initialize() {
        var assembly = Assembly.GetExecutingAssembly();
        Logger = RitsuLibFramework.CreateLogger(ModId);
        var harmony = new Harmony("sts2.vitech." + ModId.ToLowerInvariant());
        harmony.PatchAll();
        
        RegisterCardPile();
        SubscribeEvents();
        
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);
        
        Logger.Info("VYgo initialized.");
    }

    //注册额外卡组
    static void RegisterCardPile() {
        var registry = ModCardPileRegistry.For(ModId);
        ExtraPile = registry.RegisterOwned("extra_pile", new ModCardPileSpec {
            Scope = ModCardPileScope.CombatOnly,
            Style = ModCardPileUiStyle.BottomLeft,
            Anchor = ModCardPileAnchor.Default,
            IconPath =  "res://VYgo/images/extra_card_pile.png",
            OnOpen = ctx => ctx.ShowDefaultPileScreen(),
            VisibleWhen = ctx => ctx.Player != null,
        }).PileType;
    }

    static void SubscribeEvents() {
        // RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>((@event, disposable) => {
        //     Logger.Info("CombatStarting");
        //     var combatState = @event.CombatState;
        //     foreach (var p in combatState.Players) {
        //         if (p.Character is RedhatCharacter) {
        //             Logger.Info("Found Redhat Character");
        //             //查找额外卡牌
        //             if (p.PlayerCombatState == null) continue;
        //             foreach (var card in p.PlayerCombatState.AllCards) {
        //                 Logger.Info("Found Redhat Character Card:" + card.Title);
        //                 if (card is BaseMonsterCard monsterCard && monsterCard.IsExtra) {
        //                     CardPileCmd.Add(card, ExtraPile);
        //                 }
        //             }
        //         }
        //     }
        // });
    }
}