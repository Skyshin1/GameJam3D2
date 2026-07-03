# Anchor Defense 原型

打开 `Assets/AnchorDefense/Scenes/Gameplay.unity` 后进入 Play Mode 即可试玩。核心星球、三条环绕带、18 座炮台、环境和 HUD 都是场景中可见的 Prefab 实例，不会由代码在运行时生成。

另一个可对比版本位于 `Assets/AnchorDefense/Scenes/Gameplay_DirectionalSprites.unity`。它保留相同的 3D 星球、轨道和 UI，但炮台、敌人、子弹使用始终朝向相机的八方向 Sprite。

## 操作

- 点击环绕带或其炮台来选中环绕带。
- 按住鼠标左键并横向拖动来旋转选中的环绕带。
- 炮台会自动寻找射程内最近的敌人并射击。
- 按 `C` 在固定视角、XY 平面环绕、自由 360° 环绕三种相机模式间切换。
- 相机环绕开启时，按住鼠标右键拖动来旋转相机；切回固定视角会恢复初始机位。

## 调整数值

默认配置资产位于 `Assets/AnchorDefense/Configs`，也可在 Project 窗口的 Create > Anchor Defense 菜单中建立其他版本：

- Core Config：核心生命、半径和颜色。
- Enemy Config：速度、血量、核心伤害、受击颜色与表现引用。
- Turret Config：射程、伤害、射击间隔、子弹速度和子弹表现引用。
- Endless Mode Config：出生范围、刷新速度、批次数量与成长曲线参数。

需要使用自定义配置时，在 Gameplay 场景的 `Systems` 节点上替换 `GameBootstrap` 对应字段。

## 美术资产

- `Assets/AnchorDefense/Prefabs/Gameplay`：核心、敌人、炮台、子弹和三条轨道。
- `Assets/AnchorDefense/Prefabs/VFX`：命中、死亡与炮口粒子。
- `Assets/AnchorDefense/Prefabs/UI`：场景内 HUD。
- `Assets/AnchorDefense/Art/Meshes`：当前占位轨道模型。
- `Assets/AnchorDefense/Art/Materials`：所有占位材质。
- `Assets/AnchorDefense/Art/Sprites/Directional`：方向版透明 Sprite Sheet 和可重新切片的源图。
- `Assets/AnchorDefense/Configs/Directional`：炮台、敌人和子弹的八方向 SpriteSet 及配置变体。
- `Assets/AnchorDefense/Prefabs/Directional`：方向版单位与三条轨道 Prefab。

玩法脚本放在 Prefab 根节点，美术模型放在 `VisualRoot` 下。替换模型时保留根节点、`MuzzlePoint` 等功能节点即可。

方向版美术替换时，按 `N、NE、E、SE、S、SW、W、NW` 顺序修改对应的 `DirectionalSpriteSet`。方向判断基于固定相机平面，Sprite 使用深度测试，位于星球后方时仍会被正确遮挡。

## 扩展入口

- `IDamageable` 与 `DamageInfo`：扩展伤害对象和伤害类型。
- `EnemyController.Initialize` 的命中/死亡表现回调：替换粒子、帧动画或死亡演出。
- `ProjectilePrefab`：替换默认可见子弹。
- `ComponentPool<T>`：敌人、子弹和高频粒子共用的对象池基础。
- `EnemyRegistry`：炮台查找目标时使用活动敌人列表，不进行逐炮台全场物理扫描。

Play Mode 冒烟测试位于 `Assets/AnchorDefense/Tests/PlayMode`。
