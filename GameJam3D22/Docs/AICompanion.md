# 星核助手：陪伴对话配置

## 在哪里改

- 总配置：`Assets/AnchorDefense/Configs/AI/AICompanionConfig.asset`。
- 美术预制体：`Assets/AnchorDefense/Prefabs/UI/AICommandConsoleUI.prefab`。
- DeepSeek 地址、模型和超时：原有 `Assets/AnchorDefense/Configs/AI/AICommandConfig.asset`，本机设置与环境变量继续生效。不要把 API Key 写进资产或提交到 Git。

第一次导入脚本后，安装器会给现有助手预制体增量添加气泡和救场选项；不重建原面板。若未出现，可退出运行模式后执行 `Tools → Anchor Defense → AI → Install Companion Conversation`。仅 3D 场景的初始化流程连接陪伴服务。

## 调节行为

在 `AICompanionConfig` Inspector 中：

| 参数 | 默认值 | 用途 |
| --- | --- | --- |
| Recommendation Interval | 30 | 每隔多少有效游玩秒检查可购买升级 |
| Recommendation Lifetime | 60 | 已展示推荐保持有效的游玩秒数 |
| Chatter Minimum / Maximum | 8 / 18 | 闲聊随机间隔，不计算暂停时间 |
| Bubble Seconds | 7 | 全文显示完成后，普通气泡至少停留多久 |
| Typewriter Enabled | 开启 | 闲聊、推荐等气泡逐字显示；系统错误立即显示 |
| Typewriter Characters Per Second | 28 | 每秒显示字符数，中英文均适用；越大越快 |
| Temperature | 0.8 | 对话措辞变化程度，不会改变本地执行白名单 |
| Rescue Health Ratio | 0.3 | 核心存活且血量比例不高于此值时，首次触发救援询问 |
| Rescue Visual Seconds | 0.8 | 清屏表现持续时间，使用真实时间，不受暂停影响 |
| Rescue Vfx Prefab | 可不填 | 额外救场特效，生成于核心位置；粒子采用不受暂停影响的时间 |
| Chinese / English 长度项 | 45 字 / 30 词 | 普通回复长度上限；推荐独立为 60 字 / 40 词 |
| Personality | 温暖、自信、有点调皮 | 人设与说话方式；不在这里添加可执行能力 |
| World Lore | 童话星球入侵设定 | 可以讲述的世界观事实 |

没有可买节点时不请求推荐。推荐必须前置满足、未购买、余额够用；末端节点符合条件也可推荐。玩家首次成功购买后比较节点 ID，一条建议只评价一次。

想更频繁闲聊，可把 Chatter Minimum / Maximum 调为 5 / 10 秒；推荐间隔单独用 Recommendation Interval（例如 20 秒）。这里只是发起机会，实际出现还需等待接口返回，忙碌或暂停时可能跳过。降低间隔会增加 API 用量。Recommendation Lifetime 不是推荐间隔，而是用于判断玩家是否采纳建议的有效期。修改配置资产时退出 Play 模式，避免运行时改值退出后丢失。

逐字效果只用于头像旁气泡，展开的输入面板和救场快捷回答不受影响。气泡先按全文排版再逐字展示，不跳动；新消息替换旧消息时从头显示，打开输入面板或切换语言后旧气泡不会继续冒出。动画使用真实时间，不影响战斗或输入。

普通聊天、建议和救场不花游戏内击杀点，但仍会产生供应商的 API 用量；主动闲聊每 8～18 秒一次，试玩时可以把间隔调大以降低调用频率。不会自动重试。

## 美术如何替换

助手头像目前复用项目的手绘猫咪炮手 `Assets/AnchorDefense/Art/UI/maomao.png`。展开面板和右下角入口均在 `AICommandConsoleUI.prefab` 中绑定，原有呼吸、摇摆和反馈动画继续生效；不覆盖原来的星核图片。

打开 `AICommandConsoleUI.prefab`：

- `Companion Chat Bubble`：普通聊天纸张底图、描边、阴影和 `Bubble Tail` 尾角。
- `Companion Speech`：气泡字体、字号、颜色。内容运行时由模型产生，不需要逐句配置。
- `Recommended Upgrade`：推荐卡片背景；`Upgrade Icon` 与文字使用实际升级节点的素材和本地化名称。
- `Companion Rescue Choices`：救援快捷选项底板与两个按钮。文字自动跟随中英文设置。
- `Companion Rescue Backdrop`：救场遮罩，阻止点击后面的界面。
- `Companion Rescue Flash`：清屏时短闪光；颜色和透明度由视图播放逻辑控制，额外粒子改总配置中的 Rescue Vfx Prefab。
- 原来的 `Assistant Portrait` 与 `Open Command Console` 仍是助手头像。各自的 `AICommandAssistantMotion` 可替换动画帧与运动幅度。
- 根对象 `AICompanionView → Glyph Fallback` 已绑定完整中文备用 TMP 字体，仅补美术字体缺失的字形，不替换主体风格。字体与 OFL 许可证位于 `Assets/AnchorDefense/Art/UI/Fonts/`，来源为 [Noto 官方仓库](https://github.com/notofonts/noto-cjk/tree/main/Sans/OTF/SimplifiedChinese)。也可替换为自己有授权的完整字库。

新增气泡和选项都是预制体内的真实子对象，不是临时拼出的界面。根对象 `AICompanionView` 暴露总配置、文字、推荐图标、快捷按钮、背景与闪光引用，修改美术后保留这些绑定。

## 运行规则

- “最密集的地方 / most crowded area”等指令由本地按执行准备时各立方体内存活敌人数排序，模型返回的猜测区域不能覆盖此选择。普通自动攻击也优先实际区域内数量，区域外威胁仅用于数量相同的候选；明确要求“最危险”仍使用威胁评分。范围仍是现有立方体，不是把屏幕投影重叠的敌人当成同一区域。

- 点击助手或使用原有 T / 手柄打开键可以聊天，也能继续输入游戏操作。输入法确认候选字不会同时提交。
- 普通气泡不展开输入、不抢镜头控制；推荐卡可定位技能树节点，但不自动购买。
- 救场先暂停，再等待生成询问。可以打字回答，或点击「帮帮我 / 我能应付」。手柄方向键左右选择快捷回复，确认键提交；关闭/返回视为拒绝。
- 接受救场只回收当前敌人及敌方子弹，不加击杀、不加点数、不恢复核心；闪光结束后正常刷怪。每局只问一次。
- 网络出错时仍可通过快捷按钮或关闭完成救场选择，不能被困在暂停里。
- 其他窗口拥有自己的暂停状态；救场结束不会把尚未关闭的暂停菜单或技能树一起恢复。
- 记忆只存在当前一局，发送最近 20 条原文和较早对话摘要。新的战局快照优先于历史；历史中的指令不会自动重放。

## 验证入口

`Tools → Anchor Defense → AI → Run Offline Companion Tests` 使用 Mock，不调用付费 API；结果写入 `Temp/AnchorCompanionTests.xml`。测试前应保存打开的场景与预制体。

测试包含推荐失效、购买对比、无奖励清屏、暂停叠加、断网与含糊回答、优先级、历史、原有命令协议和中英文 UI 渲染。真实 API 冒烟测试为 Explicit，普通测试运行不会自动调用。

仍需真人体验的项目：不同手柄型号的按键、中文输入法候选窗口、真实网络等待节奏以及台词自然度。模型生成不能保证每句都独一无二；历史与不同话题轮换用于降低重复。
