using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interactions.RightClick;
using VYgo.Scripts;
using VYgo.Scripts.Actions;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.Playmaker;
using VYgo.Scripts.Cards.Category.ZaneTruesdale;
using VYgo.Scripts.Monsters;
using VYgo.Scripts.Powers;
using VYgo.Utils;

namespace VYgo.Core.DevConsole;

internal sealed partial class PlaymakerTestRunner {
    private async Task ExecuteCase(Type type, bool upgraded) {
        // 衍生物必须由来源卡生成，不能通过直接召唤结果绕过来源生命周期。
        if (type == typeof(CyberseGadgetToken)) {
            await TestGadget(false, tokenOnly: true);
            return;
        }
        CardModel card = null!;
        await Step(async _ => card = await Add(type, upgraded));
        if (card is BaseVYgoCard ygo) {
            Check("运行时核心数据存在", ygo.YgoGetCore() != null, true);
            Check("卡图存在", ResourceLoader.Exists($"res://VYgo/images/cards/{ygo.CardId}.png"), true);
            if (card is BaseMonsterCard) {
                Check("随从模型存在", ygo.YgoGetMonster() != null, true);
                Check("随从场景存在", ResourceLoader.Exists($"res://VYgo/scenes/monsters/{ygo.CardId}.tscn"), true);
            }
        }

        switch (card) {
            case EncodeTalker encode: {
                var survivor = await Fixture<Bitron>();
                await StandardLink(encode);
                Stats(encode, 0, upgraded ? 16 : 12);
                var pet = Summoned(encode);
                Check("所有在场怪兽获得编码保护", player.Creature.Pets.All(p => p.HasPower<UntilNextTurnBattleDestructionProtectionPower>()), true);
                await Step(async choice => { await CreatureCmd.Damage(choice, survivor, 100, ValueProp.Move, Enemy); });
                Check("致命攻击后保留在战斗", survivor.IsAlive && Combat.Creatures.Contains(survivor), true);
                Check("致命攻击后剩余生命", survivor.CurrentHp, 1);
                await NextTurn();
                Check("持有者下回合保护到期", pet.HasPower<UntilNextTurnBattleDestructionProtectionPower>(), false);
                await Step(async choice => { await CreatureCmd.Damage(choice, survivor, 200, ValueProp.Move, Enemy); });
                Check("到期后正常战斗死亡", survivor.IsDead, true);
                break;
            }
            case ExcodeTalker excode: {
                await StandardLink(excode);
                Stats(excode, upgraded ? 20 : 15, 15);
                Check("上限降低至3", player.GetMaxMinionCount(), 3);
                await Fixture<Bitron>(); await Fixture<Digitron>();
                Bitron blocked = null!;
                await Step(async _ => blocked = await Add<Bitron>(PileType.Hand));
                Check("达到新上限后不可通召", blocked.CanPlay(), false);
                await Step(_ => CreatureCmd.Kill(Summoned(excode)));
                Check("余码离场后上限仍为3", player.GetMaxMinionCount(), 3);
                break;
            }
            case PowercodeTalker powercode: {
                await StandardLink(powercode);
                int attack = upgraded ? 16 : 12;
                Stats(powercode, attack, 1);
                var pet = Summoned(powercode);
                int hp = Enemy.CurrentHp;
                await Act(pet);
                Check("首击伤害", hp - Enemy.CurrentHp, attack);
                Check("攻击后自身攻击翻倍", Attack(pet), attack * 2);
                Check("同回合次数耗尽", pet.Powers.OfType<BasePerTurnMonsterAction>().Single().CanAct(Combat), false);
                await NextTurn();
                hp = Enemy.CurrentHp;
                await Act(pet);
                Check("次回合按翻倍攻击结算", hp - Enemy.CurrentHp, attack * 2);
                Check("再次攻击后继续翻倍", Attack(pet), attack * 4);
                break;
            }
            case TranscodeTalker transcode: {
                LinkSpider revive = null!;
                await Step(async _ => revive = await Add<LinkSpider>(PileType.Discard));
                await StandardLink(transcode);
                Stats(transcode, upgraded ? 13 : 10, 10);
                Stats(revive, 3, 1);
                Check("复活的是所选弃牌", revive.Pile?.Type == Entry.MonsterPile, true);
                break;
            }
            case ShootingcodeTalker shooting: {
                await Fixture<LinkSpider>();
                await StandardLink(shooting);
                Stats(shooting, upgraded ? 10 : 8, 3);
                var pet = Summoned(shooting);
                var action = pet.Powers.OfType<BasePerTurnMonsterAction>().Single();
                Check("两只连接怪兽给予3次攻击", action.RemainingUses, 3);
                for (int i = 0; i < 3; i++) {
                    int hand = Hand.Cards.Count;
                    await Act(pet);
                    Check("每次攻击抽一张", Hand.Cards.Count - hand, 1);
                }
                Check("额外次数用尽", action.CanAct(Combat), false);
                await NextTurn();
                Check("次回合恢复为1次", action.RemainingUses, 1);
                await Act(pet);
                Check("次回合攻击一次后不可再攻击", action.CanAct(Combat), false);
                break;
            }
            case DecodeTalkerHeatsoul heatsoul: {
                await StandardLink(heatsoul);
                Stats(heatsoul, upgraded ? 20 : 15, 10);
                int hand = Hand.Cards.Count;
                await Act(Summoned(heatsoul));
                Check("攻击抽牌", Hand.Cards.Count - hand, upgraded ? 2 : 1);
                await NextTurn();
                Check("基础抽5加炽热之魂抽1", Hand.Cards.Count, 6);
                break;
            }
            case DecodeTalkerIntegration integration: {
                await StandardLink(integration);
                Stats(integration, upgraded ? 20 : 15, 10);
                LinkSpider selected = null!; CyberDragonNova excluded = null!;
                await Step(async _ => {
                    selected = await Add<LinkSpider>(Entry.ExtraPile);
                    excluded = await Add<CyberDragonNova>(Entry.ExtraPile);
                });
                await NextTurn();
                Check("回合结束将电子界额外怪兽送墓", selected.Pile == Grave, true);
                Check("非电子界额外怪兽未被选择", excluded.Pile == Entry.ExtraPile.GetPile(player), true);
                break;
            }
            case DecodeTalkerExtended extended: {
                await Fixture<LinkSpider>();
                await StandardLink(extended);
                Stats(extended, (upgraded ? 20 : 15) + 20, 10);
                var action = Summoned(extended).Powers.OfType<BasePerTurnMonsterAction>().Single();
                Check("两只连接怪兽给予3次攻击", action.RemainingUses, 3);
                int hp = Enemy.CurrentHp;
                await Act(Summoned(extended));
                Check("强化后的实际伤害", hp - Enemy.CurrentHp, upgraded ? 40 : 35);
                await NextTurn();
                Check("次回合额外次数清除", action.RemainingUses, 1);
                Check("攻击强化仍保留", Attack(Summoned(extended)), upgraded ? 40 : 35);
                await Act(Summoned(extended));
                Check("次回合攻击一次后不可再攻击", action.CanAct(Combat), false);
                break;
            }
            case CyberseWhiteHat white: {
                Check("空场费用2", white.EnergyCost.GetAmountToSpend(), 2);
                var first = await Fixture<Bitron>();
                Check("只有一只同族仍需2费", white.EnergyCost.GetAmountToSpend(), 2);
                var second = await Fixture<BackupSecretary>();
                Check("恰好两只同族时0费", white.EnergyCost.GetAmountToSpend(), 0);
                await Play(white, expectedCost: 0);
                Stats(white, 5, 7);
                EncodeTalker target = null!;
                await Step(async _ => target = await Add<EncodeTalker>(Entry.ExtraPile));
                await Link(target, ((BaseMonster)first.Monster!).SourceCard!, ((BaseMonster)second.Monster!).SourceCard!, white);
                foreach (var enemy in Combat.HittableEnemies) Check("作为连接素材对所有敌人施加虚弱", enemy.GetPowerAmount<WeakPower>(), upgraded ? 3 : 2);
                break;
            }
            case CyberseGadget:
                await TestGadget(upgraded, existing: (CyberseGadget)card);
                break;
            case Dotscaper dots: {
                await Play(dots, expectedCost: 0); Stats(dots, 0, 1);
                var first = Summoned(dots);
                await Step(_ => CreatureCmd.Kill(first));
                Check("首次送墓后重新特召", Summoned(dots) != first, true);
                var second = Summoned(dots);
                await Step(_ => CreatureCmd.Kill(second));
                Check("同名送墓效果不重复发动", dots.Pile == Grave, true);
                await Step(choice => CardCmd.Exhaust(choice, dots));
                Check("消耗效果有独立次数", dots.Pile?.Type == Entry.MonsterPile, true);
                await Step(_ => CreatureCmd.Kill(Summoned(dots)));
                await Step(choice => CardCmd.Exhaust(choice, dots));
                Check("第二次消耗不复活", dots.Pile?.Type == PileType.Exhaust, true);
                Dotscaper copy = null!;
                await Step(async _ => copy = await Add<Dotscaper>(PileType.Hand));
                await Play(copy, expectedCost: 0);
                await Step(_ => CreatureCmd.Kill(Summoned(copy)));
                Check("不同实体共享卡名送墓次数", copy.Pile == Grave, true);
                break;
            }
            case BootStaggered boot: {
                // 自动特召的素材不能误触发手牌交错鹿。
                await Fixture<Bitron>();
                Check("特召不会触发手牌交错鹿", boot.Pile == Hand, true);
                Digitron normal = null!;
                await Step(async _ => normal = await Add<Digitron>(PileType.Hand));
                await Play(normal, expectedCost: 1);
                Stats(boot, 8, 2);
                int before = player.MinionCount();
                await Act(Summoned(boot));
                Check("攻击后新增一只衍生物", player.MinionCount(), before + 1);
                Check("由攻击生成引导鹿衍生物", player.Creature.Pets.Any(p => p.Monster is BaseMonster { SourceCard: BootStaggeredToken }), true);
                break;
            }
            case ROMCloudia rom: {
                Bitron recover = null!;
                await Step(async _ => recover = await Add<Bitron>(PileType.Discard));
                await Play(rom, expectedCost: 2); Stats(rom, 6, 1);
                Check("通召将弃牌回手", recover.Pile == Hand, true);
                int pets = player.MinionCount();
                await Step(_ => CreatureCmd.Kill(Summoned(rom)));
                Check("送墓特召仅升级后生效", player.MinionCount(), upgraded ? pets : pets - 1);
                if (upgraded) Check("升级后从抽牌堆召唤", player.Creature.Pets.Any(p => p.Monster is BaseMonster { SourceCard: Bitron }), true);
                await Step(async choice => {
                    await CardPileCmd.Add(recover, PileType.Discard);
                    await CardCmd.AutoPlay(choice, rom, null);
                });
                Check("特召不触发通召回收", recover.Pile == Grave, true);
                break;
            }
            case ClockWyvern clock: {
                await Play(clock, expectedCost: 1);
                Stats(clock, upgraded ? 6 : 3, 3);
                Check("登场生成时钟衍生物", player.Creature.Pets.Count(p => p.Monster is BaseMonster { SourceCard: ClockWyvernToken }), 1);
                break;
            }
            case ThresholdBorg threshold: {
                await Play(threshold, expectedCost: 3); Stats(threshold, 9, 5);
                foreach (var enemy in Combat.HittableEnemies) Check("全体敌人失去力量", enemy.GetPowerAmount<StrengthPower>(), upgraded ? -2 : -1);
                break;
            }
            case CodeGenerator generator: {
                Check("基础攻击", generator.Attack, upgraded ? 7 : 5);
                var material = await Fixture<CodeTalker>();
                DecodeTalker target = null!;
                CardModel milled = Draw.Cards.First();
                await Step(async _ => target = await Add<DecodeTalker>(Entry.ExtraPile));
                Check("可以作为码语者手牌素材", generator.CanUseFromHand(target), true);
                Check("不能作为其他系列手牌素材", generator.CanUseFromHand(ModelDb.Card<LinkSpider>()), false);
                await Link(target, ((BaseMonster)material.Monster!).SourceCard!, generator);
                Check("素材效果将抽牌堆怪兽送墓", milled.Pile == Grave, true);
                Check("手牌素材没有直接登场", player.Creature.Pets.Any(p => p.Monster is BaseMonster { SourceCard: CodeGenerator }), false);
                break;
            }
            case MicroCoder micro: {
                Check("稀有技能怪兽", micro.Rarity == CardRarity.Rare && micro.Type == CardType.Skill, true);
                Check("可以作为码语者手牌素材", micro.CanUseFromHand(ModelDb.Card<DecodeTalker>()), true);
                Check("不能作为其他系列手牌素材", micro.CanUseFromHand(ModelDb.Card<LinkSpider>()), false);
                await Step(choice => CardCmd.Discard(choice, micro));
                Check("普通弃牌不触发发现", Hand.Cards.Count, 0);
                await Step(async _ => { await CardPileCmd.Add(micro, PileType.Hand); });
                for (int i = 0; i < 2; i++) {
                    if (i == 1) {
                        await Step(async _ => { await CreatureCmd.Kill(player.Creature.Pets.Single(p => p.IsAlive));
                            await CardPileCmd.Add(micro, PileType.Hand); });
                        await Play(micro, expectedCost: 1);
                        Stats(micro, 1, 1);
                    }
                    var material = await Fixture<CodeTalker>();
                    DecodeTalker target = null!;
                    await Step(async _ => target = await Add<DecodeTalker>(Entry.ExtraPile));
                    var before = Hand.Cards.ToHashSet();
                    await Link(target, ((BaseMonster)material.Monster!).SourceCard!, micro);
                    Check("发现提供3张候选", _lastChoiceOptionCount, 3);
                    var generated = Hand.Cards.Where(c => !before.Contains(c)).ToArray();
                    Check(i == 0 ? "手牌素材生成一张魔陷" : "场上素材生成一张魔陷", generated.Length, 1);
                    Check("候选为电脑网魔陷", generated[0] is BaseVYgoCard selected
                        && selected is BaseSpellCard or BaseTrapCard && selected.ContainArchetype(YgoArchetypes.Cynet), true);
                    Check("生成卡升级状态", generated[0].IsUpgraded, upgraded);
                }
                break;
            }
            case DegradeBuster degrade: {
                int before = Energy;
                await RightClick(degrade);
                Check("素材不足不扣能量", Energy, before);
                Check("素材不足不特召", degrade.Pile == Hand, true);
                Bitron a = null!; Digitron b = null!;
                await Step(async _ => { a = await Add<Bitron>(PileType.Discard); b = await Add<Digitron>(PileType.Discard); });
                await RightClick(degrade);
                Check("手发扣3能量", before - Energy, 3);
                Check("两只素材消耗", a.Pile?.Type == PileType.Exhaust && b.Pile?.Type == PileType.Exhaust, true);
                Stats(degrade, 0, 10);
                await Step(async _ => { await CreatureCmd.GainBlock(Enemy, 100, ValueProp.Unpowered, null); });
                int hp = Enemy.CurrentHp, max = Enemy.MaxHp, block = Enemy.Block;
                await Act(Summoned(degrade));
                Check("除外扣当前生命", hp - Enemy.CurrentHp, upgraded ? 20 : 15);
                Check("除外扣最大生命", max - Enemy.MaxHp, upgraded ? 20 : 15);
                Check("除外不消费格挡", Enemy.Block, block);
                Check("启动每回合一次", Summoned(degrade).Powers.OfType<BasePerTurnMonsterAction>().Single().CanAct(Combat), false);
                break;
            }
            case AccesscodeTalker access: {
                await StandardLink(access);
                Stats(access, (upgraded ? 20 : 15) + 15, 10);
                var pet = Summoned(access);
                int hp = Enemy.CurrentHp;
                await Act(pet);
                Check("无限启动攻击伤害", hp - Enemy.CurrentHp, upgraded ? 35 : 30);
                Check("启动消耗连接怪兽", PileType.Exhaust.GetPile(player).Cards.OfType<DecodeTalker>().Any(), true);
                Check("没有连接弃牌时不可启动", pet.Powers.OfType<BasePerTurnMonsterAction>().Single().CanAct(Combat), false);
                await Step(async _ => { await Add<LinkSpider>(PileType.Discard); });
                hp = Enemy.CurrentHp;
                await Act(pet);
                Check("同回合可再次启动", hp - Enemy.CurrentHp, upgraded ? 35 : 30);
                break;
            }
            case CynetCodec codec: {
                await Play(codec, expectedCost: 1);
                Check("能力已安装", player.Creature.HasPower<CynetCodecPower>(), true);
                await Fixture<Bitron>();
                Check("非码语者特召不生成卡", Hand.Cards.Count, 0);
                CodeTalker target = null!;
                await Step(async _ => target = await Add<CodeTalker>(Entry.ExtraPile));
                var a = await Fixture<BackupSecretary>(); var b = await Fixture<LinkInfraFlier>();
                await Link(target, ((BaseMonster)a.Monster!).SourceCard!, ((BaseMonster)b.Monster!).SourceCard!);
                Check("特召码语者生成一张", Hand.Cards.Count, 1);
                var generated = (BaseMonsterCard)Hand.Cards.Single();
                Check("生成电子界怪兽", generated.YgoGetCore().IsRace(YgoRace.Cyberse), true);
                Check("生成属性一致", generated.YgoGetCore()!.Attribute, target.YgoGetCore()!.Attribute);
                Check("生成升级状态", generated.IsUpgraded, upgraded);
                break;
            }
            case CynetMining mining: {
                Check("没有其他手牌不能发动", mining.CanPlay(), false);
                Bitron discard = null!;
                await Step(async _ => discard = await Add<Bitron>(PileType.Hand));
                await Play(mining, expectedCost: 0);
                Check("所选手牌已丢弃", discard.Pile == Grave, true);
                Check("发现提供3张候选", _lastChoiceOptionCount, 3);
                Check("生成恰好一张手牌", Hand.Cards.Count, 1);
                var generated = (BaseMonsterCard)Hand.Cards.Single();
                Check("生成4星以下电子界怪兽", generated.Level is > 0 and <= 4 && generated.YgoGetCore().IsRace(YgoRace.Cyberse), true);
                Check("选项升级正确", generated.IsUpgraded, upgraded);
                Check("技能结算后入弃牌", mining.Pile == Grave, true);
                break;
            }
            case CynetCrosswipe crosswipe: {
                Check("没有电子界怪兽不能发动", crosswipe.CanPlay(), false);
                await Fixture<Bitron>();
                int hp = Enemy.CurrentHp;
                await Play(crosswipe, Enemy, 1);
                Check("对所选敌人造成伤害", hp - Enemy.CurrentHp, upgraded ? 10 : 8);
                Check("攻击牌入弃牌", crosswipe.Pile == Grave, true);
                break;
            }
            case CynetUniverse universe: {
                await Play(universe, expectedCost: 0);
                var power = player.Creature.GetPower<CynetUniversePower>()!;
                Bitron normal = null!; LinkSpider link = null!;
                await Step(async _ => {
                    normal = await Add<Bitron>(PileType.Discard);
                    if (upgraded) link = await Add<LinkSpider>(PileType.Discard);
                });
                await RightClick(power);
                Check("普通怪兽回抽牌堆", normal.Pile == Draw, true);
                if (upgraded) Check("额外怪兽回额外牌堆", link.Pile == Entry.ExtraPile.GetPile(player), true);
                await Step(async _ => { await Add<Bitron>(PileType.Discard); await Add<Digitron>(PileType.Discard); });
                int count = Grave.Cards.Count;
                await RightClick(power);
                Check("同回合重复启动被拒绝", Grave.Cards.Count, count);
                break;
            }
            case CynetStorm storm: {
                await Play(storm, expectedCost: 2);
                var first = await Fixture<LinkSpider>();
                Check("连接怪兽召唤时强化", Attack(first), upgraded ? 10 : 8);
                LinkSpider target = null!;
                await Step(async _ => target = await Add<LinkSpider>(Entry.ExtraPile));
                await Step(async choice => { await CreatureCmd.Damage(choice, player.Creature, 20, ValueProp.Unpowered, Enemy); });
                Check("恰好20伤害不触发", player.Creature.HasPower<CynetStormPower>(), true);
                Check("20伤害未召唤", target.Pile == Entry.ExtraPile.GetPile(player), true);
                await Step(async choice => { await CreatureCmd.Damage(choice, player.Creature, 21, ValueProp.Unpowered, Enemy); });
                Check("超过20伤害后特召", target.Pile?.Type == Entry.MonsterPile, true);
                Check("触发后移除能力", player.Creature.HasPower<CynetStormPower>(), false);
                Check("触发召唤同样得到强化", Attack(Summoned(target)), upgraded ? 10 : 8);
                break;
            }
            case CynetRecovery recovery: {
                await Play(recovery, expectedCost: upgraded ? 1 : 2);
                var noBattle = await Fixture<Bitron>();
                await Step(_ => CreatureCmd.Kill(noBattle));
                Check("普通送墓不给能量", player.Creature.GetPowerAmount<EnergyNextTurnPower>(), 0);
                var battle = await Fixture<Bitron>();
                await Step(async choice => { await CreatureCmd.Damage(choice, battle, 10, ValueProp.Move, Enemy); });
                Check("被战斗破坏后获得下回合能量", player.Creature.GetPowerAmount<EnergyNextTurnPower>(), 1);
                await NextTurn();
                Check("次回合多1能量", Energy, player.PlayerCombatState.MaxEnergy + 1);
                Check("下回合能量能力已消费", player.Creature.HasPower<EnergyNextTurnPower>(), false);
                break;
            }
            case CynetRegression regression: {
                Check("表格要求技能牌", regression.Type, CardType.Skill);
                await Play(regression, expectedCost: 0);
                Check("盖伏来源仍是有效战斗卡", regression.Pile?.Type == Entry.SetTrapPile && !regression.HasBeenRemovedFromState, true);
                int hp = Enemy.CurrentHp;
                await Fixture<LinkSpider>();
                Check("盖伏当回合不触发", Enemy.CurrentHp, hp);
                Check("盖伏当回合仍保留能力", player.Creature.HasPower<CynetRegressionPower>(), true);
                await NextTurn();
                int hand = Hand.Cards.Count;
                var enemies = Combat.HittableEnemies.ToDictionary(c => c, c => c.CurrentHp + c.Block);
                await Fixture<LinkSpider>();
                foreach (var pair in enemies) Check("陷阱触发全体10伤害（计入格挡抵扣）", pair.Value - pair.Key.CurrentHp - pair.Key.Block, 10);
                Check("陷阱触发抽牌", Hand.Cards.Count - hand, upgraded ? 2 : 1);
                Check("陷阱来源卡消耗", regression.Pile?.Type == PileType.Exhaust, true);
                Check("触发后移除盖伏能力", player.Creature.HasPower<CynetRegressionPower>(), false);
                break;
            }
            case CynetConflict conflict: {
                await Play(conflict, expectedCost: 0);
                Check("盖伏来源仍是有效战斗卡", conflict.Pile?.Type == Entry.SetTrapPile && !conflict.HasBeenRemovedFromState, true);
                var power = player.Creature.GetPower<CynetConflictPower>()!;
                await RightClick(power);
                Check("盖伏当回合不能启动", player.Creature.HasPower<CynetConflictPower>(), true);
                await Fixture<CodeTalker>(); await Fixture<DecodeTalker>();
                await NextTurn();
                int hp = Combat.HittableEnemies.Sum(e => e.CurrentHp), max = Combat.HittableEnemies.Sum(e => e.MaxHp);
                await RightClick(power);
                Check("两只码语者提供2层无效", player.Creature.GetPowerAmount<NegatingPower>(), 2);
                Check("随机除外两次当前生命总量", hp - Combat.HittableEnemies.Sum(e => e.CurrentHp), upgraded ? 30 : 20);
                Check("随机除外两次最大生命总量", max - Combat.HittableEnemies.Sum(e => e.MaxHp), upgraded ? 30 : 20);
                Check("冲突结算后入弃牌", conflict.Pile == Grave, true);
                Check("冲突能力移除", player.Creature.HasPower<CynetConflictPower>(), false);
                break;
            }
            default: throw new InvalidOperationException("没有实现此测试用例：" + type.Name);
        }
    }

    private async Task TestGadget(bool upgraded, bool tokenOnly = false, CyberseGadget? existing = null) {
        CyberseGadget source = existing!;
        Bitron revive = null!; Linkslayer excluded = null!;
        await Step(async _ => {
            if (source == null) source = await Add<CyberseGadget>(PileType.Hand, upgraded);
            revive = await Add<Bitron>(PileType.Discard);
            excluded = await Add<Linkslayer>(PileType.Discard);
        });
        await Play(source, expectedCost: 1);
        Stats(source, 5, 1);
        Stats(revive, 0, 4);
        Check("高星怪兽没有被复活", excluded.Pile == Grave, true);
        await Step(_ => CreatureCmd.Kill(Summoned(source)));
        Check("送墓后来源去向", source.Pile?.Type == (upgraded ? PileType.Discard : PileType.Exhaust), true);
        var token = player.Creature.Pets.Single(p => p.IsAlive && p.Monster is BaseMonster { SourceCard: CyberseGadgetToken });
        var tokenCard = ((BaseMonster)token.Monster!).SourceCard!;
        Stats(tokenCard, 0, 1);
        Check("工具衍生物标记", tokenCard.Rarity, CardRarity.Token);
        if (tokenOnly) {
            await Step(_ => CreatureCmd.Kill(token));
            Check("衍生物死亡后不留在战斗牌堆", player.PlayerCombatState.AllCards.Contains(tokenCard), false);
        }
    }
}
