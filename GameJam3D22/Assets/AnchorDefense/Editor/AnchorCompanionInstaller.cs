using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AnchorDefense.Editor
{
    [InitializeOnLoad]
    public static class AnchorCompanionInstaller
    {
        private const string PrefabPath = "Assets/AnchorDefense/Prefabs/UI/AICommandConsoleUI.prefab";
        private const string ConfigPath = "Assets/AnchorDefense/Configs/AI/AICompanionConfig.asset";
        static AnchorCompanionInstaller()
        {
            EditorApplication.delayCall += Install;
            EditorApplication.playModeStateChanged += state =>
            { if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Install; };
        }
        [MenuItem("Tools/Anchor Defense/AI/Install Companion Conversation")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null) return;
            AICompanionConfig config = AssetDatabase.LoadAssetAtPath<AICompanionConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<AICompanionConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                TMP_FontAsset fallback = EnsureFallbackFont();
                if (root.GetComponent<AICompanionView>() != null)
                {
                    bool changed = false;
                    AICompanionView existingView = root.GetComponent<AICompanionView>();
                    if (existingView.GlyphFallback == null && fallback != null)
                    { existingView.SetGlyphFallback(fallback); changed = true; }
                    foreach (Transform child in root.transform)
                        if (child.name.StartsWith("Companion ")) changed |= MatchLayer(child, root.layer);
                    if (changed) PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                    return;
                }
                TMP_Text sampleText = root.GetComponentInChildren<TMP_Text>(true);
                TMP_FontAsset font = sampleText != null ? sampleText.font : TMP_Settings.defaultFontAsset;
                Image samplePaper = root.transform.Find("Command Panel")?.GetComponent<Image>();
                Color paper = new Color(0.98f, 0.91f, 0.75f, 0.98f);
                Color ink = new Color(0.20f, 0.14f, 0.25f);

                Image modal = Panel("Companion Rescue Backdrop", root.transform, new Color(0.06f, 0.04f, 0.12f, 0.36f));
                Stretch(modal.rectTransform); modal.raycastTarget = true;
                modal.transform.SetAsFirstSibling();
                Image bubble = Panel("Companion Chat Bubble", root.transform, paper, samplePaper);
                SetRect(bubble.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-112f, 28f), new Vector2(420f, 150f));
                bubble.raycastTarget = false;
                var shadow = bubble.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0.12f, 0.07f, 0.18f, 0.35f); shadow.effectDistance = new Vector2(4f, -5f);
                var outline = bubble.gameObject.AddComponent<Outline>(); outline.effectColor = ink; outline.effectDistance = new Vector2(2f, -2f);
                Image tail = Panel("Bubble Tail", bubble.transform, paper);
                SetRect(tail.rectTransform, new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 26f), new Vector2(18f, 18f));
                tail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                tail.raycastTarget = false;
                TMP_Text speech = Label("Companion Speech", bubble.transform, font, 23f, ink);
                Stretch(speech.rectTransform); speech.rectTransform.offsetMin = new Vector2(18f, 64f); speech.rectTransform.offsetMax = new Vector2(-18f, -16f);
                speech.alignment = TextAlignmentOptions.MidlineLeft;
                speech.text = "星核的陪伴对话会显示在这里";
                Button node = Button("Recommended Upgrade", bubble.transform, new Color(0.32f, 0.21f, 0.38f), font, "推荐升级");
                SetRect(node.transform as RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(384f, 44f));
                TMP_Text nodeLabel = node.GetComponentInChildren<TMP_Text>();
                nodeLabel.fontSize = 20f; nodeLabel.rectTransform.offsetMin = new Vector2(52f, 2f);
                Image icon = Panel("Upgrade Icon", node.transform, Color.white);
                icon.preserveAspect = true; icon.raycastTarget = false;
                SetRect(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(32f, 32f));

                Image choices = Panel("Companion Rescue Choices", root.transform, paper, samplePaper);
                SetRect(choices.rectTransform, new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(-323f, 259f), new Vector2(620f, 62f));
                Button accept = Button("Accept Rescue", choices.transform, new Color(0.22f, 0.46f, 0.4f), font, "帮帮我");
                Button decline = Button("Decline Rescue", choices.transform, new Color(0.42f, 0.28f, 0.46f), font, "我能应付");
                SetRect(accept.transform as RectTransform, new Vector2(0.25f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(284f, 44f));
                SetRect(decline.transform as RectTransform, new Vector2(0.75f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(284f, 44f));
                var navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = decline, selectOnLeft = decline };
                accept.navigation = navigation;
                navigation.selectOnLeft = navigation.selectOnRight = accept; decline.navigation = navigation;
                Image flash = Panel("Companion Rescue Flash", root.transform, Color.clear); Stretch(flash.rectTransform); flash.raycastTarget = false;
                AICompanionView view = root.AddComponent<AICompanionView>();
                view.SetGlyphFallback(fallback);
                view.Configure(config, bubble.rectTransform, speech, node, nodeLabel, icon, choices.gameObject, accept, decline, modal, flash);
                bubble.gameObject.SetActive(false); choices.gameObject.SetActive(false); modal.gameObject.SetActive(false); flash.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        private static Image Panel(string name, Transform parent, Color color, Image source = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            Image image = go.GetComponent<Image>(); image.color = color;
            if (source != null) { image.sprite = source.sprite; image.type = source.type; }
            return image;
        }
        private static TMP_FontAsset EnsureFallbackFont()
        {
            const string path = "Assets/AnchorDefense/Art/UI/Fonts/CompanionCJKFallback.asset";
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font != null) return font;
            Font source = AssetDatabase.LoadAssetAtPath<Font>("Assets/AnchorDefense/Art/UI/Fonts/NotoSansCJKsc-Regular.otf");
            if (source == null) return null;
            font = TMP_FontAsset.CreateFontAsset(source, 48, 5, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                1024, 1024, AtlasPopulationMode.Dynamic, true);
            font.name = "CompanionCJKFallback";
            AssetDatabase.CreateAsset(font, path);
            if (font.material != null) AssetDatabase.AddObjectToAsset(font.material, font);
            if (font.atlasTexture != null) AssetDatabase.AddObjectToAsset(font.atlasTexture, font);
            AssetDatabase.SaveAssets();
            return font;
        }
        private static TMP_Text Label(string name, Transform parent, TMP_FontAsset font, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            TMP_Text text = go.GetComponent<TMP_Text>(); text.font = font; text.fontSize = size;
            text.color = color; text.raycastTarget = false; text.richText = false; text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }
        private static Button Button(string name, Transform parent, Color color, TMP_FontAsset font, string caption)
        {
            Image image = Panel(name, parent, color);
            Button button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            ColorBlock colors = button.colors; colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f); colors.selectedColor = new Color(1.4f, 1.4f, 1.1f); button.colors = colors;
            TMP_Text label = Label("Label", image.transform, font, 22f, new Color(1f, 0.96f, 0.84f));
            label.text = caption; label.alignment = TextAlignmentOptions.Center; Stretch(label.rectTransform);
            return button;
        }
        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        { rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size; }
        private static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        private static bool MatchLayer(Transform root, int layer)
        {
            bool changed = root.gameObject.layer != layer; root.gameObject.layer = layer;
            foreach (Transform child in root) changed |= MatchLayer(child, layer);
            return changed;
        }
    }
}
