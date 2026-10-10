using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnchorDefense
{
    // UI copy is deliberately separate from gameplay identifiers and AI JSON tokens.
    public static partial class GameLocalization
    {
        private static readonly Dictionary<string, string> English = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "开始游戏", "Start Game" }, { "设置", "Settings" }, { "退出游戏", "Quit Game" },
            { "稳定轨道 · 锚定火力 · 守住星核", "Steady the orbits · Anchor the fire · Defend the core" },
            { "毛绒防线", "PLUSH DEFENSE" },
            { "轨道防御网络已暂时冻结", "The orbital defense network is paused" },
            { "继续游戏", "Resume Game" }, { "系统设置", "System Settings" },
            { "返回主菜单", "Main Menu" }, { "按 Esc 继续", "Press Esc to resume" },
            { "正在建立锚定连接…", "Establishing anchor link…" },
            { "锚定完成", "Anchor link ready" },
            { "正在连接轨道防御网络", "Connecting to the orbital defense network" },
            { "提示：旋转轨道能够改变炮台阵列的拦截方向", "Tip: Rotate the rings to redirect your turret fire." },
            { "显示", "Display" }, { "图像", "Graphics" }, { "声音", "Audio" }, { "控制", "Controls" },
            { "ANCHOR SYSTEM SETTINGS  /  系统设置", "ANCHOR SYSTEM SETTINGS" },
            { "语言", "Language" },
            { "分辨率", "Resolution" }, { "显示模式", "Display Mode" },
            { "屏幕亮度", "Brightness" }, { "垂直同步", "V-Sync" },
            { "非编辑状态显示区域边框", "Show zone borders outside edit mode" },
            { "帧率上限", "Frame Rate Limit" }, { "图像质量", "Graphics Quality" },
            { "抗锯齿方案", "Anti-Aliasing" }, { "主音量", "Master Volume" },
            { "音乐音量", "Music Volume" }, { "音效音量", "Sound Effects Volume" },
            { "轨道拖拽灵敏度", "Ring Drag Sensitivity" },
            { "相机环绕灵敏度", "Camera Orbit Sensitivity" },
            { "选择 / 旋转轨道", "Select / Rotate Ring" },
            { "旋转相机", "Orbit Camera" }, { "切换相机模式", "Switch Camera Mode" },
            { "打开升级树", "Open Upgrade Tree" }, { "暂停 / 返回", "Pause / Back" },
            { "锚域编织", "Field Weaving" }, { "锚核指令终端", "Anchor Command Console" },
            { "恢复默认键位", "Reset Key Bindings" },
            { "恢复默认设置", "Restore Defaults" }, { "应用设置", "Apply Settings" },
            { "返回", "Back" }, { "启用", "Enabled" },
            { "无边框全屏", "Borderless Fullscreen" }, { "独占全屏", "Exclusive Fullscreen" },
            { "窗口模式", "Windowed" }, { "不限制", "Unlimited" },
            { "关闭", "Off" }, { "SMAA（高质量）", "SMAA (High Quality)" },
            { "ANCHOR / 星核助手", "ANCHOR / CORE ASSISTANT" },
            { "告诉星核", "Tell the core" }, { "解析中…", "Thinking…" },
            { "和星核说：在右上角轰击并减速敌人……", "Tell the core: strike and slow enemies in the upper-right…" },
            { "待输入指令", "Waiting for a command" },
            { "呼叫星核助手  [T]", "Call the core assistant  [T]" },
            { "锚核指令终端  [T]", "Anchor Command Console  [T]" },
            { "ANCHOR CORE / 锚核指令终端", "ANCHOR CORE / COMMAND CONSOLE" },
            { "执行 [Enter]", "Send [Enter]" },
            { "例：在右上区域释放轰击 / 修复受损最严重的区域", "Example: strike the upper-right zone / repair the most damaged turrets" },
            { "ANCHOR PROTOCOL  /  锚定升级树", "ANCHOR PROTOCOL  /  UPGRADE TREE" },
            { "ANCHOR FIELD WEAVING  /  锚域编织模式", "ANCHOR FIELD WEAVING  /  FIELD EDIT MODE" },
            { "完成编织", "Finish Weaving" },
            { "CUBE FIELD FRAGMENTS  /  立方体区块功能解锁", "CUBE FIELD FRAGMENTS  /  ZONE UNLOCKS" },
            { "FIELD FRAGMENTS  /  场域碎片", "FIELD FRAGMENTS" },
            { "TURRET ANCHOR PROTOCOLS  /  炮塔与轨道升级", "TURRET ANCHOR PROTOCOLS  /  TURRET & RING UPGRADES" },
            { "核心失守", "CORE LOST" }, { "生存时间 00:00", "SURVIVED 00:00" },
            { "生存  00:00", "SURVIVAL  00:00" }, { "核心  100 / 100", "CORE  100 / 100" },
            { "重新开始", "RESTART" }, { "敌人  0", "ENEMIES  0" }
        };

        private static readonly Dictionary<string, string> Chinese = BuildReverse();
        private static bool initialized;
        private static GameLanguage lastLanguage;
        public static bool IsEnglish => GameSettingsService.Current.language == GameLanguage.English;
        public static GameLanguage Language => GameSettingsService.Current.language;
        public static event Action LanguageChanged;

        public static string T(string chinese, string english) => IsEnglish ? english : chinese;

        public static string Translate(string source)
        {
            if (string.IsNullOrEmpty(source)) return source;
            if (source.StartsWith("图像质量会切换项目已有的", StringComparison.Ordinal))
                return IsEnglish
                    ? "Graphics Quality selects the project's Performant, Balanced, or High Fidelity URP profile.\nSMAA looks better with the fixed camera; FXAA costs less performance."
                    : source;
            if (!IsEnglish && source.StartsWith("Graphics Quality selects the project's", StringComparison.Ordinal))
                return "图像质量会切换项目已有的 Performant、Balanced、High Fidelity 三档 URP 配置。\nSMAA 适合当前固定视角，FXAA 性能开销更低。";
            return (IsEnglish ? English : Chinese).TryGetValue(source, out string translated)
                ? translated : source;
        }

        public static string AIResultMessage(AICommandExecutionResult result)
        {
            if (result == null) return T("没有收到结果", "No result was returned.");
            if (!IsEnglish) return result.Message;
            switch (result.Outcome)
            {
                case AICommandOutcome.Success: return "Done. The command is taking effect.";
                case AICommandOutcome.Busy: return "I'm still working on your last command.";
                case AICommandOutcome.EmptyInput: return "Tell me what you want to do first.";
                case AICommandOutcome.InsufficientPoints: return "Not enough command points.";
                case AICommandOutcome.NotConfigured: return "The AI service is not configured.";
                case AICommandOutcome.Rejected: return "I can only use abilities available in this game.";
                case AICommandOutcome.InvalidCommand: return "I couldn't turn that into a valid game command.";
                case AICommandOutcome.InvalidTarget: return "I couldn't find a valid target zone.";
                case AICommandOutcome.Cancelled: return "That command was cancelled.";
                case AICommandOutcome.NetworkError: return "I couldn't reach the AI service. No points were spent.";
                case AICommandOutcome.GameUnavailable: return "Commands are unavailable right now.";
                default: return "The command was interrupted. Unused points were refunded.";
            }
        }

        public static string AIOperationSummary(AICommandExecutionResult result)
        {
            if (result == null) return string.Empty;
            if (!IsEnglish) return result.OperationSummary ?? result.Message;
            var parts = new List<string>();
            foreach (AICommandOperation operation in result.Operations)
            {
                if (operation == null) continue;
                string part = operation.IsUpgrade
                    ? "Upgrade: " + operation.Upgrade.LocalizedDisplayName
                    : operation.Skill != null && operation.Skill.Effect.IsWorldOperation
                        ? "Ring Control: " + (operation.Ring != null ? operation.Ring.name : "all rings")
                        : (operation.Skill != null ? operation.Skill.LocalizedDisplayName : "Command") +
                          (operation.ZoneId >= 0 ? $" → C{operation.ZoneId + 1:00}" : string.Empty);
                parts.Add(part + (operation.Executed ? string.Empty : " (pending)"));
            }
            return parts.Count > 0 ? string.Join("; ", parts) : AIResultMessage(result);
        }

        public static void SetLanguage(GameLanguage language)
        {
            if (Language == language) return;
            GameSettingsData data = GameSettingsService.Current.Clone();
            data.language = language;
            GameSettingsService.ApplyAndSave(data);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (initialized) return;
            initialized = true;
            lastLanguage = Language;
            GameSettingsService.Changed += OnSettingsChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => RefreshVisibleText();

        private static void OnSettingsChanged(GameSettingsData settings)
        {
            if (settings.language == lastLanguage) return;
            lastLanguage = settings.language;
            RefreshVisibleText();
            LanguageChanged?.Invoke();
        }

        public static void RefreshVisibleText()
        {
            foreach (Text label in Resources.FindObjectsOfTypeAll<Text>())
                if (label != null && label.gameObject.scene.IsValid()) label.text = Translate(label.text);

            foreach (TMP_Text label in Resources.FindObjectsOfTypeAll<TMP_Text>())
            {
                if (label == null || !label.gameObject.scene.IsValid()) continue;
                TMP_InputField input = label.GetComponentInParent<TMP_InputField>(true);
                if (input != null && input.textComponent == label) continue;
                label.text = Translate(label.text);
            }
            RefreshTitleArtwork();
        }

        private static void RefreshTitleArtwork()
        {
            foreach (Image image in Resources.FindObjectsOfTypeAll<Image>())
            {
                if (image == null || !image.gameObject.scene.IsValid() || image.sprite == null ||
                    image.sprite.name != "start_screen_0003_毛绒防线") continue;
                image.enabled = !IsEnglish;
                Transform existing = image.transform.parent.Find("English Plush Defense Title");
                Text label;
                if (existing == null)
                {
                    GameObject objectWithText = new GameObject("English Plush Defense Title", typeof(RectTransform), typeof(Text));
                    objectWithText.transform.SetParent(image.transform.parent, false);
                    RectTransform rect = (RectTransform)objectWithText.transform;
                    RectTransform source = image.rectTransform;
                    rect.anchorMin = source.anchorMin;
                    rect.anchorMax = source.anchorMax;
                    rect.pivot = source.pivot;
                    rect.anchoredPosition = source.anchoredPosition;
                    rect.sizeDelta = source.sizeDelta;
                    rect.localRotation = source.localRotation;
                    rect.localScale = source.localScale;
                    label = objectWithText.GetComponent<Text>();
                    label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    label.fontStyle = FontStyle.Bold;
                    label.fontSize = 48;
                    label.alignment = TextAnchor.MiddleCenter;
                    label.resizeTextForBestFit = true;
                    label.resizeTextMinSize = 24;
                    label.resizeTextMaxSize = 64;
                    label.color = new Color(1f, 0.79f, 0.51f);
                    label.raycastTarget = false;
                }
                else label = existing.GetComponent<Text>();
                if (label != null)
                {
                    label.text = "PLUSH DEFENSE";
                    label.enabled = IsEnglish;
                }
            }
        }

        private static Dictionary<string, string> BuildReverse()
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> pair in English)
                if (!result.ContainsKey(pair.Value)) result.Add(pair.Value, pair.Key);
            return result;
        }
    }
}
