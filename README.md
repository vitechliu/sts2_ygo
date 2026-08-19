# VYgo：杀戮尖塔 2 × 游戏王 Mod

VYgo 是一个仍在开发中的《杀戮尖塔 2》Mod，将游戏王的怪兽、魔法、额外卡组与召唤方式改造成适合尖塔战斗的卡组构筑玩法。

当前内容以机械族、电子龙与电子暗黑系列为主，已经支持怪兽随从、通常召唤、特殊召唤、融合召唤、连接召唤、超量召唤、装备、手发与额外卡组交互。项目同时包含自定义主菜单界面、FMOD 音效和用于管理卡牌数据及资源的 Web 工具。

> 项目处于早期开发阶段，卡牌数值、资源、界面和存档格式仍可能调整。

## 主菜单

- 重构主菜单视觉与控制器，加入游戏王风格背景、怪兽立绘组合及专属菜单音乐。
- 加入左侧菜单、顶部工具栏、左上角玩家资料区和新闻轮播组件。
- 新增 Profile 级持久化框架，可保存角色等级、经验、解锁项、开关与累计数据，并为后续进度系统预留扩展空间。
- 拆分主菜单视觉、菜单、工具栏、玩家资料和轮播逻辑，便于继续迭代 UI。

## 玩法机制

### 怪兽卡

- 怪兽卡打出后会召唤随从，其战斗卡进入怪兽牌堆并与随从绑定。
- 怪兽离场后，对应战斗卡才会按随从离场逻辑进入其他牌堆。
- 场上最多可以存在 5 只怪兽。

### 通常召唤与上级召唤

- 当前没有每回合一次的通常召唤次数限制，通常以能量费用表达怪兽等级带来的登场成本。
- 上级召唤仍在逐卡设计阶段；需要解放素材或根据解放数量生效的怪兽会由具体卡牌单独实现。

### 特殊召唤

- `[特召]` 表示不按通常方式登场；满足条件后，相关卡牌通常会在本次打出前降为 0 费。
- 例如「电子龙」会在己方没有随从时获得特召条件。
- 由自动打出流程召唤的怪兽也视为特殊召唤，并会触发统一的特殊召唤钩子。

### 额外卡组

- 额外卡组是独立牌堆，不会进入普通抽牌堆。
- 融合、连接和超量怪兽可以从额外卡组发起召唤，并通过统一界面手动选择合法素材。
- 融合召唤可按卡牌效果使用场上、手牌或其他指定牌堆中的素材。
- 连接召唤会校验素材数量、种族、名称等卡牌自定义条件。
- 超量召唤会校验怪兽等级与目标阶级，素材会保留为超量素材并供后续效果消耗。

### 无效

- 每层 `[无效]` 可以抵消敌人的一次强化或削弱，但不抵消回血或获得护甲。
- 无效既可附加给玩家，也可附加给怪兽；部分卡牌需要支付额外代价才能发动。

### 手发
- 部分怪兽（灰流丽、增殖G）等，除了左键正常打出外，可以右键发动"手发"效果。

### 盖伏
- 部分陷阱具有该关键词，多半为能力牌，盖伏的卡只有盖伏的回合结束之后才能发效果。

### 装备
- 装备魔法和装备行动的卡，会寄生在怪兽身上，怪兽离场时才会进入墓地。

## 开发环境

### 依赖

- Godot 4.5.1 .NET
- .NET 9 SDK
- 《杀戮尖塔 2》`0.106.0` 或更高版本
- [STS2 RitsuLib](https://github.com/BAKAOLC/STS2-RitsuLib) `0.4.59` 或更高版本
- MinionLib `0.6.0` 或更高版本

项目当前编译时引用 `STS2.RitsuLib 0.5.4` 和 `FuYnAloft.Sts2.MinionLib 0.6.2`；运行时最低版本以 [`VYgo.json`](VYgo.json) 为准。

### 配置

1. 克隆仓库并初始化卡牌数据子模块：

   ```bash
   git submodule update --init --recursive
   ```

2. 按操作系统复制环境配置模板：

   ```bash
   # macOS
   cp env.props.example_mac env.props

   # Linux
   cp env.props.example_linux env.props
   ```

   Windows 请将 `env.props.example_windows` 复制为 `env.props`。

3. 修改 `env.props` 中的 Godot、游戏目录、游戏数据目录、DLL 目录和 Mod 目录。
4. 第一次修改 Godot 资源前，使用 Godot 打开项目一次，生成 `.godot/` 和资源导入文件。

`env.props` 是本机配置文件，不应提交到仓库。

### 构建与发布

只修改 C# 时，执行：

```bash
dotnet build
```

构建完成后会将 `VYgo.dll` 和 `VYgo.json` 复制到 `Sts2ModDir/VYgo/`。

修改 Godot 场景、图片、Shader 或 FMOD Bank 后，使用完整发布流程：

```bash
# macOS / Linux
./publish.sh

# Windows
publish.bat
```

发布流程会生成程序集，并在已配置 `GodotPath` 时导出 `VYgo.pck`。

## Web 卡牌管理工具

[`Web/`](Web/) 中的管理工具可以按卡片 ID 管理 YGO 卡牌数据，并完成以下工作：

- 从本地上游数据库读取卡名、描述、种族、属性、攻防、系列等字段。
- 从外部资源目录导入卡图和怪兽立绘。
- 裁剪卡图、生成怪兽场景与基础随从脚本。
- 生成中文本地化，并将单张或全部卡牌导出到 `VYgo/db.json`。

### 字段数据源

卡牌字段由 `External/ygopro` 子模块中的 `cards.cdb` 和 `strings.conf` 提供。上游数据不会由 Web 服务自动更新；需要更新时请显式拉取 `server` 分支，并在主仓库中审查子模块指针变化：

```bash
git submodule update --remote --merge External/ygopro
git add External/ygopro
```

导出 `VYgo/db.json` 时，工具会从上游数据库实时生成 `archetypes` 数组。如果子模块未初始化或找不到对应卡片，页面会显示警告并导出空数组。

### 配置与运行

准备以下资源目录，并在 Web 设置页中配置其本地路径：

- [ygopro2-closeup 怪兽立绘](https://code.moenext.com/mycard/ygopro2-closeup)
- [hd-arts 高清卡图](https://code.moenext.com/mycard/hd-arts)

然后启动服务：

```bash
cd Web
npm install
OPEN_BROWSER=false npm start
```

确保本项目的子模块git submodule成功安装。

访问 [http://localhost:3000](http://localhost:3000) 即可使用。需要自动打开浏览器时，可以直接运行 `npm start`。


### 自动化Skill

- 本项目的自动化Skill可以自动读取腾讯文档进行开发。
- 要求已经全局安装了 [腾讯文档Skill](https://docs.qq.com/open/document/mcp/skill/) 。


## 鸣谢

- 感谢 [苍蓝 coccvo](https://code.moenext.com/coccvo) 制作 ygopro2 怪兽立绘并授权本项目使用。
- 感谢赤子奈落（MDPro3 作者）的授权。
