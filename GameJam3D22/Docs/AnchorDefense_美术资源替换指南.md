# 美术资源替换指南

如果大家之后需要根据个人风格修改作品，可以按照下面的说明配置和更换资源

项目主要资源根目录：

```text
Assets/AnchorDefense/
├─ Art/                 原始图片、模型、材质、字体、网格等
├─ Prefabs/             游戏真正实例化的预制体
├─ Configs/             数据资产；负责把预制体、图标、特效接入游戏
├─ Scenes/              MainMenu、Loading、Gameplay 等场景
└─ Input/               Input System 输入配置，不属于美术资源
```

## 一、替换建议

1. 新资源建议放入 `Assets/AnchorDefense/Art/` 下对应分类，不要直接放在项目根目录
2. 替换模型或图片时，优先修改 Prefab 的视觉子节点，不要删除带控制脚本的 Prefab 根节点
4. 不要直接改 `.meta` 文件，也不要在资源管理器中覆盖同名文件；通过 Unity Project 窗口导入。
5. 替换完成后点击 Prefab 界面右上角 `Overrides > Apply All`，确认修改保存到 Prefab Asset

## 二、UI 资源

### 2.1 UI 原始素材建议目录

```text
Assets/AnchorDefense/Art/UI/
```

图片导入建议：

- `Texture Type`：Sprite (2D and UI)
- 需要拉伸的面板：在 Sprite Editor 中设置 Border，并在 Image 上使用 `Sliced`
- 图标：通常使用 `Simple`，勾选 `Preserve Aspect`
- 像素类素材关闭压缩或使用高质量压缩，避免文字边缘发糊
- 透明图使用 PNG，确认 Alpha Source 正确

### 2.2 各页面 Prefab

| 页面 | Prefab 路径 | 主要可替换内容 |
|---|---|---|
| 开始菜单 | `Assets/AnchorDefense/Prefabs/UI/MainMenuUI.prefab` | 背景、标题、开始/设置/退出按钮、设置面板外观 |
| 加载界面 | `Assets/AnchorDefense/Prefabs/UI/LoadingUI.prefab` | 背景、进度条、旋转 Anchor 图标、提示文字 |
| 游戏 HUD | `Assets/AnchorDefense/Prefabs/UI/HUD.prefab` | 核心血条、击杀数、生存时间、锚域编织按钮 |
| 暂停菜单 | `Assets/AnchorDefense/Prefabs/UI/PauseMenuUI.prefab` | 暂停背景、继续/设置/主菜单按钮 |
| 共用设置面板 | `Assets/AnchorDefense/Prefabs/UI/SharedSettingsMenuUI.prefab` | 设置分类、Dropdown、Slider、Toggle、键位绑定行 |
| 旧设置面板/局部引用 | `Assets/AnchorDefense/Prefabs/UI/Settings Panel.prefab` | 若场景仍引用此 Prefab，同步检查其外观 |
| 技能树与锚域编织 | `Assets/AnchorDefense/Prefabs/UI/UpgradeTreeUI.prefab` | 技能树底板、节点框、连线、侧栏、碎片按钮、编织模式横幅 |

注意事项：
1. Button 的 `Target Graphic` 必须仍指向可见 Image，否则鼠标和手柄选中高亮不会显示
2. Dropdown 的选中框和展开列表分别位于 Dropdown 本体与其 `Template` 子节点；修改 Template 后，该 Dropdown 的所有运行时列表项会统一使用新样式，不需要逐行替换

### 2.3 手柄焦点与虚拟光标

- 普通菜单的当前选项会在运行时增加青色描边与轻微放大
- 不要删除 Button、Dropdown、Slider、Toggle 的 `Target Graphic`
- 技能树和锚域编织使用运行时虚拟光标，当前光标由 `GamepadVirtualCursorController` 绘制，不需要单独的场景图片
- 如果以后要换成美术光标，可让程序将 `ControllerCursorGraphic` 替换为指定 Sprite；不要直接在场景里新增第二个 EventSystem

### 2.4 字体
如果需要全局换字体：

1. 导入新的 `.ttf` 或 `.otf`
2. 在各 UI Prefab 中批量替换 Text 组件的 Font
3. 技能树详情字体还需同步替换上述字体文件，或让程序移除 `UpgradeTreeController` 中的固定字体路径
4. 不要只改 Scene 中的临时实例；优先修改 Prefab Asset，否则重新加载场景后可能恢复

## 三、星球、轨道与场景模型

### 3.1 核心星球

```text
Assets/AnchorDefense/Prefabs/Gameplay/CorePlanet.prefab
```

替换方式：

- 保留根节点和核心碰撞/生命逻辑
- 将新模型作为视觉子节点放入 Prefab，调整 Local Position/Rotation/Scale
- 材质直接配置在模型子节点的 MeshRenderer 上
- 核心生命、半径和基础颜色数据在 `Assets/AnchorDefense/Configs/CoreConfig.asset`

### 3.2 三条轨道

```text
Assets/AnchorDefense/Prefabs/Gameplay/OrbitRing_Inner.prefab
Assets/AnchorDefense/Prefabs/Gameplay/OrbitRing_Middle.prefab
Assets/AnchorDefense/Prefabs/Gameplay/OrbitRing_Outer.prefab
```

可以替换轨道 MeshRenderer、MeshFilter 和材质，但必须保留：

- Prefab 根节点上的 `OrbitRingController`
- 炮塔槽位对象及其顺序
- 用于点击/拖动的 Collider
- 每个槽位上的炮塔 Prefab 引用

如果只是增加装饰，建议新建 `VisualRoot` 或 `DecorationRoot` 子节点，不要移动槽位根节点

## 四、炮塔模型与底座

主要炮塔 Prefab：

```text
Assets/AnchorDefense/Prefabs/Gameplay/Turret.prefab
Assets/AnchorDefense/Prefabs/Gameplay/Turret_B.prefab
Assets/AnchorDefense/Prefabs/Gameplay/Turret 1.prefab
```
替换步骤：

1. 打开需要修改的炮塔 Prefab
2. 将模型放在 `VisualRoot` 下；如果没有 VisualRoot，可以新建同名空节点
3. 底座放在根节点下的 `Base`，不要放进会瞄准旋转的炮口视觉节点中
4. 把 `FirePoint` 移到新炮口位置
5. 保留根节点上的 TurretController、TurretHealth、Collider 和 AudioSource
6. 在 TurretController 的 `Projectile Definition` 中指定该炮塔使用的子弹资产

不同轨道使用哪种炮塔，由三条 OrbitRing Prefab 中各 `Turret Slot` 的 `Turret Prefab` 字段决定。需要逐个槽位指定时，打开对应 OrbitRing Prefab，选择具体槽位后拖入目标炮塔 Prefab

## 五、敌人模型与 Sprite

敌人 Prefab：

```text
Assets/AnchorDefense/Prefabs/Gameplay/Enemy.prefab
Assets/AnchorDefense/Prefabs/Gameplay/Enemy_Ranged.prefab
Assets/AnchorDefense/Prefabs/Gameplay/Enemy_Boss.prefab
```

敌人数据资产：

```text
Assets/AnchorDefense/Configs/EnemyConfig.asset
Assets/AnchorDefense/Configs/EnemyConfig_Ranged.asset
Assets/AnchorDefense/Configs/BossEnemyConfig.asset
```

Config 中与美术有关的字段：

- `Prefab`：敌人 Prefab
- `Base Color` / `Hit Color`：默认色和受击闪色
- `Projectile Prefab`：远程敌人子弹
- `Projectile Color`：敌人子弹颜色
- `Hit Effect Prefab`：受击特效
- `Death Effect Prefab`：死亡特效

3D 场景中的普通敌人目前采用单张 Sprite 始终面向相机。替换时进入 Enemy Prefab，找到 SpriteRenderer/SingleSpriteBillboardVisual 的视觉子节点，替换 SpriteRenderer 的 Sprite。不要删除 EnemyController、Collider 或 Billboard 脚本

新增敌人种类时：复制一份 Enemy Prefab 和 EnemyConfig，再到 `Assets/AnchorDefense/Configs/EndlessModeConfig.asset > Enemy Types` 添加新 Config，设置 Spawn Weight 和 Prewarm Count

## 七、子弹模型、颜色与合成子弹

子弹 Prefab：

```text
Assets/AnchorDefense/Prefabs/Gameplay/Projectile.prefab
Assets/AnchorDefense/Prefabs/Gameplay/Projectile_B.prefab
Assets/AnchorDefense/Prefabs/Gameplay/Projectile_Fused.prefab
Assets/AnchorDefense/Prefabs/Gameplay/EnemyProjectile.prefab
```

玩家子弹定义：

```text
Assets/AnchorDefense/Configs/Projectiles/Projectile_A.asset
Assets/AnchorDefense/Configs/Projectiles/Projectile_B.asset
Assets/AnchorDefense/Configs/Projectiles/Projectile_C.asset
Assets/AnchorDefense/Configs/Projectiles/Projectile_Fused.asset
```

替换玩家子弹的正确入口是 Projectile Definition：

- `Prefab`：具体子弹 Prefab
- `Override Visual Color`：是否由 Definition 强制染色
- `Visual Color`：模型、Trail、粒子和灯光使用的颜色
- `Visual Scale Multiplier`：整体视觉大小
- `Light Intensity Multiplier`：子弹灯光强度

如果美术材质已经有最终颜色，不希望程序染色，就关闭 `Override Visual Color`

子弹 Prefab 中可替换：

- `VisualRoot` 下的模型或 Sprite
- TrailRenderer 的材质、宽度和渐变
- 子弹自身 ParticleSystem
- Light 参数

必须保留 ProjectileController。子弹命中判定不依赖模型 Collider，因此不要随意修改 Controller 的引用。

合成规则与合成特效配置：

```text
Assets/AnchorDefense/Configs/Projectiles/ProjectileFusionConfig.asset
```

每条 Recipe 中：

- `Input A` + `Input B`：参与合成的两种子弹定义
- `Result`：生成的子弹定义
- `Fusion Effect Prefab`：合成瞬间特效
- `Fusion Effect Color`、粒子数量、大小、速度和持续时间：合成反馈参数

## 八、通用战斗特效

项目内置特效目录：

```text
Assets/AnchorDefense/Prefabs/VFX/
```

常用入口：

| 特效 | 配置位置 |
|---|---|
| 炮塔开火特效 | `TurretConfig.asset > Muzzle Effect Prefab` |
| 炮塔受击特效 | `TurretConfig.asset > Hit Effect Prefab` |
| 敌人受击特效 | 对应 `EnemyConfig > Hit Effect Prefab` |
| 敌人死亡特效 | 对应 `EnemyConfig > Death Effect Prefab` |
| 子弹合成特效 | `ProjectileFusionConfig.asset > Recipe > Fusion Effect Prefab` |
| 区域/角色 Buff 特效 | 对应 Zone Effect 资产中的三个 VFX 字段 |

可池化粒子 Prefab 必须满足：

1. 根节点挂 `PooledParticleEffect`。
2. `Particles` 字段拖入实际 ParticleSystem。
3. ParticleSystem 不要勾选无限循环；由池管理播放和回收。
4. Prefab 根节点缩放保持合理，实际位置由程序设置。

## 九、八个立方区域与区域效果

区域几何 Prefab：

```text
Assets/AnchorDefense/Prefabs/Zones/CubeZoneGrid.prefab
```

总配置：

```text
Assets/AnchorDefense/Configs/Zones/CubeZoneConfig.asset
```

这里可调整：

- Cube Size：所有方块统一尺寸
- Selected Cube Color：选中边框颜色和透明度
- Swap Target Color：可交换目标颜色和透明度
- Available Hint Color：空位虚影颜色
- Hovered Hint Color：当前指向空位颜色

每种区域效果分别位于：

```text
Assets/AnchorDefense/Configs/Zones/*.asset
```

例如 BlueTurretAcceleration、RedEnemySuppression、GreenTurretDamage、YellowTurretHealth 等

每个效果有三个独立特效入口：

- `Zone Vfx Prefab`：显示在区域方块内部，跟随效果移动
- `Turret Vfx Prefab`：显示在该区域内的每座炮塔身上
- `Enemy Vfx Prefab`：显示在该区域内的每个敌人身上

因此，美术提供某个区域效果时，推荐一套包含：区域环境特效、友方 Buff 特效、敌方 Buff/Debuff 特效。某类对象不需要表现时，对应字段可留空

区域效果的 UI 图标也在同一个 Zone Effect 资产的 `Icon` 字段中配置

## 十、技能树素材

技能树节点数据：

```text
Assets/AnchorDefense/Configs/Upgrades/Nodes/*.asset
```

每个 UpgradeNodeDefinition 的 `Icon` 字段就是节点图标。建议图标尺寸统一，例如 128×128 或 256×256，透明背景、主体留出约 10% 安全边距。

技能树共用外框、底板、连接线、详情栏和按钮样式统一在：

```text
Assets/AnchorDefense/Prefabs/UI/UpgradeTreeUI.prefab
```

节点颜色会根据分支和状态由程序调整。如果图标必须保持原色，请不要把状态颜色直接烘焙进图标背景；图标只提供前景，背景由节点 Image 负责。

技能树升级成功/失败音效挂在 UpgradeTreeUI 根节点的 `UpgradeTreeController`：

- `Upgrade Purchased Clip`
- `Purchase Failed Clip`

## 十一、音频资源

暂时没有

## 十二、材质、Shader 与渲染注意事项

- 项目使用 URP，材质优先使用 Universal Render Pipeline 系列 Shader
- 透明特效设置 Surface Type 为 Transparent，并根据效果决定是否写入深度
- 2D 炮塔、敌人和子弹由 Billboard/DirectionalSpriteRenderer 负责朝向相机，不要在 Animator 中额外旋转整个根节点

如果只想替换视觉而不改变玩法数值，原则上只修改 Prefab 的视觉子节点、Sprite/Material/AudioClip 引用，以及 Config 中明确标注为 Icon、Prefab、Color、VFX 的字段；不要修改伤害、生命、速度、倍率和升级消耗。
