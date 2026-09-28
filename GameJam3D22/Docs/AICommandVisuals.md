# 区域指令视觉配置

区域指令使用 `Assets/AnchorDefense/Prefabs/VFX/AICommandCornerMarker.prefab`，在根节点的 `AICommandFieldMarker` 组件中调整效果。该预制体由 `Assets/AnchorDefense/Configs/AI/AICommandConfig.asset` 的 `Field Marker Prefab` 引用。

每项区域技能实际激活时，薄网格扫描面从区域顶部扫到底部，同时亮起一阵稀疏星光。扫描光面有青绿、紫红、暖金渐变和细光环。持续期间，多彩四角星芒错开时间闪烁并轻微漂动，两段淡淡的星尘光弧带有流动亮点，八个边框角点也会闪亮。最后一项区域技能的持续时间结束前，星光、光弧和边框一起淡出。扫描和星光跟随实际执行顺序，目标区域不显示悬浮卡通图标。

| Inspector 参数 | 默认值 | 用途 |
| --- | --- | --- |
| Star Burst Duration | 0.75 秒 | 技能释放时短暂星光的持续时间 |
| Star Burst Opacity | 0.5 | 释放时的星光亮度；设为 0 可关闭 |
| Scan Sweep Duration | 0.9 秒 | 扫描面从顶部到底部的时间 |
| Scan Sweep Opacity | 0.38 | 扫描面亮度；设为 0 可关闭扫描 |
| Corner Color Variation | 0.6 | 边框的多彩渐变程度；设为 0 使用原有边框颜色 |
| Star Count | 42 | 星光数量，最多 128；设为 0 可关闭持续星光 |
| Star World Size | 0.3 | 星芒基础尺寸；角点略大，区域内大小各异 |
| Star Opacity | 0.85 | 持续星光的亮度 |
| Star Drift Speed | 0.35 | 轻微漂动的速度；设为 0 可关闭漂动 |
| Corner Breath Amount | 0.18 | 持续期间边角透明度的呼吸幅度 |
| Ending Fade Duration | 0.45 秒 | 结束时提亮和淡出的时间 |
| Actor Cue World Size | 0.9 | 单位标记基础尺寸；按模型包围盒适配，最大为此值的 3 倍 |
| Actor Cue Opacity | 0.9 | 单位标记亮度 |
| Line Width | 0.055 | 边角线宽 |
| Resting Color | 青色，透明度 0.13 | 持续期间边角颜色 |
| Flash Color / Flash Duration | 青色 / 0.6 秒 | 激活时边角提亮 |

区域星光由 `AICommandStarfield` 合并成一份动态网格，另外使用两条细线绘制光弧；单位反馈由 `AICommandActorFeedback` 绘制。它们与扫描面共用 `AICommandSignal.shader` 和 `M_AICommandSignal.mat`，不依赖粒子贴图或下载资源：

- 攻击：受击位置亮起金色星芒和向外散开的小星光，0.32 秒后消失；击杀后仍保留这段短暂反馈。
- 治疗：炮塔生命实际增加时，三枚绿色小星光环绕主体。
- 减速：受影响敌人两侧缓慢闪烁蓝色小星芒，跟随单位移动。
- 增益：紫色小星光环绕炮塔。
- 推离：青色小星光环绕敌人。

持续效果重复作用时复用同一单位标记。停止收到效果刷新后，标记会在 0.24 秒内消失；离开区域、敌人死亡、敌人被对象池复用或指令场清理时也会清理对应标记。标记不带碰撞体，不修改技能效果、指令费用、威力预算或区域范围。

以上五类内置效果不再在区域中央生成技能配置中的大团粒子。其他自定义效果仍沿用 `ActiveSkillDefinition.VfxPrefab`；如需调整自定义技能特效，可继续在对应技能配置中设置。

PlayMode 回归覆盖扫描向下移动并结束、释放星光结束、持续星光淡出、旧卡通图标不再出现、单位反馈复用和清理、真实治疗反馈、击杀后的命中闪光、对象池复用及跨类型指令的视觉时序。
