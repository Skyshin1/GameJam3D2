#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AnchorDefense.Editor
{
    // Bakes the selector into the shared, artist-editable prefab without rebuilding its artwork.
    [InitializeOnLoad]
    public static class AnchorLocalizationPrefabInstaller
    {
        private const string PrefabPath = "Assets/AnchorDefense/Prefabs/UI/SharedSettingsMenuUI.prefab";
        private const string MainMenuPrefabPath = "Assets/AnchorDefense/Prefabs/UI/MainMenuUI.prefab";

        static AnchorLocalizationPrefabInstaller()
        {
            EditorApplication.delayCall += EnsureInstalled;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                    EditorApplication.delayCall += EnsureInstalled;
            };
        }

        [MenuItem("Anchor Defense/Localization/Install Language Selector")]
        public static void EnsureInstalled()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            EnsureTitleOverlay();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null) return;
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform panel = Find(root.transform, "Settings Panel");
                if (panel == null || panel.Find("Language Dropdown") != null) return;
                Transform source = Find(root.transform, "Screen Mode");
                if (source == null) return;
                GameObject copy = Object.Instantiate(source.gameObject, panel);
                copy.name = "Language Dropdown";
                RectTransform rect = copy.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = Vector2.one;
                rect.pivot = Vector2.one;
                rect.anchoredPosition = new Vector2(-92f, -76f);
                rect.sizeDelta = new Vector2(270f, 42f);
                rect.localScale = Vector3.one;
                Dropdown dropdown = copy.GetComponent<Dropdown>();
                if (dropdown != null)
                {
                    dropdown.onValueChanged.RemoveAllListeners();
                    dropdown.ClearOptions();
                    dropdown.AddOptions(new System.Collections.Generic.List<string> { "简体中文", "English" });
                }

                GameObject labelObject = new GameObject("Language Label", typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(panel, false);
                RectTransform labelRect = (RectTransform)labelObject.transform;
                labelRect.anchorMin = labelRect.anchorMax = Vector2.one;
                labelRect.pivot = Vector2.one;
                labelRect.anchoredPosition = new Vector2(-377f, -76f);
                labelRect.sizeDelta = new Vector2(170f, 42f);
                Text label = labelObject.GetComponent<Text>();
                label.text = "语言";
                label.font = dropdown != null && dropdown.captionText != null ? dropdown.captionText.font :
                    Resources.GetBuiltinResource<Font>("Arial.ttf");
                label.fontSize = 24;
                label.alignment = TextAnchor.MiddleRight;
                label.color = Color.white;
                label.raycastTarget = false;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void EnsureTitleOverlay()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(MainMenuPrefabPath) == null) return;
            GameObject root = PrefabUtility.LoadPrefabContents(MainMenuPrefabPath);
            try
            {
                Image titleImage = null;
                foreach (Image image in root.GetComponentsInChildren<Image>(true))
                    if (image.sprite != null && image.sprite.name == "start_screen_0003_毛绒防线")
                    { titleImage = image; break; }
                if (titleImage == null || titleImage.transform.parent.Find("English Plush Defense Title") != null) return;
                GameObject title = new GameObject("English Plush Defense Title", typeof(RectTransform), typeof(Text));
                title.transform.SetParent(titleImage.transform.parent, false);
                RectTransform rect = (RectTransform)title.transform;
                RectTransform source = titleImage.rectTransform;
                rect.anchorMin = source.anchorMin;
                rect.anchorMax = source.anchorMax;
                rect.pivot = source.pivot;
                rect.anchoredPosition = source.anchoredPosition;
                rect.sizeDelta = source.sizeDelta;
                rect.localRotation = source.localRotation;
                rect.localScale = source.localScale;
                Text text = title.GetComponent<Text>();
                text.text = "PLUSH DEFENSE";
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                text.fontStyle = FontStyle.Bold;
                text.fontSize = 48;
                text.alignment = TextAnchor.MiddleCenter;
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 24;
                text.resizeTextMaxSize = 64;
                text.color = new Color(1f, 0.79f, 0.51f);
                text.raycastTarget = false;
                text.enabled = false;
                PrefabUtility.SaveAsPrefabAsset(root, MainMenuPrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                Transform found = Find(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
#endif
