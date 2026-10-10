using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AnchorDefense.Editor
{
    [InitializeOnLoad]
    public static class CatCourierInstaller
    {
        private const string Prefab = "Assets/AnchorDefense/Prefabs/UI/AICommandConsoleUI.prefab";
        private const string Atlas = "Assets/AnchorDefense/Art/UI/CatCourierExpressions.png";
        private static readonly Color Ink = new Color(.24f,.16f,.29f);
        private static readonly Color Cream = new Color(.98f,.91f,.76f);
        static CatCourierInstaller()
        {
            EditorApplication.delayCall += Install;
            EditorApplication.playModeStateChanged += state => { if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Install; };
        }
        [MenuItem("Tools/Anchor Defense/AI/Install Cat Courier UI")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || AssetDatabase.LoadAssetAtPath<GameObject>(Prefab) == null) return;
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.assetPath == Prefab && stage.scene.isDirty) return;
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab).GetComponent<CatCourierLayout>();
            if (existing != null && existing.layoutVersion >= 6) return;
            if (existing != null && existing.layoutVersion >= 2)
            {
                // Incremental repair: preserve all artist overrides on the installed layout.
                var repair = PrefabUtility.LoadPrefabContents(Prefab);
                try
                {
                    foreach (var paper in repair.GetComponentsInChildren<CourierPaperGraphic>(true))
                        if (paper.GetComponent<CanvasRenderer>() == null) paper.gameObject.AddComponent<CanvasRenderer>();
                    foreach (var button in repair.transform.Find("Companion Rescue Choices").GetComponentsInChildren<Button>(true))
                    {
                        var label = button.GetComponentInChildren<TMP_Text>(true);
                        label.fontSizeMax = 20;
                        label.rectTransform.offsetMin = new Vector2(14, 8);
                        label.rectTransform.offsetMax = new Vector2(-14, -8);
                    }
                    UpgradeInputAndStationery(repair);
                    UpgradeExpressionsAndSpeech(repair);
                    repair.GetComponent<CatCourierLayout>().layoutVersion = 6;
                    PrefabUtility.SaveAsPrefabAsset(repair, Prefab);
                }
                finally { PrefabUtility.UnloadPrefabContents(repair); }
                return;
            }
            Sprite[] poses = LoadExpressions();
            if (poses.Length != 6) return;
            GameObject root = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                if (root.GetComponent<AICompanionView>() == null) return;
                var layout = root.GetComponent<CatCourierLayout>() ?? root.AddComponent<CatCourierLayout>();
                layout.layoutVersion = 6;
                if (root.GetComponent<CanvasGroup>() == null) root.AddComponent<CanvasGroup>();
                var panel = (RectTransform)root.transform.Find("Command Panel");
                var bubble = (RectTransform)root.transform.Find("Companion Chat Bubble");
                var opener = (RectTransform)root.transform.Find("Open Command Console");
                var sticker = (RectTransform)bubble.Find("Recommended Upgrade");
                sticker.SetParent(root.transform, false);
                var font = panel.Find("Status").GetComponent<TMP_Text>().font;
                layout.cat = opener; layout.bubble = bubble; layout.inputStrip = panel; layout.sticker = sticker;
                layout.rescueChoices = (RectTransform)root.transform.Find("Companion Rescue Choices");
                foreach (string name in new[] { "Title", "Assistant Portrait", "Assistant Speech Bubble", "Status", "Result", "Result Icon", "Loading" })
                    panel.Find(name)?.gameObject.SetActive(false);
                opener.GetComponent<Image>().sprite = poses[0]; opener.GetComponent<Image>().preserveAspect = true;
                opener.GetComponent<Image>().color = Color.white;
                // Legacy mirroring reads this reference in Awake; keep it synchronized but hidden.
                panel.Find("Assistant Portrait").GetComponent<Image>().sprite = poses[0];
                if (opener.GetComponent<AICommandAssistantMotion>() != null) opener.GetComponent<AICommandAssistantMotion>().enabled = false;
                layout.portrait = opener.GetComponent<CatCourierPortrait>() ?? opener.gameObject.AddComponent<CatCourierPortrait>();
                layout.portrait.expressions = poses;
                var colors = opener.GetComponent<Button>().colors;
                colors.normalColor = Color.white; colors.highlightedColor = new Color(1f,.95f,.86f);
                colors.selectedColor = new Color(1f,.87f,.65f); opener.GetComponent<Button>().colors = colors;
                foreach (var label in opener.GetComponentsInChildren<TMP_Text>(true)) label.gameObject.SetActive(false);
                Paper(panel, Cream); Paper(bubble, Cream); Paper(sticker, new Color(.80f,.87f,.76f));
                if (bubble.GetComponent<CanvasGroup>() == null) bubble.gameObject.AddComponent<CanvasGroup>();
                bubble.Find("Bubble Tail")?.gameObject.SetActive(false);
                var speech = bubble.Find("Companion Speech").GetComponent<TMP_Text>();
                speech.fontSize = 23; speech.color = Ink; speech.richText = false;
                speech.overflowMode = TextOverflowModes.Overflow;
                RectTransform input = (RectTransform)panel.Find("Command Input");
                input.anchorMin = new Vector2(0,0); input.anchorMax = new Vector2(1,1);
                input.offsetMin = new Vector2(18,30); input.offsetMax = new Vector2(-92,-13);
                input.GetComponent<Image>().color = new Color(.32f,.23f,.37f);
                foreach (var text in input.GetComponentsInChildren<TMP_Text>(true))
                { text.fontSize = 21; text.color = text.name == "Placeholder" ? new Color(.88f,.81f,.89f) : new Color(1f,.97f,.88f); }
                var submit = (RectTransform)panel.Find("Submit Command");
                At(submit, new Vector2(1,.5f), new Vector2(-49,8), new Vector2(70,50));
                Paper(submit, new Color(.22f,.43f,.40f));
                var close = (RectTransform)panel.Find("Close");
                At(close, new Vector2(1,0), new Vector2(-26,16), new Vector2(22,22));
                close.GetComponent<Image>().color = Color.clear;
                foreach (var text in close.GetComponentsInChildren<TMP_Text>()) { text.color = Ink; text.fontSize = 18; }
                var balance = (RectTransform)panel.Find("Balance");
                balance.anchorMin = new Vector2(0,0); balance.anchorMax = new Vector2(1,0);
                balance.pivot = new Vector2(.5f,0); balance.anchoredPosition = new Vector2(-16,7); balance.sizeDelta = new Vector2(-70,20);
                var balanceText = balance.GetComponent<TMP_Text>(); balanceText.fontSize = 14; balanceText.color = Ink;
                balanceText.alignment = TextAlignmentOptions.Left; balanceText.enableAutoSizing = true; balanceText.fontSizeMin = 11;
                var stickerLabel = sticker.Find("Label").GetComponent<TMP_Text>();
                stickerLabel.fontSize = 18; stickerLabel.color = Ink; stickerLabel.enableAutoSizing = true; stickerLabel.fontSizeMin = 13;
                stickerLabel.rectTransform.offsetMin = new Vector2(56,8); stickerLabel.rectTransform.offsetMax = new Vector2(-12,-8);
                At((RectTransform)sticker.Find("Upgrade Icon"), new Vector2(0,.5f), new Vector2(29,0), new Vector2(38,38));
                sticker.gameObject.SetActive(false);
                layout.rescueChoices.GetComponent<Image>().color = Color.clear;
                foreach (Button button in layout.rescueChoices.GetComponentsInChildren<Button>(true))
                {
                    Paper((RectTransform)button.transform, button.name.Contains("Accept") ? new Color(.70f,.84f,.73f) : new Color(.84f,.76f,.86f));
                    var rt = (RectTransform)button.transform;
                    bool accept = button.name.Contains("Accept");
                    rt.anchorMin = new Vector2(accept ? 0f : .5f,0); rt.anchorMax = new Vector2(accept ? .5f : 1f,1);
                    rt.offsetMin = new Vector2(3,3); rt.offsetMax = new Vector2(-3,-3);
                    var text = button.GetComponentInChildren<TMP_Text>(); text.fontSize = 20; text.color = Ink; text.enableAutoSizing = true; text.fontSizeMin = 14; text.fontSizeMax = 20;
                    text.rectTransform.offsetMin = new Vector2(14,8); text.rectTransform.offsetMax = new Vector2(-14,-8);
                }
                root.transform.Find("Companion Rescue Backdrop").GetComponent<Image>().color = new Color(.1f,.06f,.14f,.13f);
                layout.alertText = Label("Courier Expression Mark", opener, font, 28);
                layout.alertText.color = new Color(.96f,.66f,.29f);
                At(layout.alertText.rectTransform,new Vector2(.85f,.95f),Vector2.zero,new Vector2(28,34));
                layout.historyButton = (RectTransform)NewButton("Courier Log Button", root.transform, font, "信笺").transform;
                layout.historyPanel = NewRect("Courier Log", root.transform); Paper(layout.historyPanel, Cream);
                var viewport = NewRect("Viewport", layout.historyPanel); Stretch(viewport, 18,18,18,18); viewport.gameObject.AddComponent<RectMask2D>();
                var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
                layout.historyText = Label("Conversation History", viewport, font, 20); layout.historyText.richText = false;
                layout.historyText.alignment = TextAlignmentOptions.TopLeft;
                RectTransform content = layout.historyText.rectTransform;
                content.anchorMin = new Vector2(0,1); content.anchorMax = new Vector2(1,1); content.pivot = new Vector2(.5f,1); content.sizeDelta = Vector2.zero;
                content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                layout.historyText.raycastTarget = true;
                scroll.viewport = viewport; scroll.content = content;
                layout.historyPanel.gameObject.SetActive(false);
                opener.SetAsLastSibling(); layout.alertText.raycastTarget = false;
                UpgradeInputAndStationery(root);
                UpgradeExpressionsAndSpeech(root);
                layout.ApplyLayout(); panel.gameObject.SetActive(false); bubble.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        private static void UpgradeExpressionsAndSpeech(GameObject root)
        {
            const string helplessPath = "Assets/AnchorDefense/Art/UI/CatCourierHelpless.png";
            var importer = AssetImporter.GetAtPath(helplessPath) as TextureImporter;
            if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single))
            {
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
                importer.maxTextureSize = 1024; importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            var layout = root.GetComponent<CatCourierLayout>();
            layout.portrait.helplessExpression = AssetDatabase.LoadAssetAtPath<Sprite>(helplessPath);
            var speech = layout.bubble.GetComponentsInChildren<TMP_Text>(true).First(t => t.name == "Companion Speech");
            CatCourierSpeechBubble.Configure(layout.bubble, speech);
        }
        private static void UpgradeInputAndStationery(GameObject root)
        {
            var panel = (RectTransform)root.transform.Find("Command Panel");
            var field = panel.GetComponentInChildren<TMP_InputField>(true);
            // The compact strip is only 51 units tall. The old 26-unit vertical
            // inset plus 24pt font left no complete line for TMP's Ellipsis mode.
            field.pointSize = 21;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.richText = false;
            Stretch(field.textViewport, 14, 4, 14, 4);
            foreach (var text in new[] { field.textComponent, field.placeholder as TMP_Text })
            {
                if (text == null) continue;
                Stretch(text.rectTransform, 0, 0, 0, 0);
                text.alignment = TextAlignmentOptions.MidlineLeft;
                text.enableAutoSizing = false;
                text.fontSize = 21;
                text.enableWordWrapping = false;
                text.overflowMode = TextOverflowModes.Overflow;
                text.margin = Vector4.zero;
                text.richText = false;
                text.maxVisibleCharacters = int.MaxValue;
                text.color = text == field.textComponent ? new Color(1,.97f,.88f) : new Color(.88f,.81f,.89f);
            }
            field.customCaretColor = true; field.caretColor = new Color(1,.89f,.58f);
            field.selectionColor = new Color(.65f,.48f,.77f,.55f);
            Paper((RectTransform)field.transform, new Color(.26f,.19f,.32f));
            field.targetGraphic = field.GetComponentInChildren<CourierPaperGraphic>(true);
            foreach (var parent in new[] { panel, (RectTransform)root.transform.Find("Companion Chat Bubble") })
            {
                var paper = parent.Find("Courier Paper").GetComponent<CourierPaperGraphic>();
                paper.stationeryDetails = true;
                paper.pawStamp = parent != panel;
            }
            var balance = panel.Find("Balance").GetComponent<TMP_Text>();
            balance.fontSizeMax = 14;
            balance.rectTransform.anchoredPosition = new Vector2(-16,10);
        }
        private static Sprite[] LoadExpressions()
        {
            var importer = AssetImporter.GetAtPath(Atlas) as TextureImporter;
            if (importer == null) { AssetDatabase.ImportAsset(Atlas); importer = AssetImporter.GetAtPath(Atlas) as TextureImporter; }
            if (importer == null) return new Sprite[0];
            if (importer.spriteImportMode != SpriteImportMode.Multiple)
            {
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.GetSourceTextureWidthAndHeight(out int w,out int h);
                importer.spritesheet = Enumerable.Range(0,6).Select(i => new SpriteMetaData {
                    name = "Courier_" + i, rect = new Rect(i % 3 * w / 3f, (1 - i / 3) * h / 2f, w / 3f, h / 2f),
                    pivot = new Vector2(.5f,.5f), alignment = (int)SpriteAlignment.Center }).ToArray();
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAllAssetsAtPath(Atlas).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        }
        private static void Paper(RectTransform parent, Color tint)
        {
            var image = parent.GetComponent<Image>(); if (image != null) image.color = Color.clear;
            foreach (var effect in parent.GetComponents<BaseMeshEffect>()) effect.enabled = false;
            RectTransform paper = parent.Find("Courier Paper") as RectTransform ?? NewRect("Courier Paper", parent);
            Stretch(paper,0,0,0,0); paper.SetAsFirstSibling();
            var shape = paper.GetComponent<CourierPaperGraphic>() ?? paper.gameObject.AddComponent<CourierPaperGraphic>();
            shape.color = tint; shape.ink = Ink; shape.raycastTarget = false;
            var button = parent.GetComponent<Button>();
            if (button != null) button.targetGraphic = shape;
        }
        private static RectTransform NewRect(string name, Transform parent)
        { var go = new GameObject(name,typeof(RectTransform)); go.layer = parent.gameObject.layer; go.transform.SetParent(parent,false); return (RectTransform)go.transform; }
        private static TMP_Text Label(string name,Transform parent,TMP_FontAsset font,float size)
        { var rect = NewRect(name,parent); var t = rect.gameObject.AddComponent<TextMeshProUGUI>(); t.font=font;t.fontSize=size;t.color=Ink;t.raycastTarget=false;t.alignment=TextAlignmentOptions.Center;return t; }
        private static Button NewButton(string name,Transform parent,TMP_FontAsset font,string text)
        { var r=NewRect(name,parent);r.gameObject.AddComponent<Image>();var b=r.gameObject.AddComponent<Button>();Paper(r,Cream);var t=Label("Label",r,font,15);t.text=text;Stretch(t.rectTransform,0,0,0,0);return b; }
        private static void At(RectTransform r,Vector2 a,Vector2 p,Vector2 s)
        { r.anchorMin=r.anchorMax=a;r.pivot=Vector2.one*.5f;r.anchoredPosition=p;r.sizeDelta=s; }
        private static void Stretch(RectTransform r,float left,float bottom,float right,float top)
        { r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(left,bottom);r.offsetMax=new Vector2(-right,-top); }
    }
}
