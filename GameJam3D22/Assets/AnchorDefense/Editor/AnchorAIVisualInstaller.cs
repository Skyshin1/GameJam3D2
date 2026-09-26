using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AnchorDefense.Editor
{
    // Installs once into the existing, artist-editable prefab. Never rebuilds MainMenu or HUD.
    public static class AnchorAIVisualInstaller
    {
        private const string Root = "Assets/AnchorDefense";
        private const string ConsolePath = Root + "/Prefabs/UI/AICommandConsoleUI.prefab";
        private const string PortraitPath = Root + "/Art/UI/AnchorAssistant.png";
        private const string OldMarkerPath = Root + "/Prefabs/VFX/AICommandFieldMarker.prefab";
        private const string CornerMarkerPath = Root + "/Prefabs/VFX/AICommandCornerMarker.prefab";
        private const string CornerMaterialPath = Root + "/Art/Materials/M_AICommandCorners.mat";
        private const string ConfigPath = Root + "/Configs/AI/AICommandConfig.asset";

        [InitializeOnLoadMethod]
        private static void ScheduleInstallation()
        {
            EditorApplication.delayCall += InstallWhenReady;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += InstallWhenReady;
        }

        private static void InstallWhenReady()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Install();
        }

        [MenuItem("Tools/Anchor Defense/AI/Apply Assistant Dialogue UI")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Sprite portrait = LoadPortrait();
            if (portrait == null) return;
            ApplyConsolePrefab(portrait);
            EnsureCornerMarker(portrait);
            AssetDatabase.SaveAssets();
        }

        private static Sprite LoadPortrait()
        {
            TextureImporter importer = AssetImporter.GetAtPath(PortraitPath) as TextureImporter;
            if (importer == null) return null;
            if (importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.maxTextureSize = 1024;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(PortraitPath);
        }

        private static void ApplyConsolePrefab(Sprite portrait)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ConsolePath) == null) return;
            GameObject root = PrefabUtility.LoadPrefabContents(ConsolePath);
            try
            {
                Transform panel = root.transform.Find("Command Panel");
                Transform opener = root.transform.Find("Open Command Console");
                if (panel == null || opener == null || panel.Find("Assistant Portrait") != null) return;

                Color ink = new Color(0.17f, 0.13f, 0.23f, 1f);
                Color gold = new Color(0.72f, 0.39f, 0.12f, 1f);
                Image panelImage = panel.GetComponent<Image>();
                if (panelImage != null && panelImage.sprite == null)
                    panelImage.color = new Color(0.96f, 0.86f, 0.66f, 0.98f);
                Outline panelOutline = panel.GetComponent<Outline>();
                if (panelOutline != null)
                {
                    panelOutline.effectColor = new Color(0.32f, 0.17f, 0.26f, 0.96f);
                    panelOutline.effectDistance = new Vector2(4f, -4f);
                }
                SetRect(panel as RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(-520f, 32f), new Vector2(520f, 386f));

                Image openImage = opener.GetComponent<Image>();
                if (openImage != null && openImage.sprite == null)
                    openImage.color = new Color(0.35f, 0.20f, 0.43f, 0.97f);
                SetRect(opener as RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(-192f, 24f), new Vector2(192f, 86f));
                TMP_Text openerLabel = opener.GetComponentInChildren<TMP_Text>(true);
                if (openerLabel != null) openerLabel.text = "呼叫星核助手  [T]";

                TMP_Text title = Text(panel, "Title");
                TMP_Text balance = Text(panel, "Balance");
                TMP_Text status = Text(panel, "Status");
                TMP_Text result = Text(panel, "Result");
                TMP_Text loading = Text(panel, "Loading");
                if (title != null)
                {
                    title.text = "ANCHOR / 星核助手";
                    title.color = ink;
                    title.fontSize = 29f;
                    SetRect(title.rectTransform, Vector2.zero, Vector2.zero,
                        new Vector2(285f, 297f), new Vector2(670f, 342f));
                }
                if (balance != null)
                {
                    balance.color = gold;
                    SetRect(balance.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                        new Vector2(-393f, 298f), new Vector2(-74f, 338f));
                }

                Image portraitImage = CreateImage("Assistant Portrait", panel, Color.white);
                portraitImage.sprite = portrait;
                portraitImage.preserveAspect = true;
                portraitImage.raycastTarget = false;
                SetRect(portraitImage.rectTransform, Vector2.zero, Vector2.zero,
                    new Vector2(16f, 51f), new Vector2(260f, 295f));
                portraitImage.gameObject.AddComponent<AICommandAssistantMotion>();

                Image bubble = CreateImage("Assistant Speech Bubble", panel,
                    new Color(1f, 0.96f, 0.84f, 0.98f));
                bubble.raycastTarget = false;
                SetRect(bubble.rectTransform, Vector2.zero, Vector2.one,
                    new Vector2(275f, 161f), new Vector2(-24f, -78f));
                Outline bubbleOutline = bubble.gameObject.AddComponent<Outline>();
                bubbleOutline.effectColor = new Color(0.42f, 0.26f, 0.29f, 0.55f);
                bubbleOutline.effectDistance = new Vector2(2f, -2f);
                if (status != null) bubble.transform.SetSiblingIndex(status.transform.GetSiblingIndex());

                if (status != null)
                {
                    status.color = ink;
                    status.fontSize = 20f;
                    status.alignment = TextAlignmentOptions.Left;
                    SetRect(status.rectTransform, Vector2.zero, Vector2.one,
                        new Vector2(298f, 205f), new Vector2(-44f, -84f));
                }
                if (result != null)
                {
                    result.color = gold;
                    result.alignment = TextAlignmentOptions.Left;
                    SetRect(result.rectTransform, Vector2.zero, Vector2.one,
                        new Vector2(298f, 170f), new Vector2(-62f, -149f));
                }
                if (loading != null)
                {
                    loading.color = gold;
                    SetRect(loading.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                        new Vector2(-208f, 15f), new Vector2(-30f, 52f));
                }

                Transform input = panel.Find("Command Input");
                if (input != null)
                {
                    SetRect(input as RectTransform, Vector2.zero, Vector2.one,
                        new Vector2(275f, 68f), new Vector2(-181f, -214f));
                    Image inputImage = input.GetComponent<Image>();
                    if (inputImage != null && inputImage.sprite == null)
                        inputImage.color = new Color(0.24f, 0.20f, 0.29f, 1f);
                    TMP_InputField field = input.GetComponent<TMP_InputField>();
                    if (field != null && field.placeholder is TMP_Text placeholder)
                        placeholder.text = "和星核说：在右上角轰击并减速敌人……";
                }

                Transform submit = panel.Find("Submit Command");
                if (submit != null)
                {
                    SetRect(submit as RectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                        new Vector2(-169f, 68f), new Vector2(-26f, 140f));
                    TMP_Text submitLabel = submit.GetComponentInChildren<TMP_Text>(true);
                    if (submitLabel != null) submitLabel.text = "告诉星核";
                }
                Transform close = panel.Find("Close");
                if (close != null)
                    SetRect(close as RectTransform, Vector2.one, Vector2.one,
                        new Vector2(-58f, -57f), new Vector2(-17f, -16f));
                Transform resultIcon = panel.Find("Result Icon");
                if (resultIcon != null)
                    SetRect(resultIcon as RectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                        new Vector2(-66f, 175f), new Vector2(-29f, 212f));

                PrefabUtility.SaveAsPrefabAsset(root, ConsolePath);
                Debug.Log("锚核助手对话 UI 已安装；肖像、气泡、输入框和按钮均可在预制体中单独替换。", root);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureCornerMarker(Sprite fallbackIcon)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CornerMarkerPath);
            if (prefab == null || prefab.GetComponentsInChildren<LineRenderer>(true).Length != 24)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) return;
                Material material = AssetDatabase.LoadAssetAtPath<Material>(CornerMaterialPath);
                if (material == null)
                {
                    material = new Material(shader) { name = "M_AICommandCorners" };
                    AssetDatabase.CreateAsset(material, CornerMaterialPath);
                }

                bool loadedContents = prefab != null;
                GameObject marker = prefab == null
                    ? new GameObject("AI Command Corner Marker", typeof(AICommandFieldMarker))
                    : PrefabUtility.LoadPrefabContents(CornerMarkerPath);
                for (int i = marker.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = marker.transform.GetChild(i);
                    if (child.name.StartsWith("Corner Segment") || child.name == "Skill Icon Badge")
                        Object.DestroyImmediate(child.gameObject);
                }
                var lines = new LineRenderer[24];
                for (int i = 0; i < lines.Length; i++)
                {
                    GameObject segment = new GameObject($"Corner Segment {i + 1:00}",
                        typeof(LineRenderer));
                    segment.transform.SetParent(marker.transform, false);
                    LineRenderer line = segment.GetComponent<LineRenderer>();
                    line.sharedMaterial = material;
                    line.useWorldSpace = false;
                    line.loop = false;
                    line.alignment = LineAlignment.View;
                    line.widthMultiplier = 0.055f;
                    line.numCornerVertices = 2;
                    line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    line.receiveShadows = false;
                    line.sortingOrder = 80;
                    line.positionCount = 2;
                    lines[i] = line;
                }

                GameObject badge = new GameObject("Skill Icon Badge");
                badge.transform.SetParent(marker.transform, false);
                GameObject iconObject = new GameObject("Skill Icon", typeof(SpriteRenderer));
                iconObject.transform.SetParent(badge.transform, false);
                SpriteRenderer icon = iconObject.GetComponent<SpriteRenderer>();
                icon.sprite = fallbackIcon;
                icon.sortingOrder = 90;
                icon.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                icon.receiveShadows = false;

                CubeZoneConfig zone = AssetDatabase.LoadAssetAtPath<CubeZoneConfig>(
                    Root + "/Configs/Zones/CubeZoneConfig.asset");
                marker.GetComponent<AICommandFieldMarker>().ConfigureEditor(lines,
                    badge.transform, icon, fallbackIcon, material,
                    zone != null ? zone.CubeSize : 10.5f);
                prefab = PrefabUtility.SaveAsPrefabAsset(marker, CornerMarkerPath);
                if (loadedContents)
                    PrefabUtility.UnloadPrefabContents(marker);
                else
                    Object.DestroyImmediate(marker);
            }

            AICommandConfig config = AssetDatabase.LoadAssetAtPath<AICommandConfig>(ConfigPath);
            if (config == null) return;
            string currentPath = AssetDatabase.GetAssetPath(config.FieldMarkerPrefab);
            if (config.FieldMarkerPrefab != null && currentPath != OldMarkerPath) return;
            SerializedObject serialized = new SerializedObject(config);
            SerializedProperty markerProperty = serialized.FindProperty("<FieldMarkerPrefab>k__BackingField");
            if (markerProperty == null) return;
            markerProperty.objectReferenceValue = prefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        private static TMP_Text Text(Transform parent, string name) =>
            parent.Find(name) != null ? parent.Find(name).GetComponent<TMP_Text>() : null;

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            if (rect == null) return;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
        }
    }
}
