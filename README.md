# 杀戮尖塔2 YGO mod


## 项目配置
1. 在项目中创建一份env.props用于配置环境变量，可从env.props.example中参考
2. Godot导入项目，生成export_presets.cfg

### 发布pck
- 使用publish.bat(windows)或publish.sh(unix/mac)

## 原则


## 设计

### 怪兽卡
- 含有[召唤]关键词的怪兽卡打出后和能力卡类似，被绑定在了怪兽身上，表现为消失，只有召唤怪兽死亡后才会移入弃牌堆

### 通常召唤
- 暂时不做通召限制，星数越高的卡费用越高

### 上级召唤
- 需要解放一定数量的怪兽才能登场
- 部分主打上级召唤的标志性卡牌可以做，比如说要根据解放数量提供效果的卡片(小丑，真龙)

### 特召
- 拥有该关键词代表不占用通召的召唤，包括连接召唤等，并且手发打出时费用为0
- 如电子龙: 场上没有随从时获得[特召]
- 如右起子: 从卡组[特召]1张左起子
- 当一张怪兽卡被AutoPlay打出，则视为特召。

### 战破抗性
- 拥有[守护者]的怪兽可以获得，不会被战斗击杀，至少会保留1血 守护者能力默认隐藏

### 康/无效
- [无效]作为能力，不管在玩家身上还是怪兽身上效果相同
- 有的怪兽自身会获得[无效],有的卡牌则会把[无效]附加给玩家
- 每层[无效]会抵消敌人的1次强化或削弱（不包含回血/获得护甲）
- 有的[无效]需要支付代价才能生效



## Web卡牌自动导入工具

 自动导入工具能够输入卡片id自动从外部资源目录导入卡图、立绘，并从ygocdb api接口获取卡片名称翻译等

### 字段数据源

卡牌字段由 `External/ygopro` submodule 中的 `cards.cdb` 和 `strings.conf` 提供。首次克隆项目后先初始化：

```bash
git submodule update --init External/ygopro
```

上游数据不会由 Web 服务自动更新。需要更新时，显式拉取 `server` 分支并在主仓库审查 submodule 指针变化：

```bash
git submodule update --remote --merge External/ygopro
git add External/ygopro
```

Web 导出 `VYgo/db.json` 时会从上游数据库实时生成 `archetypes` 数组；若 submodule 未初始化或卡片不存在，会显示警告并为该卡导出空数组。
 
### 配置与使用

- 首先从萌卡clone两个仓库：
- 立绘：https://code.moenext.com/mycard/ygopro2-closeup
- 高清卡图：https://code.moenext.com/mycard/hd-arts
- 执行` node ./Web/server.js `，后访问[http://localhost:3000](http://localhost:3000)配置路径即可使用。


### 鸣谢
- 感谢 [苍蓝coccvo](https://code.moenext.com/coccvo) 对ygopro2中立绘的制作和对本项目的授权
- 感谢 赤子奈落(MDPro3作者)的授权。
