# Anchor 星核助手自然语言指令系统

## 快速启用 DeepSeek

1. 在 Unity 菜单打开 `Tools > Anchor Defense > AI > Configure Local Cloud API`。
2. 保持默认值：
   - Base URL：`https://api.deepseek.com`
   - 模型：`deepseek-flash`
3. 填入你自己的 DeepSeek API Key，点击“保存本机配置”。
4. 重新进入 `Gameplay` 场景运行。

项目已默认选用 `deepseek-flash`，不必在 DeepSeek 后台另外选择模型。指令解析请求会关闭
思考模式，并为 JSON 输出预留 512 tokens。

密钥只会写到：

`Application.persistentDataPath/AnchorDefense/ai-provider.json`

不会写入 Unity 项目或 Git。也可以使用环境变量 `ANCHOR_AI_API_KEY` 或
`DEEPSEEK_API_KEY`，环境变量优先级更高。

如果你的 DeepSeek 账户仍使用其他模型名，可以在同一窗口把模型名改为实际可用的名称，
例如 `deepseek-chat`。请求层会按 OpenAI 兼容的 Chat Completions 协议工作。

## 游戏内操作

- 键盘 `T`：打开星核助手对话框并聚焦输入框。
- `Enter`：提交。
- `Escape`：关闭。
- 手柄左摇杆按下：打开。
- 手柄 `A`：提交。
- 手柄 `B`：关闭。

对话框打开时只关闭轨道和镜头等 Gameplay 输入，敌人、炮塔、生成器与计时不会暂停。
每条成功指令固定消耗 10 个当前可用击杀点；网络错误、无效输出或游戏结束不会扣点。
玩家可以一次组合至多 6 种已登记操作（上限可在总配置中调整），如“在右上角轰击并减速敌人”。操作按顺序间隔 0.25 秒启动，
共享一次消耗与威力预算（每种效果强度为 `1 / 操作数`）。当前区域没有敌人或受损炮塔也能施放：
指令场会持续 6 秒，播放释放特效，并影响随后进入该方块的单位。
“这里”“选中区域”、C01～C08、左上/右下等明确位置由 Unity 优先解析；模型猜出的区域编号不会被直接采用。
没有明确位置时，游戏根据全部操作和当前战场重新选择敌人、受损炮塔或炮塔集中的实际方块；
敌人暂时位于方块外时，也会参考其离各方块的距离。

## 默认基础操作

| 操作 ID | 资产 | 默认效果 |
| --- | --- | --- |
| `anchor_strike` | `Configs/AI/Skills/AnchorStrike.asset` | 区域内全部敌人受到 25 伤害 |
| `repair_pulse` | `Configs/AI/Skills/RepairPulse.asset` | 区域内全部存活炮塔恢复 35 生命 |
| `repulsion_wave` | `Configs/AI/Skills/RepulsionWave.asset` | 区域内全部敌人远离核心 3.5 米 |
| `slow_field` | `Configs/AI/Skills/SlowField.asset` | 区域内敌人移动速度降低 |
| `rapid_field` | `Configs/AI/Skills/TurretRapidField.asset` | 区域内炮塔射击间隔缩短 |
| `power_field` | `Configs/AI/Skills/TurretPowerField.asset` | 区域内炮塔子弹伤害提升 |

总配置位于 `Assets/AnchorDefense/Configs/AI/AICommandConfig.asset`，可统一调整消耗、
超时、温度、输入长度、最多操作数、指令场持续时间、操作间隔和技能列表。
这些指令场与已有立方体长期效果并存，不会改写方块绑定的效果。

## 美术替换位置

- 整套对话 UI：`Assets/AnchorDefense/Prefabs/UI/AICommandConsoleUI.prefab`。其中
  `Assistant Portrait` 是可直接换 Sprite 的头像，`Assistant Speech Bubble`、`Command Input`、
  `Submit Command` 均可单独替换；卡通素材源文件为 `Assets/AnchorDefense/Art/UI/AnchorAssistant.png`。
- 指令场角标：`Assets/AnchorDefense/Prefabs/VFX/AICommandCornerMarker.prefab`，
  在总配置的 `Field Marker Prefab` 引用。`AICommandFieldMarker` 组件可调整角标长度、线宽、
  闪光/常驻颜色和透明度、闪光时长，以及技能图标的显示时长、大小和位置。
  默认施放时闪光约 0.6 秒，之后只保留淡角标直至指令场结束。技能资产的 `Icon` 优先作为
  浮动图标；未配置时使用星核助手头像占位。旧圆环预制体保留作回退，但不再是默认引用。
- 每种操作的图标与释放特效：`Configs/AI/Skills/` 对应资产上的 `Icon`、`Vfx Prefab`、
  `Vfx Lifetime`；数值在 `Configs/AI/Effects/` 对应资产中。可分别替换，不必改脚本。

UI 预制体是独立 Canvas，不会重建或覆盖现有 HUD。美术可以直接修改背景、气泡、头像、按钮、
字体、加载动画和排版，脚本引用保持不丢即可。自动安装器只在预制体没有 `Assistant Portrait`
时做一次布局升级，不会反复覆盖后续美术改动。必要时可用菜单
`Tools > Anchor Defense > AI > Apply Assistant Dialogue UI` 检查安装。

若需要重新创建缺失的资产或重新接入场景，可手动执行：

`Tools > Anchor Defense > AI > Build Natural Language Command System`

。

## 增加一种基础操作

1. 在 `Assets/AnchorDefense/Scripts/Skills/` 新建 `ActiveSkillEffect` 子类。复用伤害、治疗等机制时，
   直接复制相近的现有类；新增机制时实现 `ApplyToEnemy` 或 `ApplyToTurret`。需要每帧持续刷新时
   将 `RepeatWhileInside` 设为 `true`。不要直接修改 DeepSeek 通信或消费流程。
2. 在 `Configs/AI/Effects/` 创建该效果资产，设置强度。若操作会修改已有区域效果也在修改的属性，
   应像当前减速、速射那样给单位增加独立的“指令临时加成”通道，避免每帧被区域系统覆盖。
3. 在 `Configs/AI/Skills/` 创建 `Active Skill` 资产，填写唯一 `Id`、显示名、给 AI 理解的
   `Model Description`、关键词、`Target Policy`、效果资产、图标和释放 VFX。
4. 将此资产加入 `AICommandConfig.asset > Skills` 数组。运行时提示词和本地白名单会自动读取它；
   不用改 UI、解析器或钱包。为新机制写空区域、进入区域、离开区域的测试。

目前一条命令只选**一个现有立方体**作为目标，但可在该区域组合多个操作；不能创造游戏没有的
模型、机制、任意坐标或价格。完全超出能力的请求不会扣点，助手会说明无法执行。

## 安全边界

DeepSeek 只负责把玩家文本转换为 `{action, operation_ids, target_zone_id}` 的 JSON 配方。
所有操作 ID、区域 ID、解锁状态、操作数量、价格与效果数值都由 Unity 本地重新校验。
返回额外字段、未知操作或未知区域不会被直接执行。旧版单技能 JSON 暂时兼容。
若模型返回错误 JSON 或拒绝明显可做的动作，游戏只会依据已解锁技能资产中的明确关键词做保守兜底；
否则拒绝且不扣点，不会从错误输出中读取数值或预制体名。
系统不保留聊天历史、不自动重试，并限制同一时间只有一个请求。
如果服务偶尔返回空内容或截断，终端会给出对应提示；除非原文命中上述保守兜底，
此类失败不会扣除指令点。

DeepSeek JSON Output 官方说明：
https://api-docs.deepseek.com/guides/json_mode/
