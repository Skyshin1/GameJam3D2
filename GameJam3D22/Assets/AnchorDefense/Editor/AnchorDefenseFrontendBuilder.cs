using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnchorDefense.Editor
{
    public static class AnchorDefenseFrontendBuilder
    {
        private const string Root = "Assets/AnchorDefense";
        private const string InputActionsPath = Root + "/Input/AnchorDefenseInputActions.inputactions";
        private const string MainMenuPrefabPath = Root + "/Prefabs/UI/MainMenuUI.prefab";
        private const string LoadingPrefabPath = Root + "/Prefabs/UI/LoadingUI.prefab";
        private const string PausePrefabPath = Root + "/Prefabs/UI/PauseMenuUI.prefab";
        private const string MainMenuScenePath = Root + "/Scenes/MainMenu.unity";
        private const string LoadingScenePath = Root + "/Scenes/Loading.unity";
        private const string GameplayScenePath = Root + "/Scenes/Gameplay.unity";
        private const string DirectionalScenePath = Root + "/Scenes/Gameplay_DirectionalSprites.unity";
        private const string BrightnessProfilePath = Root + "/Configs/FrontendBrightnessProfile.asset";

        private static readonly Color Background = new Color(0.008f, 0.018f, 0.045f, 1f);
        private static readonly Color Panel = new Color(0.025f, 0.055f, 0.105f, 0.98f);
        private static readonly Color Field = new Color(0.045f, 0.105f, 0.165f, 1f);
        private static readonly Color Cyan = new Color(0.16f, 0.86f, 1f, 1f);
        private static readonly Color Violet = new Color(0.66f, 0.38f, 1f, 1f);
        private static readonly Color Orange = new Color(1f, 0.5f, 0.16f, 1f);
        private static readonly Color TextPrimary = new Color(0.86f, 0.96f, 1f, 1f);
        private static DefaultControls.Resources uiResources;

        [MenuItem("Tools/Anchor Defense/Build Frontend and Input System")]
        public static void BuildAll()
        {
            BuildInternal(true);
        }

        public static void RepairAfterGameplayRebuild()
        {
            BuildInternal(false);
        }

        [MenuItem("Tools/Anchor Defense/Rebuild Shared Settings UI")]
        public static void RebuildSettingsUiOnly()
        {
            AssetDatabase.ImportAsset(MainMenuPrefabPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(PausePrefabPath, ImportAssetOptions.ForceUpdate);
            Debug.Log("Existing art-authored settings prefabs preserved. Device binding columns are extended at runtime.");
        }

        private static void BuildInternal(bool rebuildUi)
        {
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputActions == null)
            {
                Debug.LogError("Anchor Defense input actions asset is missing or has not been imported yet.");
                return;
            }

            VolumeProfile brightnessProfile = CreateBrightnessProfile();
            GameObject mainMenuPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainMenuPrefabPath);
            GameObject loadingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LoadingPrefabPath);
            GameObject pausePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PausePrefabPath);
            if (mainMenuPrefab == null)
            {
                mainMenuPrefab = CreateMainMenuPrefab(inputActions);
            }
            if (loadingPrefab == null || rebuildUi)
            {
                loadingPrefab = CreateLoadingPrefab();
            }
            if (pausePrefab == null)
            {
                pausePrefab = CreatePausePrefab(inputActions);
            }

            AssetDatabase.SaveAssets();
            CreateMainMenuScene(mainMenuPrefab, brightnessProfile);
            CreateLoadingScene(loadingPrefab);
            PatchGameplayScene(GameplayScenePath, inputActions, brightnessProfile, pausePrefab);
            PatchGameplayScene(DirectionalScenePath, inputActions, brightnessProfile, pausePrefab);
            SetBuildScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Anchor Defense frontend, settings, loading flow, and Input System refreshed successfully.");
        }

        private static VolumeProfile CreateBrightnessProfile()
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(BrightnessProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, BrightnessProfilePath);
            }
            if (!profile.TryGet(out ColorAdjustments adjustments))
            {
                adjustments = profile.Add<ColorAdjustments>(true);
            }
            adjustments.active = true;
            adjustments.postExposure.overrideState = true;
            adjustments.postExposure.value = 0f;
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static GameObject CreateMainMenuPrefab(InputActionAsset inputActions)
        {
            Font font = LoadEnglishUiFont();
            GameObject root = new GameObject(
                "MainMenuUI",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(MainMenuController),
                typeof(SettingsMenuController));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Image background = CreateImage("Deep Space Backdrop", root.transform, Background);
            Stretch(background.rectTransform, 0f);
            CreateDecorations(background.transform);

            Text kicker = CreateText("Kicker", background.transform, font, 18, TextAnchor.MiddleLeft, Cyan);
            SetRect(kicker.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(126f, -208f), new Vector2(660f, -165f));
            kicker.text = "ORBITAL DEFENSE NETWORK  //  ONLINE";
            Text title = CreateText("Game Title", background.transform, font, 64, TextAnchor.UpperLeft, TextPrimary);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(120f, -390f), new Vector2(840f, -220f));
            title.text = "ANCHOR\nDEFENSE";
            Text subtitle = CreateText("Subtitle", background.transform, font, 23, TextAnchor.UpperLeft, new Color(0.54f, 0.68f, 0.82f));
            SetRect(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(126f, -468f), new Vector2(790f, -395f));
            subtitle.text = "稳定轨道 · 锚定火力 · 守住星核";

            Image hub = CreateImage("Anchor Hub", background.transform, new Color(0.045f, 0.22f, 0.32f, 1f));
            RectTransform hubRect = hub.rectTransform;
            hubRect.anchorMin = hubRect.anchorMax = new Vector2(0.78f, 0.53f);
            hubRect.sizeDelta = new Vector2(330f, 330f);
            hubRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            AddOutline(hub.gameObject, Cyan, new Vector2(5f, -5f));
            Image hubInner = CreateImage("Anchor Core", hub.transform, new Color(0.015f, 0.065f, 0.1f, 1f));
            SetRect(hubInner.rectTransform, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f), Vector2.zero, Vector2.zero);
            Text hubText = CreateText("Hub Label", hubInner.transform, font, 27, TextAnchor.MiddleCenter, Cyan);
            Stretch(hubText.rectTransform, 8f);
            hubText.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            hubText.text = "ANCHOR\nCORE";

            Button startButton = CreateMenuButton("Start Game", background.transform, font, "开始游戏", new Vector2(126f, 270f), Cyan);
            Button settingsButton = CreateMenuButton("Settings", background.transform, font, "设置", new Vector2(126f, 188f), Violet);
            Button quitButton = CreateMenuButton("Quit", background.transform, font, "退出游戏", new Vector2(126f, 106f), Orange);
            Text version = CreateText("Version", background.transform, font, 16, TextAnchor.LowerRight, new Color(0.35f, 0.5f, 0.64f));
            SetRect(version.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-480f, 26f), new Vector2(-42f, 62f));
            version.text = "ANCHOR PROTOCOL BUILD 01";

            SettingsMenuController settingsController = root.GetComponent<SettingsMenuController>();
            CreateSettingsPanel(root.transform, font, inputActions, settingsController);
            MainMenuController mainMenu = root.GetComponent<MainMenuController>();
            mainMenu.Configure(startButton, settingsButton, quitButton, settingsController, inputActions);

            AssignUiFonts(root);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, MainMenuPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void CreateSettingsPanel(
            Transform parent,
            Font font,
            InputActionAsset inputActions,
            SettingsMenuController controller)
        {
            Image overlay = CreateImage("Settings Panel", parent, new Color(0.003f, 0.01f, 0.025f, 0.97f));
            Stretch(overlay.rectTransform, 0f);
            Image frame = CreateImage("Settings Frame", overlay.transform, Panel);
            SetRect(frame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-800f, -440f), new Vector2(800f, 440f));
            AddOutline(frame.gameObject, Cyan, new Vector2(3f, -3f));

            Text title = CreateText("Settings Title", frame.transform, font, 33, TextAnchor.MiddleLeft, TextPrimary);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0.7f, 1f), new Vector2(34f, -76f), new Vector2(0f, -18f));
            title.text = "ANCHOR SYSTEM SETTINGS  /  系统设置";
            Image headerLine = CreateImage("Header Line", frame.transform, Cyan);
            SetRect(headerLine.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(30f, -91f), new Vector2(-30f, -87f));

            Button closeButton = CreateSmallButton("Close Settings", frame.transform, font, "返回", new Vector2(-142f, -70f), new Vector2(110f, 44f), Orange, new Vector2(1f, 1f));
            Image tabRail = CreateImage("Category Rail", frame.transform, new Color(0.015f, 0.035f, 0.07f, 0.9f));
            SetRect(tabRail.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(30f, 32f), new Vector2(280f, -112f));
            AddOutline(tabRail.gameObject, new Color(0.08f, 0.3f, 0.44f), new Vector2(2f, -2f));

            string[] tabNames = { "显示", "图像", "声音", "控制" };
            Color[] tabColors = { Cyan, Violet, Orange, new Color(0.25f, 1f, 0.67f) };
            var tabButtons = new Button[tabNames.Length];
            for (int i = 0; i < tabNames.Length; i++)
            {
                tabButtons[i] = CreateTabButton(tabNames[i], tabRail.transform, font, tabNames[i], -65f - i * 78f, tabColors[i]);
            }

            RectTransform contentRoot = CreateRect("Category Content", frame.transform);
            SetRect(contentRoot, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(310f, 122f), new Vector2(-36f, -118f));
            var panels = new GameObject[4];
            for (int i = 0; i < panels.Length; i++)
            {
                Image category = CreateImage(tabNames[i] + " Panel", contentRoot, new Color(0.012f, 0.03f, 0.06f, 0.72f));
                Stretch(category.rectTransform, 0f);
                panels[i] = category.gameObject;
            }

            Dropdown resolution = CreateDropdown("Resolution", panels[0].transform, font, 104f);
            CreateRowLabel(panels[0].transform, font, "分辨率", 104f);
            Dropdown screenMode = CreateDropdown("Screen Mode", panels[0].transform, font, 182f);
            CreateRowLabel(panels[0].transform, font, "显示模式", 182f);
            Slider brightness = CreateSlider("Brightness", panels[0].transform, -2f, 2f, 260f);
            Text brightnessValue = CreateValueLabel(panels[0].transform, font, 260f);
            CreateRowLabel(panels[0].transform, font, "屏幕亮度", 260f);
            Toggle verticalSync = CreateToggle("Vertical Sync", panels[0].transform, font, 338f);
            CreateRowLabel(panels[0].transform, font, "垂直同步", 338f);
            Dropdown frameRate = CreateDropdown("Frame Rate", panels[0].transform, font, 416f);
            CreateRowLabel(panels[0].transform, font, "帧率上限", 416f);
            Toggle showZoneBorders = CreateToggle("Show Zone Borders Outside Edit", panels[0].transform, font, 494f);
            CreateRowLabel(panels[0].transform, font, "非编辑状态显示区域边框", 494f);

            Dropdown quality = CreateDropdown("Quality", panels[1].transform, font, 104f);
            CreateRowLabel(panels[1].transform, font, "图像质量", 104f);
            Dropdown antiAliasing = CreateDropdown("Anti Aliasing", panels[1].transform, font, 182f);
            CreateRowLabel(panels[1].transform, font, "抗锯齿方案", 182f);
            Text graphicsHint = CreateText("Graphics Hint", panels[1].transform, font, 19, TextAnchor.UpperLeft, new Color(0.52f, 0.68f, 0.82f));
            SetRect(graphicsHint.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(48f, -360f), new Vector2(-48f, -248f));
            graphicsHint.text = "图像质量会切换项目已有的 Performant、Balanced、High Fidelity 三档 URP 配置。\nSMAA 适合当前固定视角，FXAA 性能开销更低。";

            Slider master = CreateSlider("Master Volume", panels[2].transform, 0f, 1f, 104f);
            Text masterValue = CreateValueLabel(panels[2].transform, font, 104f);
            CreateRowLabel(panels[2].transform, font, "主音量", 104f);
            Slider music = CreateSlider("Music Volume", panels[2].transform, 0f, 1f, 182f);
            Text musicValue = CreateValueLabel(panels[2].transform, font, 182f);
            CreateRowLabel(panels[2].transform, font, "音乐音量", 182f);
            Slider sfx = CreateSlider("SFX Volume", panels[2].transform, 0f, 1f, 260f);
            Text sfxValue = CreateValueLabel(panels[2].transform, font, 260f);
            CreateRowLabel(panels[2].transform, font, "音效音量", 260f);
            Text audioHint = CreateText("Audio Hint", panels[2].transform, font, 18, TextAnchor.UpperLeft, new Color(0.52f, 0.68f, 0.82f));
            SetRect(audioHint.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(48f, -405f), new Vector2(-48f, -330f));
            audioHint.text = "音乐和音效通道已预留；后续 AudioSource 挂载 AudioCategorySource 即可自动接入。";

            Slider ringSensitivity = CreateSlider("Ring Sensitivity", panels[3].transform, 0.25f, 2f, 74f);
            Text ringValue = CreateValueLabel(panels[3].transform, font, 74f);
            CreateRowLabel(panels[3].transform, font, "轨道拖拽灵敏度", 74f);
            Slider cameraSensitivity = CreateSlider("Camera Sensitivity", panels[3].transform, 0.25f, 2f, 136f);
            Text cameraValue = CreateValueLabel(panels[3].transform, font, 136f);
            CreateRowLabel(panels[3].transform, font, "相机环绕灵敏度", 136f);

            Text keyboardHeader = CreateText("Keyboard Header", panels[3].transform, font, 16,
                TextAnchor.MiddleCenter, Cyan);
            SetRect(keyboardHeader.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-545f, -205f), new Vector2(-315f, -170f));
            keyboardHeader.text = "键鼠";
            Text gamepadHeader = CreateText("Gamepad Header", panels[3].transform, font, 16,
                TextAnchor.MiddleCenter, new Color(0.25f, 1f, 0.67f));
            SetRect(gamepadHeader.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-265f, -205f), new Vector2(-35f, -170f));
            gamepadHeader.text = "手柄";

            string[] actionLabels =
            {
                "选择 / 切换轨道", "旋转相机", "切换相机模式",
                "打开升级树", "锚域编织", "暂停 / 返回"
            };
            string[] keyboardActions =
            {
                "PrimaryPress", "SecondaryPress", "CycleCamera",
                "ToggleUpgrade", "ToggleZoneEdit", "Pause"
            };
            string[] gamepadActions =
            {
                "CycleRing", "CameraOrbitPress", "CycleCamera",
                "ToggleUpgrade", "ToggleZoneEdit", "Pause"
            };
            var rebindRows = new List<InputRebindRow>(actionLabels.Length * 2);
            for (int i = 0; i < actionLabels.Length; i++)
            {
                CreateDualRebindRow(panels[3].transform, font, actionLabels[i],
                    keyboardActions[i], gamepadActions[i], 210f + i * 54f, rebindRows);
            }
            Text stickHint = CreateText("Gamepad Stick Hint", panels[3].transform, font, 15,
                TextAnchor.MiddleLeft, new Color(0.55f, 0.76f, 0.88f));
            SetRect(stickHint.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(48f, 72f), new Vector2(-280f, 108f));
            stickHint.text = "手柄：左摇杆旋转当前轨道 · 按住右摇杆并推动可环绕相机 · 十字键/左摇杆导航菜单";
            Button resetBindings = CreateSmallButton("Reset Bindings", panels[3].transform, font, "恢复默认键位", new Vector2(-180f, 34f), new Vector2(220f, 46f), Violet, new Vector2(1f, 0f));

            Button resetDefaults = CreateSmallButton("Reset Defaults", frame.transform, font, "恢复默认设置", new Vector2(-430f, 60f), new Vector2(210f, 50f), Violet, new Vector2(1f, 0f));
            Button apply = CreateSmallButton("Apply", frame.transform, font, "应用设置", new Vector2(-185f, 60f), new Vector2(210f, 50f), Cyan, new Vector2(1f, 0f));

            controller.ConfigureView(
                overlay.gameObject, closeButton, apply, resetDefaults, resetBindings, tabButtons, panels,
                resolution, screenMode, brightness, brightnessValue, verticalSync, showZoneBorders, frameRate,
                quality, antiAliasing, master, masterValue, music, musicValue, sfx, sfxValue,
                ringSensitivity, ringValue, cameraSensitivity, cameraValue, inputActions, rebindRows.ToArray());
            overlay.gameObject.SetActive(false);
        }

        private static GameObject CreateLoadingPrefab()
        {
            Font font = LoadEnglishUiFont();
            GameObject root = new GameObject(
                "LoadingUI",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(LoadingScreenController));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            Image background = CreateImage("Loading Background", root.transform, Background);
            Stretch(background.rectTransform, 0f);
            CreateDecorations(background.transform);
            Image anchor = CreateImage("Rotating Anchor", background.transform, new Color(0.04f, 0.27f, 0.38f, 1f));
            RectTransform anchorRect = anchor.rectTransform;
            anchorRect.anchorMin = anchorRect.anchorMax = new Vector2(0.5f, 0.58f);
            anchorRect.sizeDelta = new Vector2(170f, 170f);
            anchorRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            AddOutline(anchor.gameObject, Cyan, new Vector2(4f, -4f));
            Text anchorText = CreateText("Anchor Mark", anchor.transform, font, 22, TextAnchor.MiddleCenter, Cyan);
            Stretch(anchorText.rectTransform, 8f);
            anchorText.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            anchorText.text = "ANCHOR";

            Text title = CreateText("Loading Title", background.transform, font, 34, TextAnchor.MiddleCenter, TextPrimary);
            SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420f, -45f), new Vector2(420f, 20f));
            title.text = "正在连接轨道防御网络";
            Text status = CreateText("Loading Status", background.transform, font, 20, TextAnchor.MiddleCenter, new Color(0.55f, 0.74f, 0.88f));
            SetRect(status.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420f, -100f), new Vector2(420f, -55f));
            status.text = "正在建立锚定连接…";

            Image barBackground = CreateImage("Progress Background", background.transform, new Color(0.025f, 0.08f, 0.13f, 1f));
            SetRect(barBackground.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-420f, 155f), new Vector2(420f, 181f));
            AddOutline(barBackground.gameObject, new Color(0.08f, 0.35f, 0.48f), new Vector2(2f, -2f));
            Image fill = CreateImage("Progress Fill", barBackground.transform, Cyan);
            Stretch(fill.rectTransform, 4f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 0f;
            Text percentage = CreateText("Progress Text", background.transform, font, 19, TextAnchor.MiddleCenter, TextPrimary);
            SetRect(percentage.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-150f, 101f), new Vector2(150f, 142f));
            percentage.text = "0%";
            Text tip = CreateText("Tip", background.transform, font, 17, TextAnchor.LowerCenter, new Color(0.38f, 0.55f, 0.68f));
            SetRect(tip.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-620f, 35f), new Vector2(620f, 82f));
            tip.text = "提示：旋转轨道能够改变炮台阵列的拦截方向";

            root.GetComponent<LoadingScreenController>().Configure(fill, percentage, status, anchorRect);
            AssignUiFonts(root);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, LoadingPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreatePausePrefab(InputActionAsset inputActions)
        {
            Font font = LoadEnglishUiFont();
            GameObject root = new GameObject(
                "PauseMenuUI",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(PauseMenuController),
                typeof(SettingsMenuController));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            Image overlay = CreateImage("Pause Panel", root.transform, new Color(0.003f, 0.012f, 0.03f, 0.88f));
            Stretch(overlay.rectTransform, 0f);
            Image frame = CreateImage("Pause Frame", overlay.transform, Panel);
            SetRect(frame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-290f, -270f), new Vector2(290f, 270f));
            AddOutline(frame.gameObject, Cyan, new Vector2(3f, -3f));
            Text title = CreateText("Pause Title", frame.transform, font, 37, TextAnchor.MiddleCenter, TextPrimary);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(25f, -110f), new Vector2(-25f, -35f));
            title.text = "ANCHOR PAUSED";
            Text subtitle = CreateText("Pause Subtitle", frame.transform, font, 18, TextAnchor.MiddleCenter, new Color(0.5f, 0.7f, 0.84f));
            SetRect(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(25f, -152f), new Vector2(-25f, -110f));
            subtitle.text = "轨道防御网络已暂时冻结";
            Button resume = CreateSmallButton("Resume", frame.transform, font, "继续游戏", new Vector2(0f, 65f), new Vector2(330f, 58f), Cyan, new Vector2(0.5f, 0.5f));
            Button settings = CreateSmallButton("Settings", frame.transform, font, "系统设置", new Vector2(0f, -10f), new Vector2(330f, 58f), Violet, new Vector2(0.5f, 0.5f));
            Button mainMenu = CreateSmallButton("Main Menu", frame.transform, font, "返回主菜单", new Vector2(0f, -85f), new Vector2(330f, 58f), Orange, new Vector2(0.5f, 0.5f));
            Text hint = CreateText("Pause Hint", frame.transform, font, 16, TextAnchor.MiddleCenter, new Color(0.38f, 0.55f, 0.68f));
            SetRect(hint.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(20f, 18f), new Vector2(-20f, 52f));
            hint.text = "按 Esc 继续";
            SettingsMenuController settingsController = root.GetComponent<SettingsMenuController>();
            CreateSettingsPanel(root.transform, font, inputActions, settingsController);
            root.GetComponent<PauseMenuController>().ConfigureView(
                overlay.gameObject, resume, settings, mainMenu, settingsController);
            overlay.gameObject.SetActive(false);

            AssignUiFonts(root);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PausePrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void CreateMainMenuScene(GameObject uiPrefab, VolumeProfile profile)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MainMenu";
            Camera camera = CreateCamera(Background);
            Volume volume = CreateSettingsVolume(profile);
            SceneSettingsApplier applier = new GameObject("Settings Applier").AddComponent<SceneSettingsApplier>();
            applier.Configure(camera, volume);
            PrefabUtility.InstantiatePrefab(uiPrefab, scene);
            CreateInputSystemEventSystem();
            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        }

        private static void CreateLoadingScene(GameObject uiPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Loading";
            CreateCamera(Background);
            PrefabUtility.InstantiatePrefab(uiPrefab, scene);
            EditorSceneManager.SaveScene(scene, LoadingScenePath);
        }

        private static void PatchGameplayScene(
            string scenePath,
            InputActionAsset actions,
            VolumeProfile profile,
            GameObject pausePrefab)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                return;
            }
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameBootstrap bootstrap = Object.FindObjectOfType<GameBootstrap>(true);
            if (bootstrap == null)
            {
                return;
            }
            GameObject systems = bootstrap.gameObject;
            GameInputController input = systems.GetComponent<GameInputController>();
            if (input == null)
            {
                input = systems.AddComponent<GameInputController>();
            }
            input.Configure(actions);
            bootstrap.ConfigureInput(input);

            Transform volumeTransform = systems.transform.Find("Settings Volume");
            GameObject volumeObject = volumeTransform != null ? volumeTransform.gameObject : new GameObject("Settings Volume");
            volumeObject.transform.SetParent(systems.transform, false);
            Volume volume = volumeObject.GetComponent<Volume>();
            if (volume == null)
            {
                volume = volumeObject.AddComponent<Volume>();
            }
            volume.isGlobal = true;
            volume.priority = 100f;
            volume.sharedProfile = profile;
            SceneSettingsApplier applier = systems.GetComponent<SceneSettingsApplier>();
            if (applier == null)
            {
                applier = systems.AddComponent<SceneSettingsApplier>();
            }
            applier.Configure(Camera.main, volume);
            PauseMenuController pauseMenu = Object.FindObjectOfType<PauseMenuController>(true);
            if (pauseMenu == null && pausePrefab != null)
            {
                GameObject pauseInstance = (GameObject)PrefabUtility.InstantiatePrefab(pausePrefab, scene);
                pauseInstance.name = "PauseMenuUI";
                pauseMenu = pauseInstance.GetComponent<PauseMenuController>();
            }
            GameFlowController gameFlow = systems.GetComponent<GameFlowController>();
            UpgradeTreeController upgradeTree = Object.FindObjectOfType<UpgradeTreeController>(true);
            pauseMenu?.ConfigureRuntime(input, gameFlow, upgradeTree);
            ReplaceEventSystem();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static void ReplaceEventSystem()
        {
            EventSystem eventSystem = Object.FindObjectOfType<EventSystem>(true);
            if (eventSystem == null)
            {
                CreateInputSystemEventSystem();
                return;
            }
            StandaloneInputModule legacy = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacy != null)
            {
                Object.DestroyImmediate(legacy);
            }
            InputSystemUIInputModule module = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (module == null)
            {
                module = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
            module.AssignDefaultActions();
        }

        private static EventSystem CreateInputSystemEventSystem()
        {
            GameObject root = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            root.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            return root.GetComponent<EventSystem>();
        }

        private static Camera CreateCamera(Color backgroundColor)
        {
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = backgroundColor;
            camera.orthographic = true;
            return camera;
        }

        private static Volume CreateSettingsVolume(VolumeProfile profile)
        {
            GameObject volumeObject = new GameObject("Settings Volume");
            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;
            volume.sharedProfile = profile;
            return volume;
        }

        private static void SetBuildScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(LoadingScenePath, true),
                new EditorBuildSettingsScene(GameplayScenePath, true)
            };
        }

        private static void CreateDecorations(Transform parent)
        {
            for (int i = 0; i < 4; i++)
            {
                Image line = CreateImage("Orbit Trace", parent, new Color(0.05f, 0.3f, 0.44f, 0.45f));
                RectTransform rect = line.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.78f, 0.53f);
                rect.sizeDelta = new Vector2(650f + i * 115f, 2f);
                rect.localRotation = Quaternion.Euler(0f, 0f, 18f + i * 37f);
            }
            Image rail = CreateImage("Left Energy Rail", parent, Cyan);
            SetRect(rail.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(72f, 74f), new Vector2(77f, -74f));
        }

        private static Button CreateMenuButton(string name, Transform parent, Font font, string label, Vector2 position, Color accent)
        {
            Image image = CreateImage(name, parent, new Color(0.025f, 0.1f, 0.15f, 0.97f));
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(390f, 62f);
            rect.pivot = Vector2.zero;
            AddOutline(image.gameObject, accent, new Vector2(3f, -3f));
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            SetButtonColors(button, accent);
            Text text = CreateText("Label", image.transform, font, 24, TextAnchor.MiddleLeft, TextPrimary);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(30f, 0f), new Vector2(-18f, 0f));
            text.text = label;
            return button;
        }

        private static Button CreateTabButton(string name, Transform parent, Font font, string label, float topOffset, Color accent)
        {
            Image image = CreateImage(name + " Tab", parent, new Color(0.035f, 0.085f, 0.13f, 1f));
            SetRect(image.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, topOffset - 58f), new Vector2(-18f, topOffset));
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            SetButtonColors(button, accent);
            Text text = CreateText("Label", image.transform, font, 22, TextAnchor.MiddleLeft, TextPrimary);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(24f, 0f), new Vector2(-10f, 0f));
            text.text = label;
            return button;
        }

        private static Button CreateSmallButton(
            string name,
            Transform parent,
            Font font,
            string label,
            Vector2 anchoredPosition,
            Vector2 size,
            Color accent,
            Vector2 anchor)
        {
            Image image = CreateImage(name, parent, new Color(0.035f, 0.11f, 0.16f, 1f));
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            AddOutline(image.gameObject, accent, new Vector2(2f, -2f));
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            SetButtonColors(button, accent);
            Text text = CreateText("Label", image.transform, font, 19, TextAnchor.MiddleCenter, TextPrimary);
            Stretch(text.rectTransform, 4f);
            text.text = label;
            return button;
        }

        private static Dropdown CreateDropdown(string name, Transform parent, Font font, float top)
        {
            GameObject dropdownObject = DefaultControls.CreateDropdown(GetUiResources());
            dropdownObject.name = name;
            dropdownObject.transform.SetParent(parent, false);
            RectTransform rect = dropdownObject.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(420f, -top - 50f), new Vector2(-56f, -top));
            Dropdown dropdown = dropdownObject.GetComponent<Dropdown>();
            StyleDropdown(dropdown, font);
            return dropdown;
        }

        private static Slider CreateSlider(string name, Transform parent, float min, float max, float top)
        {
            GameObject sliderObject = DefaultControls.CreateSlider(GetUiResources());
            sliderObject.name = name;
            sliderObject.transform.SetParent(parent, false);
            RectTransform rect = sliderObject.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(420f, -top - 42f), new Vector2(-160f, -top - 8f));
            Slider slider = sliderObject.GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            sliderObject.AddComponent<SliderStepQuantizer>().Configure(10);
            Image[] images = sliderObject.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                images[i].color = images[i].gameObject.name == "Handle" ? Cyan : Field;
            }
            for (int i = 0; i <= 10; i++)
            {
                Image tick = CreateImage("Step " + i, sliderObject.transform, new Color(0.16f, 0.86f, 1f, 0.38f));
                tick.raycastTarget = false;
                RectTransform tickRect = tick.rectTransform;
                float anchor = i / 10f;
                tickRect.anchorMin = tickRect.anchorMax = new Vector2(anchor, 0.5f);
                tickRect.anchoredPosition = Vector2.zero;
                tickRect.sizeDelta = new Vector2(i % 5 == 0 ? 3f : 2f, i % 5 == 0 ? 12f : 8f);
            }
            return slider;
        }

        private static Toggle CreateToggle(string name, Transform parent, Font font, float top)
        {
            GameObject toggleObject = DefaultControls.CreateToggle(GetUiResources());
            toggleObject.name = name;
            toggleObject.transform.SetParent(parent, false);
            RectTransform rect = toggleObject.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(420f, -top - 46f), new Vector2(620f, -top));
            Text label = toggleObject.GetComponentInChildren<Text>();
            label.font = font;
            label.color = TextPrimary;
            label.text = "启用";
            return toggleObject.GetComponent<Toggle>();
        }

        private static InputRebindRow CreateRebindRow(Transform parent, Font font, string label, string actionName, float top)
        {
            CreateRowLabel(parent, font, label, top);
            Button button = CreateSmallButton(actionName + " Rebind", parent, font, "--", new Vector2(-310f, -top - 25f), new Vector2(360f, 46f), Cyan, new Vector2(1f, 1f));
            Text bindingText = button.GetComponentInChildren<Text>();
            InputRebindRow row = button.gameObject.AddComponent<InputRebindRow>();
            row.Configure("Gameplay", actionName, 0, button, bindingText);
            return row;
        }

        private static void CreateDualRebindRow(Transform parent, Font font, string label,
            string keyboardAction, string gamepadAction, float top, List<InputRebindRow> rows)
        {
            CreateRowLabel(parent, font, label, top);
            Button keyboardButton = CreateSmallButton(keyboardAction + " Keyboard Rebind", parent, font, "--",
                new Vector2(-430f, -top - 24f), new Vector2(230f, 42f), Cyan, new Vector2(1f, 1f));
            InputRebindRow keyboardRow = keyboardButton.gameObject.AddComponent<InputRebindRow>();
            keyboardRow.Configure("Gameplay", keyboardAction, "Keyboard&Mouse",
                keyboardButton, keyboardButton.GetComponentInChildren<Text>());
            rows.Add(keyboardRow);

            Button gamepadButton = CreateSmallButton(gamepadAction + " Gamepad Rebind", parent, font, "--",
                new Vector2(-150f, -top - 24f), new Vector2(230f, 42f),
                new Color(0.25f, 1f, 0.67f), new Vector2(1f, 1f));
            InputRebindRow gamepadRow = gamepadButton.gameObject.AddComponent<InputRebindRow>();
            gamepadRow.Configure("Gameplay", gamepadAction, "Gamepad",
                gamepadButton, gamepadButton.GetComponentInChildren<Text>());
            rows.Add(gamepadRow);
        }

        private static void CreateRowLabel(Transform parent, Font font, string text, float top)
        {
            Text label = CreateText(text + " Label", parent, font, 20, TextAnchor.MiddleLeft, TextPrimary);
            SetRect(label.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -top - 50f), new Vector2(390f, -top));
            label.text = text;
        }

        private static Text CreateValueLabel(Transform parent, Font font, float top)
        {
            Text label = CreateText("Value", parent, font, 18, TextAnchor.MiddleRight, Cyan);
            SetRect(label.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-145f, -top - 48f), new Vector2(-55f, -top));
            return label;
        }

        private static void StyleGeneratedControl(GameObject control, Font font)
        {
            Image rootImage = control.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.color = Field;
            }
            Text[] texts = control.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                texts[i].font = font;
                texts[i].fontSize = 18;
                texts[i].color = TextPrimary;
                texts[i].verticalOverflow = VerticalWrapMode.Overflow;
            }
        }

        private static void StyleDropdown(Dropdown dropdown, Font font)
        {
            StyleGeneratedControl(dropdown.gameObject, font);

            if (dropdown.captionText != null)
            {
                dropdown.captionText.color = TextPrimary;
                dropdown.captionText.verticalOverflow = VerticalWrapMode.Overflow;
            }
            if (dropdown.itemText != null)
            {
                dropdown.itemText.color = TextPrimary;
                dropdown.itemText.verticalOverflow = VerticalWrapMode.Overflow;
            }

            RectTransform template = dropdown.template;
            if (template == null)
            {
                return;
            }

            Image templateBackground = template.GetComponent<Image>();
            if (templateBackground != null)
            {
                templateBackground.color = Panel;
            }

            Transform itemRoot = template.Find("Viewport/Content/Item");
            if (itemRoot != null)
            {
                Image itemBackground = itemRoot.Find("Item Background")?.GetComponent<Image>();
                Toggle itemToggle = itemRoot.GetComponent<Toggle>();
                if (itemBackground != null)
                {
                    itemBackground.color = Color.white;
                }
                if (itemToggle != null)
                {
                    ColorBlock colors = itemToggle.colors;
                    colors.normalColor = new Color(0.025f, 0.055f, 0.105f, 1f);
                    colors.highlightedColor = new Color(0.07f, 0.3f, 0.4f, 1f);
                    colors.pressedColor = new Color(0.09f, 0.42f, 0.52f, 1f);
                    colors.selectedColor = new Color(0.055f, 0.22f, 0.32f, 1f);
                    itemToggle.colors = colors;
                }

                Image checkmark = itemRoot.Find("Item Checkmark")?.GetComponent<Image>();
                if (checkmark != null)
                {
                    checkmark.color = Cyan;
                }

                Text itemLabel = itemRoot.Find("Item Label")?.GetComponent<Text>();
                if (itemLabel != null)
                {
                    itemLabel.font = font;
                    itemLabel.fontSize = 18;
                    itemLabel.color = TextPrimary;
                    itemLabel.verticalOverflow = VerticalWrapMode.Overflow;
                }
            }

            Transform scrollbarRoot = template.Find("Scrollbar");
            Image scrollbarBackground = scrollbarRoot?.GetComponent<Image>();
            if (scrollbarBackground != null)
            {
                scrollbarBackground.color = new Color(0.012f, 0.03f, 0.06f, 1f);
            }
            Image handle = scrollbarRoot?.Find("Sliding Area/Handle")?.GetComponent<Image>();
            if (handle != null)
            {
                handle.color = Cyan;
            }
        }

        private static void SetButtonColors(Button button, Color accent)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.Lerp(Color.white, accent, 0.3f);
            colors.pressedColor = Color.Lerp(Color.white, accent, 0.55f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.3f, 0.35f, 0.4f, 0.6f);
            button.colors = colors;
        }

        private static DefaultControls.Resources GetUiResources()
        {
            if (uiResources.standard != null)
            {
                return uiResources;
            }
            uiResources.standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            uiResources.background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            uiResources.inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");
            uiResources.knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            uiResources.checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
            uiResources.dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd");
            uiResources.mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd");
            return uiResources;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            return root.GetComponent<RectTransform>();
        }

        private static Text CreateText(string name, Transform parent, Font font, int size, TextAnchor alignment, Color color)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            root.transform.SetParent(parent, false);
            Text text = root.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            return text;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(parent, false);
            Image image = root.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static void AddOutline(GameObject target, Color color, Vector2 distance)
        {
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        private static void Stretch(RectTransform rect, float margin)
        {
            SetRect(rect, Vector2.zero, Vector2.one, Vector2.one * margin, Vector2.one * -margin);
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
        private const string EnglishUiFontPath = "Assets/AnchorDefense/Art/UI/DomeaScrawl-Regular.ttf";
        private const string ChineseUiFontPath = "Assets/AnchorDefense/Art/UI/PF频凡胡涂体 PFANHUTUTI.ttf";

        private static Font LoadEnglishUiFont()
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(EnglishUiFontPath);
            return font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static Font LoadChineseUiFont()
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(ChineseUiFontPath);
            return font != null ? font : LoadEnglishUiFont();
        }

        private static void AssignUiFonts(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            Font english = LoadEnglishUiFont();
            Font chinese = LoadChineseUiFont();
            Text[] texts = root.GetComponentsInChildren<Text>(true);
            foreach (Text text in texts)
            {
                string value = string.IsNullOrEmpty(text.text) ? GetTransformPath(text.transform) : text.text;
                text.font = ContainsCjk(value) ? chinese : english;
                EditorUtility.SetDirty(text);
            }
        }

        private static bool ContainsCjk(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            foreach (char c in value)
            {
                if ((c >= 0x3400 && c <= 0x4DBF) ||
                    (c >= 0x4E00 && c <= 0x9FFF) ||
                    (c >= 0xF900 && c <= 0xFAFF))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetTransformPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }
    }
}
