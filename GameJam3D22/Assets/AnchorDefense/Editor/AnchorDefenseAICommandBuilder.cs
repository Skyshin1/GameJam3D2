using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnchorDefense.Editor
{
    public static class AnchorDefenseAICommandBuilder
    {
        private const string Root = "Assets/AnchorDefense";
        private const string ConfigFolder = Root + "/Configs/AI";
        private const string SkillFolder = ConfigFolder + "/Skills";
        private const string EffectFolder = ConfigFolder + "/Effects";
        private const string ConfigPath = ConfigFolder + "/AICommandConfig.asset";
        private const string UiPrefabPath = Root + "/Prefabs/UI/AICommandConsoleUI.prefab";
        private const string GameplayScenePath = Root + "/Scenes/Gameplay.unity";
        private const string FontSourcePath = Root + "/Art/UI/PF频凡胡涂体 PFANHUTUTI.ttf";
        private const string TmpFontPath = Root + "/Art/UI/AnchorChineseTMP.asset";

        [MenuItem("Tools/Anchor Defense/AI/Build Natural Language Command System")]
        public static void BuildAll()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("已取消构建锚核指令系统：当前场景尚未保存。");
                return;
            }

            EnsureFolder(ConfigFolder);
            EnsureFolder(SkillFolder);
            EnsureFolder(EffectFolder);

            AreaDamageSkillEffect damage = CreateOrLoad<AreaDamageSkillEffect>(
                EffectFolder + "/AnchorStrikeDamage.asset");
            TurretRepairSkillEffect repair = CreateOrLoad<TurretRepairSkillEffect>(
                EffectFolder + "/RepairPulseHealing.asset");
            EnemyRepulsionSkillEffect repulsion = CreateOrLoad<EnemyRepulsionSkillEffect>(
                EffectFolder + "/RepulsionWavePush.asset");
            EnemySlowSkillEffect slow = CreateOrLoad<EnemySlowSkillEffect>(
                EffectFolder + "/EnemySlowField.asset");
            TurretBoostSkillEffect rapid = CreateOrLoad<TurretBoostSkillEffect>(
                EffectFolder + "/TurretRapidField.asset");
            TurretBoostSkillEffect power = CreateOrLoad<TurretBoostSkillEffect>(
                EffectFolder + "/TurretPowerField.asset");
            RingRotationSkillEffect ringRotation = CreateOrLoad<RingRotationSkillEffect>(
                EffectFolder + "/RingRotation.asset");

            ActiveSkillDefinition anchorStrike = CreateOrLoad<ActiveSkillDefinition>(
                SkillFolder + "/AnchorStrike.asset");
            if (string.IsNullOrWhiteSpace(anchorStrike.Id))
            {
                anchorStrike.Configure("anchor_strike", "锚星轰击",
                    "攻击技能，对目标区域内所有敌人造成一次范围伤害，适合敌人密集区域。",
                    new[] { "轰击", "攻击", "轰炸", "消灭", "敌人" },
                    ActiveSkillTargetPolicy.EnemyRichZone, damage,
                    AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/VFX/DeathEffect.prefab"), 2f);
            }

            ActiveSkillDefinition repairPulse = CreateOrLoad<ActiveSkillDefinition>(
                SkillFolder + "/RepairPulse.asset");
            if (string.IsNullOrWhiteSpace(repairPulse.Id))
            {
                repairPulse.Configure("repair_pulse", "修复脉冲",
                    "治疗技能，恢复目标区域内所有存活炮塔的生命，适合受损炮塔集中的区域。",
                    new[] { "修复", "治疗", "恢复", "炮塔", "生命" },
                    ActiveSkillTargetPolicy.DamagedTurretZone, repair,
                    AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/VFX/Heel.prefab"), 2.5f);
            }

            ActiveSkillDefinition repulsionWave = CreateOrLoad<ActiveSkillDefinition>(
                SkillFolder + "/RepulsionWave.asset");
            if (string.IsNullOrWhiteSpace(repulsionWave.Id))
            {
                repulsionWave.Configure("repulsion_wave", "斥力浪潮",
                    "控制技能，把目标区域内所有敌人向远离星球核心的方向击退。",
                    new[] { "击退", "推开", "远离", "控制", "敌人" },
                    ActiveSkillTargetPolicy.EnemyRichZone, repulsion,
                    AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/VFX/HCFX_Stun1.prefab"), 2f);
            }

            ActiveSkillDefinition slowField = CreateOrLoad<ActiveSkillDefinition>(
                SkillFolder + "/SlowField.asset");
            if (string.IsNullOrWhiteSpace(slowField.Id))
            {
                slowField.Configure("slow_field", "凝滞力场",
                    "在目标区域生成持续减速场，对当前和后来进入的敌人生效。",
                    new[] { "减速", "变慢", "冰冻", "迟缓" },
                    ActiveSkillTargetPolicy.EnemyRichZone, slow,
                    AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/VFX/HCFX_Stun1.prefab"), 2f);
            }

            ActiveSkillDefinition rapidField = CreateOrLoad<ActiveSkillDefinition>(
                SkillFolder + "/TurretRapidField.asset");
            if (string.IsNullOrWhiteSpace(rapidField.Id))
            {
                rapid.Configure(0.6f, 1f);
                rapidField.Configure("rapid_field", "速射增幅",
                    "让目标区域内的炮塔在指令场持续期间加快发射速度。",
                    new[] { "射速", "速射", "攻速", "开火" },
                    ActiveSkillTargetPolicy.TurretRichZone, rapid,
                    AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/VFX/HCFX_Stun1.prefab"), 2f);
            }
            ActiveSkillDefinition powerField = CreateOrLoad<ActiveSkillDefinition>(
                SkillFolder + "/TurretPowerField.asset");
            if (string.IsNullOrWhiteSpace(powerField.Id))
            {
                power.Configure(1f, 1.7f);
                powerField.Configure("power_field", "火力增幅",
                    "让目标区域内的炮塔在指令场持续期间提高子弹伤害。",
                    new[] { "炮塔伤害", "火力", "威力", "强化" },
                    ActiveSkillTargetPolicy.TurretRichZone, power,
                    AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/VFX/Heel.prefab"), 2f);
            }

            ActiveSkillDefinition ringRotationSkill = CreateOrLoad<ActiveSkillDefinition>(
                SkillFolder + "/RingRotation.asset");
            if (string.IsNullOrWhiteSpace(ringRotationSkill.Id))
            {
                ringRotationSkill.Configure("rotate_ring", "星环调度",
                    "操控已有星环，默认持续接管，直到玩家停止或手动控制；可指定内中外轨和方向，仅明确要求单次转动时才转一次。",
                    new[] { "轨道", "星环", "旋转", "转动星环", "星环调度" },
                    ActiveSkillTargetPolicy.AnyValidZone, ringRotation);
            }

            AICommandConfig config = CreateOrLoad<AICommandConfig>(ConfigPath);
            if (config.Skills == null || config.Skills.Length == 0)
            {
                config.Configure("https://api.deepseek.com", "deepseek-flash", 0.1f, 12f,
                    10, 120, new[] { anchorStrike, repairPulse, repulsionWave,
                        slowField, rapidField, powerField, ringRotationSkill });
            }
            else
            {
                EnsureSkillInConfig(config, slowField);
                EnsureSkillInConfig(config, rapidField);
                EnsureSkillInConfig(config, powerField);
                EnsureSkillInConfig(config, ringRotationSkill);
            }

            TMP_FontAsset font = CreateOrLoadTmpFont();
            GameObject uiPrefab = BuildUiPrefab(font);
            InstallIntoGameplay(config, uiPrefab);

            EditorUtility.SetDirty(damage);
            EditorUtility.SetDirty(repair);
            EditorUtility.SetDirty(repulsion);
            EditorUtility.SetDirty(slow);
            EditorUtility.SetDirty(rapid);
            EditorUtility.SetDirty(power);
            EditorUtility.SetDirty(ringRotation);
            EditorUtility.SetDirty(anchorStrike);
            EditorUtility.SetDirty(repairPulse);
            EditorUtility.SetDirty(repulsionWave);
            EditorUtility.SetDirty(slowField);
            EditorUtility.SetDirty(rapidField);
            EditorUtility.SetDirty(powerField);
            EditorUtility.SetDirty(ringRotationSkill);
            EditorUtility.SetDirty(config);
            AssetDatabase.ImportAsset(Root + "/Input/AnchorDefenseInputActions.inputactions",
                ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AnchorAIVisualInstaller.Install();
            Debug.Log("Anchor 自然语言技能指令系统已创建并接入 Gameplay 场景。");
        }

        private static void EnsureSkillInConfig(AICommandConfig config, ActiveSkillDefinition skill)
        {
            if (config == null || skill == null ||
                System.Array.Exists(config.Skills, candidate => candidate == skill)) return;
            SerializedObject serialized = new SerializedObject(config);
            SerializedProperty skills = serialized.FindProperty("<Skills>k__BackingField");
            int index = skills.arraySize;
            skills.InsertArrayElementAtIndex(index);
            skills.GetArrayElementAtIndex(index).objectReferenceValue = skill;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject BuildUiPrefab(TMP_FontAsset font)
        {
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabPath);
            if (existingPrefab != null)
            {
                return existingPrefab;
            }

            GameObject canvasObject = new GameObject("AI Command Console UI",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 45;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Image openImage = CreateImage("Open Command Console", canvasObject.transform,
                new Color(0.35f, 0.12f, 0.35f, 0.96f));
            SetRect(openImage.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-215f, 22f), new Vector2(215f, 82f));
            Button openButton = openImage.gameObject.AddComponent<Button>();
            openButton.targetGraphic = openImage;
            TMP_Text openLabel = CreateText("Label", openImage.transform, font, 25,
                TextAlignmentOptions.Center, new Color(1f, 0.88f, 0.65f));
            Stretch(openLabel.rectTransform, 12f);
            openLabel.text = "锚核指令终端  [T]";

            Image panelImage = CreateImage("Command Panel", canvasObject.transform,
                new Color(0.025f, 0.018f, 0.065f, 0.97f));
            SetRect(panelImage.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-470f, 25f), new Vector2(470f, 245f));
            Outline outline = panelImage.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.18f, 0.88f, 1f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);

            TMP_Text title = CreateText("Title", panelImage.transform, font, 28,
                TextAlignmentOptions.Left, new Color(0.35f, 0.92f, 1f));
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0.55f, 1f),
                new Vector2(24f, -57f), new Vector2(-6f, -13f));
            title.text = "ANCHOR CORE / 锚核指令终端";

            TMP_Text balance = CreateText("Balance", panelImage.transform, font, 20,
                TextAlignmentOptions.Right, new Color(1f, 0.75f, 0.28f));
            SetRect(balance.rectTransform, new Vector2(0.52f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -57f), new Vector2(-68f, -13f));
            balance.text = "指令点 0   /   单次消耗 10";

            Image closeImage = CreateImage("Close", panelImage.transform, new Color(0.45f, 0.09f, 0.14f, 1f));
            SetRect(closeImage.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-56f, -54f), new Vector2(-14f, -12f));
            Button closeButton = closeImage.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;
            TMP_Text closeLabel = CreateText("Label", closeImage.transform, font, 23,
                TextAlignmentOptions.Center, Color.white);
            Stretch(closeLabel.rectTransform, 0f);
            closeLabel.text = "×";

            TMP_InputField input = CreateInputField(panelImage.transform, font);
            SetRect(input.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(24f, -31f), new Vector2(-180f, 31f));

            Image submitImage = CreateImage("Submit Command", panelImage.transform,
                new Color(0.06f, 0.66f, 0.72f, 1f));
            SetRect(submitImage.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-166f, -31f), new Vector2(-24f, 31f));
            Button submitButton = submitImage.gameObject.AddComponent<Button>();
            submitButton.targetGraphic = submitImage;
            TMP_Text submitLabel = CreateText("Label", submitImage.transform, font, 22,
                TextAlignmentOptions.Center, Color.white);
            Stretch(submitLabel.rectTransform, 4f);
            submitLabel.text = "执行 [Enter]";

            TMP_Text status = CreateText("Status", panelImage.transform, font, 19,
                TextAlignmentOptions.Left, new Color(0.78f, 0.9f, 1f));
            SetRect(status.rectTransform, new Vector2(0f, 0f), new Vector2(0.62f, 0f),
                new Vector2(24f, 17f), new Vector2(-8f, 58f));
            status.text = "待输入指令";

            TMP_Text result = CreateText("Result", panelImage.transform, font, 18,
                TextAlignmentOptions.Right, new Color(0.55f, 1f, 0.75f));
            SetRect(result.rectTransform, new Vector2(0.58f, 0f), new Vector2(1f, 0f),
                new Vector2(12f, 17f), new Vector2(-24f, 58f));
            result.text = "";

            Image resultIcon = CreateImage("Result Icon", panelImage.transform,
                new Color(0.3f, 0.9f, 1f, 0.25f));
            SetRect(resultIcon.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-62f, 14f), new Vector2(-24f, 52f));
            resultIcon.preserveAspect = true;

            TMP_Text loading = CreateText("Loading", panelImage.transform, font, 17,
                TextAlignmentOptions.Center, new Color(1f, 0.76f, 0.3f));
            SetRect(loading.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-80f, 15f), new Vector2(80f, 53f));
            loading.text = "解析中…";

            AICommandConsoleController controller = canvasObject.AddComponent<AICommandConsoleController>();
            controller.Configure(panelImage.gameObject, openButton, closeButton, input, submitButton,
                balance, status, result, resultIcon, loading.gameObject);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(canvasObject, UiPrefabPath);
            Object.DestroyImmediate(canvasObject);
            return prefab;
        }

        private static TMP_InputField CreateInputField(Transform parent, TMP_FontAsset font)
        {
            Image background = CreateImage("Command Input", parent, new Color(0.035f, 0.11f, 0.16f, 1f));
            TMP_InputField input = background.gameObject.AddComponent<TMP_InputField>();
            input.targetGraphic = background;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.contentType = TMP_InputField.ContentType.Standard;

            GameObject viewportObject = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            viewportObject.transform.SetParent(background.transform, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            Stretch(viewport, 13f);

            TMP_Text text = CreateText("Text", viewport, font, 22,
                TextAlignmentOptions.Left, Color.white);
            Stretch(text.rectTransform, 0f);
            text.enableWordWrapping = false;

            TMP_Text placeholder = CreateText("Placeholder", viewport, font, 22,
                TextAlignmentOptions.Left, new Color(0.55f, 0.68f, 0.76f, 0.8f));
            Stretch(placeholder.rectTransform, 0f);
            placeholder.text = "例：在右上区域释放轰击 / 修复受损最严重的区域";
            placeholder.fontStyle = FontStyles.Italic;

            input.textViewport = viewport;
            input.textComponent = text;
            input.placeholder = placeholder;
            return input;
        }

        private static TMP_FontAsset CreateOrLoadTmpFont()
        {
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpFontPath);
            if (existing != null) return existing;
            Font source = AssetDatabase.LoadAssetAtPath<Font>(FontSourcePath);
            if (source == null) return TMP_Settings.defaultFontAsset;

            if (Shader.Find("TextMeshPro/Mobile/Distance Field") == null)
            {
                TMP_PackageResourceImporter.ImportResources(true, false, false);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(
                source, 90, 9,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                1024, 1024, AtlasPopulationMode.Dynamic, true);
            asset.name = "AnchorChineseTMP";
            asset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            AssetDatabase.CreateAsset(asset, TmpFontPath);
            if (asset.material != null && !AssetDatabase.Contains(asset.material))
            {
                AssetDatabase.AddObjectToAsset(asset.material, asset);
            }
            if (asset.atlasTexture != null && !AssetDatabase.Contains(asset.atlasTexture))
            {
                AssetDatabase.AddObjectToAsset(asset.atlasTexture, asset);
            }
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static void InstallIntoGameplay(AICommandConfig config, GameObject prefab)
        {
            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(GameplayScenePath);
            bool openedTemporarily = !scene.IsValid() || !scene.isLoaded;

            if (openedTemporarily)
            {
                scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);
            }

            AICommandConsoleController existing = FindComponentInScene<AICommandConsoleController>(scene);
            if (existing == null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.name = "AI Command Console UI";
            }

            GameBootstrap bootstrap = FindComponentInScene<GameBootstrap>(scene);
            if (bootstrap != null)
            {
                SerializedObject serialized = new SerializedObject(bootstrap);
                SerializedProperty property = serialized.FindProperty("aiCommandConfig");
                property.objectReferenceValue = config;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(bootstrap);
            }
            else
            {
                Debug.LogError("Gameplay 场景中没有 GameBootstrap，AI 指令系统未接线。");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            if (openedTemporarily)
            {
                EditorSceneManager.CloseScene(scene, true);
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActiveScene);
                }
            }
        }

        private static T FindComponentInScene<T>(Scene scene) where T : Component
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                T component = roots[i].GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font,
            float size, TextAlignmentOptions alignment, Color color)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.enableAutoSizing = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform rect, float padding)
        {
            SetRect(rect, Vector2.zero, Vector2.one,
                new Vector2(padding, padding), new Vector2(-padding, -padding));
        }
    }
}
