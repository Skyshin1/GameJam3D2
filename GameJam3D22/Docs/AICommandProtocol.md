# AI 指令 JSON 协议

模型返回一个 JSON 对象。先区分区域、星环和升级，再从运行时能力目录选择操作。目录按请求读取技能配置与当前升级树，包含说明、节点费用、前置条件和状态，配置新升级节点无需修改提示词或新增专用技能。

```json
{
  "action": "compose",
  "commands": [
    {
      "type": "区域",
      "operations": [
        { "operate": { "action": "anchor_strike", "index": "C01" }, "source": "攻击 C01" },
        { "operate": { "action": "repair_pulse", "index": "C02" }, "source": "治疗 C02" }
      ],
      "addSkill": {}
    },
    {
      "type": "星环",
      "operations": [
        {
          "operate": { "action": "rotate_ring", "index": "01", "mode": "repeat", "count": 3 },
          "source": "第一星环每次转45度，转3次后停止"
        }
      ],
      "addSkill": {}
    }
  ]
}
```

上例对应玩家输入：`攻击 C01，治疗 C02，第一星环每次转45度，转3次后停止`。

## 字段

| 字段 | 约定 |
| --- | --- |
| `action` | `compose`；无法执行时使用 `reject` 且 `commands` 为空数组 |
| `commands` | 按原文顺序排列的分组，区域→星环→区域可使用三个分组 |
| `type` | `区域`、`星环` 或 `升级` |
| `operations` | 每条操作独立携带动作、目标和原文 |
| `operate.action` | 技能 ID；升级为 `purchase_upgrade`；区域不可包含星环或升级动作 |
| `operate.index` | 区域 `C01`～`C08`；星环 `01`～`03` 对应内、中、外环；升级为实际节点 ID；未指定为 `null` |
| `source` | 对应动作和目标的连续原文片段，必须保留否定词；同一区域的组合技能可共用片段 |
| `addSkill` | 预留字段，只允许 `{}` |

区域操作包含 `action` 和 `index`，可选 `selector`。升级操作仅包含 `action` 和 `index`。星环操作还必须包含 `mode` 和 `count`：

| `mode` | 行为 | `count` |
| --- | --- | --- |
| `adaptive` | 自动接管，默认用于“操作星环” | `null` |
| `spin` | 持续旋转 | `null` |
| `once` | 单次旋转 | `null` |
| `repeat` | 每次转动指定角度，完成指定次数后停止 | 原文给出的 1～20 整数 |
| `stop` | 停止对应星环；未指定星环则全部停止 | `null` |

每次旋转默认 45°、用时 0.8 秒，指定角度限制 5°～180°。方向、角度、持续时间和速度从各自 `source` 读取。指定有限次数后停止不会被误判为立即停止。

## 升级与动态选择

```json
{
  "action": "compose",
  "commands": [
    {
      "type": "升级",
      "operations": [
        { "operate": { "action": "purchase_upgrade", "index": null }, "source": "升级炮塔" }
      ],
      "addSkill": {}
    },
    {
      "type": "区域",
      "operations": [
        {
          "operate": {
            "action": "anchor_strike",
            "index": null,
            "selector": { "metric": "enemy_count", "order": "desc" }
          },
          "source": "对敌人最密集的区域发动攻击"
        }
      ],
      "addSkill": {}
    }
  ]
}
```

对应输入：`升级炮塔，对敌人最密集的区域发动攻击`。

- 未指定升级方向时，选择当前可购买、费用最低的一项炮塔属性升级；同价按节点 ID 排序。结果显示实际节点名称及费用。已经买过、资源不足和缺少前置条件分别反馈。
- 指定名称、ID、属性方向或星环扩容目标时必须与已有节点匹配。升级调用 `UpgradeSystem.TryPurchase`，保留原有费用、效果、解锁及事件通知；不会自动购买玩家未要求的前置。
- `selector.metric` 可用 `enemy_count`（区域内敌人数）、`missing_health`（炮塔缺失生命总量）、`turret_count`（炮塔数）、`enemy_pressure`（周边敌人威胁）；`order` 为 `asc` 或 `desc`。非法字段或值拒绝执行。
- 原文明确条件优先于模型 selector。“敌人最密集”严格按区域内敌人数降序选择，不叠加周边威胁；完全同分时优先当前选区，否则选较小区域编号。原文片段不得省略该条件。
- 未提供选择条件的普通区域命令保留原有技能择优策略。相同原文的技能组合共用目标，选择条件冲突时拒绝。
- 新配置节点自动发现；新的游戏功能系统仍需接入执行接口一次，模型不能通过 JSON 创造未实现的机制。

## 本地校验与执行

- 原文中的实际目标优先于模型编号。未指定位置时按操作自动择优；同一原文片段的区域组合共用一个目标。
- 原文片段不得改写、遗漏明确目标或颠倒操作顺序。明确但不存在的目标不会被替换为自动目标。
- 星环操作必须有明确操控意图。“攻击轨道附近的敌人”仍是区域攻击；仅出现“轨道”不能启动旋转。
- 全部操作先预校验，检查指令费用与所有升级费用的总和。区域/非停止星环整句扣一次配置费用；纯停止免费；纯升级只扣节点费用。各区域共享技能威力预算，升级不占该预算；总操作数受 `MaximumOperations` 限制。
- 按顺序检查升级前置条件与技能解锁条件；允许先购买明确指定的前置/解锁节点再执行后续操作。反向顺序或重复购买拒绝整条执行。
- 同一技能可用于不同区域。同一区域的重复技能、同一星环的冲突操作以及“停止全部星环”与其他星环操作的组合会被拒绝。
- 区域技能各自持续配置时长，星环有限旋转可被停止、手动接管或游戏结束打断。
- `AICommandExecutionResult.Operations` 提供逐项目标、描述与 `Executed` 状态。延后执行与失败通过 `AICommandService.ExecutionUpdated` 通知界面，未执行项显示“未执行”。
- 执行失败取消剩余任务并清理本次指令场；尚未产生技能效果时返还指令费用。已经购买的升级保留节点费用和所有权，实际执行明细分别报告；原生购买已提交但效果异常时说明该节点应用效果失败。

## 兼容与解析

旧的 `cast_skill`、`operation_ids` 单目标返回仍通过适配层处理，也须通过原文目标和类型校验。新模型提示词只要求新格式。

使用 `AICommandJsonValidator.TryParse` 解析原始返回，严格检查嵌套字段、重复键、次数类型和空 `addSkill`。新结构包含可空整数 `count`，不要直接使用 Unity `JsonUtility.FromJson` 解析新格式。网络响应预算为 2048 tokens。
