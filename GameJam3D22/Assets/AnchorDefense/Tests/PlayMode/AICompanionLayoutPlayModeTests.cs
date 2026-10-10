#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnchorDefense.Tests
{
    public sealed partial class AICommandProtocolPlayModeTests
    {
        [UnityTest]
        public IEnumerator CompanionPrefabRendersChineseAndEnglishAcrossAspectRatios()
        {
            var companion = Companion(new CompanionMock(), out _);
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AnchorDefense/Prefabs/UI/AICommandConsoleUI.prefab");
            Assert.That(prefab.GetComponent<AICompanionView>(), Is.Not.Null, "Install the editable companion UI first.");
            var root = Object.Instantiate(prefab); owned.Add(root);
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 5;
            var console = root.GetComponent<AICommandConsoleController>();
            var view = root.GetComponent<AICompanionView>();
            console.Initialize(service, null, flow); console.AttachCompanion(companion);
            view.Initialize(companion, console, null);
            // Offscreen UI render keeps the user's Game view resolution and open scenes untouched.
            view.enabled = false;
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            root.GetComponent<CanvasScaler>().enabled = false;
            RectTransform rect = (RectTransform)root.transform;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.position = Vector3.zero; rect.rotation = Quaternion.identity; rect.localScale = Vector3.one;
            var preview = Create("Companion UI Preview").AddComponent<Camera>();
            preview.transform.position = new Vector3(0f, 0f, -10f);
            preview.orthographic = true; preview.clearFlags = CameraClearFlags.SolidColor;
            preview.backgroundColor = new Color(0.045f, 0.06f, 0.12f); preview.cullingMask = 1 << 5;
            preview.enabled = false; canvas.worldCamera = preview;
            Vector2Int[] sizes = { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(1920, 1200), new Vector2Int(2560, 1080), new Vector2Int(3440, 1440) };
            foreach (bool english in new[] { false, true })
            {
                GameLocalization.SetLanguage(english ? GameLanguage.English : GameLanguage.SimplifiedChinese);
                foreach (Vector2Int size in sizes)
                {
                    rect.sizeDelta = size; preview.orthographicSize = size.y * 0.5f;
                    var texture = new RenderTexture(size.x, size.y, 24); owned.Add(texture);
                    preview.targetTexture = texture;
                    view.GetType().GetMethod("Show", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(view, new object[] {
                        new CompanionSpeech { Text = english ? "Oh well, we can try your plan. Whatever happens among these stars, I will stay beside you and help protect our little planet." : "好耶，听我的建议啦！星星已经替我们点亮前方，一起守住这颗小小的星球，把外星捣蛋鬼赶回去。", Purpose = CompanionPurpose.Chatter, Emotion = english ? CompanionEmotion.Helpless : CompanionEmotion.Happy } });
                    Canvas.ForceUpdateCanvases(); yield return new WaitForSecondsRealtime(.3f);
                    view.GetType().GetMethod("AdvanceBubble", BindingFlags.NonPublic | BindingFlags.Instance)
                        .Invoke(view, new object[] { Time.unscaledTime + 60f });
                    TMP_Text speech = root.transform.Find("Companion Chat Bubble/Speech Viewport/Companion Speech").GetComponent<TMP_Text>();
                    speech.ForceMeshUpdate();
                    Assert.That(speech.overflowMode, Is.EqualTo(TextOverflowModes.Overflow));
                    var courier = root.GetComponent<CatCourierLayout>();
                    Assert.That(courier, Is.Not.Null);
                    Assert.That(courier.portrait.expressions.Length, Is.EqualTo(6));
                    Assert.That(courier.portrait.helplessExpression, Is.Not.Null);
                    Assert.That(speech.textInfo.pageCount, Is.EqualTo(1));
                    foreach (var paper in root.GetComponentsInChildren<CourierPaperGraphic>(true))
                        Assert.That(paper.GetComponent<CanvasRenderer>(), Is.Not.Null, paper.transform.parent.name);
                    Assert.That(courier.bubble.GetComponent<CanvasGroup>().alpha, Is.GreaterThan(.95f));
                    AssertInsideCourierCanvas(rect, courier.cat);
                    AssertInsideCourierCanvas(rect, courier.bubble);
                    CaptureCompanionPreview(preview, texture, $"bubble-{(english ? "en" : "zh")}-{size.x}x{size.y}");
                    view.GetType().GetMethod("OnRescue", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(view, new object[] { true });
                    console.ShowCompanionMessage(english ? "The core is in trouble. Want me to clear the invaders?" : "核心快撑不住了，要我帮你把敌人清走吗？", false, true);
                    console.InputField.text = english ? "Help me protect our planet!" : "小猫，帮我保护星球！";
                    Canvas.ForceUpdateCanvases(); yield return new WaitForSecondsRealtime(.3f);
                    view.GetType().GetMethod("AdvanceBubble", BindingFlags.NonPublic | BindingFlags.Instance)
                        .Invoke(view, new object[] { Time.unscaledTime + 60f });
                    Canvas.ForceUpdateCanvases();
                    AssertInputVisible(console.InputField);
                    AssertInsideCourierCanvas(rect, courier.inputStrip);
                    AssertInsideCourierCanvas(rect, courier.rescueChoices);
                    Assert.That(courier.cat.gameObject.activeSelf, Is.True, "Cat stays visible during rescue.");
                    CaptureCompanionPreview(preview, texture, $"rescue-{(english ? "en" : "zh")}-{size.x}x{size.y}");
                    foreach (TMP_Text text in root.transform.Find("Companion Rescue Choices").GetComponentsInChildren<TMP_Text>())
                    { text.ForceMeshUpdate(); Assert.That(text.isTextOverflowing, Is.False, text.name); }
                    view.GetType().GetMethod("OnRescue", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(view, new object[] { false });
                    preview.targetTexture = null; texture.Release();
                }
            }
        }
        private static void AssertInsideCourierCanvas(RectTransform canvas, RectTransform element)
        {
            var corners = new Vector3[4]; element.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector3 local = canvas.InverseTransformPoint(corner);
                Assert.That(local.x, Is.InRange(canvas.rect.xMin - 1f, canvas.rect.xMax + 1f), element.name);
                Assert.That(local.y, Is.InRange(canvas.rect.yMin - 1f, canvas.rect.yMax + 1f), element.name);
            }
        }
        [UnityTest]
        public IEnumerator CourierRescueShowsCatAndChoicesAndDeclinesWithoutStuckPause()
        {
            var companion = Companion(new CompanionMock(), out var health);
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AnchorDefense/Prefabs/UI/AICommandConsoleUI.prefab");
            var root = Object.Instantiate(prefab); owned.Add(root);
            var console = root.GetComponent<AICommandConsoleController>();
            var view = root.GetComponent<AICompanionView>();
            var courier = root.GetComponent<CatCourierLayout>();
            console.Initialize(service, null, flow); console.AttachCompanion(companion); view.Initialize(companion, console, null);
            health.ApplyDamage(health.MaxHealth * .71f);
            yield return null;
            Assert.That(companion.RescuePending, Is.True);
            Assert.That(courier.cat.gameObject.activeInHierarchy, Is.True);
            Assert.That(courier.portrait.Worried, Is.True);
            Assert.That(courier.rescueChoices.gameObject.activeSelf, Is.True);
            Assert.That(courier.bubble.gameObject.activeSelf, Is.True);
            Assert.That(console.IsOpen, Is.True);
            root.transform.Find("Companion Rescue Choices/Decline Rescue").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.That(companion.RescuePending, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(console.IsOpen, Is.False);
            Assert.That(courier.rescueChoices.gameObject.activeSelf, Is.False);
        }
        [TestCase("一起守护星球，我们能赢！")]
        [TestCase("Together we can protect our planet!")]
        public void CompanionBubbleTypesThenKeepsFullReadingTime(string message)
        {
            var companion = Companion(new CompanionMock(), out _);
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AnchorDefense/Prefabs/UI/AICommandConsoleUI.prefab");
            var root = Object.Instantiate(prefab); owned.Add(root);
            var console = root.GetComponent<AICommandConsoleController>();
            var view = root.GetComponent<AICompanionView>();
            var settings = Object.Instantiate(view.Config); owned.Add(settings);
            settings.typewriterEnabled = true; settings.typewriterCharactersPerSecond = 10f; settings.bubbleSeconds = 7f;
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(AICompanionView).GetField("config", flags).SetValue(view, settings);
            console.Initialize(service, null, flow); console.AttachCompanion(companion);
            view.Initialize(companion, console, null);
            var show = typeof(AICompanionView).GetMethod("Show", flags);
            var advance = typeof(AICompanionView).GetMethod("AdvanceBubble", flags);
            var bubble = root.transform.Find("Companion Chat Bubble").gameObject;
            var text = bubble.GetComponentInChildren<TMP_Text>(true);
            float start = Time.unscaledTime;
            show.Invoke(view, new object[] { new CompanionSpeech { Text = message, Purpose = CompanionPurpose.Chatter, Emotion = CompanionEmotion.Happy } });
            Assert.That(text.text, Is.EqualTo(message), "Keep complete text for stable wrapping.");
            Assert.That(text.maxVisibleCharacters, Is.Zero);
            // Stay away from the exact float boundary (4.999999 may floor to 4).
            advance.Invoke(view, new object[] { start + 0.55f });
            Assert.That(text.maxVisibleCharacters, Is.EqualTo(5));
            advance.Invoke(view, new object[] { start + 20f });
            Assert.That(text.maxVisibleCharacters, Is.EqualTo(int.MaxValue));
            advance.Invoke(view, new object[] { start + 26f });
            Assert.That(bubble.activeSelf, Is.True, "Reading time starts after the final character.");
            Assert.That(root.GetComponent<CatCourierLayout>().portrait.SpeechEmotion, Is.EqualTo(CompanionEmotion.Happy));
            advance.Invoke(view, new object[] { start + 28f });
            advance.Invoke(view, new object[] { start + 28.3f });
            Assert.That(bubble.activeSelf, Is.False);
            Assert.That(root.GetComponent<CatCourierLayout>().portrait.SpeechEmotion, Is.Null);
            show.Invoke(view, new object[] { new CompanionSpeech { Text = message, Purpose = CompanionPurpose.Chatter, Emotion = CompanionEmotion.Happy } });
            Assert.That(text.maxVisibleCharacters, Is.Zero, "Replacement starts a fresh reveal.");
            typeof(AICompanionView).GetMethod("OnConsoleOpen", flags).Invoke(view, new object[] { true });
            // The courier keeps replies separate from the input strip; disabling still cancels them.
            typeof(AICompanionView).GetMethod("OnDisable", flags).Invoke(view, null);
            advance.Invoke(view, new object[] { start + 50f });
            Assert.That(bubble.activeSelf, Is.False, "Cancelled bubbles must not reappear.");
            settings.typewriterEnabled = false;
            show.Invoke(view, new object[] { new CompanionSpeech { Text = message } });
            Assert.That(text.maxVisibleCharacters, Is.EqualTo(int.MaxValue));
            settings.typewriterEnabled = true;
            show.Invoke(view, new object[] { new CompanionSpeech { Text = message, SystemMessage = true } });
            Assert.That(text.maxVisibleCharacters, Is.Zero, "Local cat prompts also use the typewriter.");
            foreach (CompanionPurpose purpose in System.Enum.GetValues(typeof(CompanionPurpose)))
            {
                show.Invoke(view, new object[] { new CompanionSpeech { Text = message, Purpose = purpose } });
                Assert.That(text.maxVisibleCharacters, Is.Zero, purpose.ToString());
                advance.Invoke(view, new object[] { Time.unscaledTime + .55f });
                Assert.That(text.maxVisibleCharacters, Is.EqualTo(5), purpose.ToString());
            }
        }

        private static void AssertInputVisible(TMP_InputField field)
        {
            field.ForceLabelUpdate(); field.textComponent.ForceMeshUpdate();
            Assert.That(field.textViewport.rect.height, Is.GreaterThan(35f), "Input needs a full line of vertical space.");
            Assert.That(field.textComponent.maxVisibleCharacters, Is.GreaterThan(1000), "Typing animation must not hide player input.");
            int visible = 0;
            foreach (var character in field.textComponent.textInfo.characterInfo)
            {
                if (!character.isVisible) continue;
                visible++;
                Vector3 min = field.textViewport.InverseTransformPoint(field.textComponent.transform.TransformPoint(character.bottomLeft));
                Vector3 max = field.textViewport.InverseTransformPoint(field.textComponent.transform.TransformPoint(character.topRight));
                Assert.That(min.y, Is.GreaterThanOrEqualTo(field.textViewport.rect.yMin - 1f));
                Assert.That(max.y, Is.LessThanOrEqualTo(field.textViewport.rect.yMax + 1f));
            }
            Assert.That(visible, Is.GreaterThan(5), "Typed text must generate visible glyphs, not only an ellipsis.");
        }

        [UnityTest]
        public IEnumerator CourierInputRemainsReadableWhileTypingAndAfterReopening()
        {
            var companion = Companion(new CompanionMock(), out _);
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AnchorDefense/Prefabs/UI/AICommandConsoleUI.prefab");
            var root = Object.Instantiate(prefab); owned.Add(root);
            var console = root.GetComponent<AICommandConsoleController>();
            var view = root.GetComponent<AICompanionView>();
            console.Initialize(service, null, flow); console.AttachCompanion(companion); view.Initialize(companion, console, null);
            if (UnityEngine.EventSystems.EventSystem.current == null) Create("UI Events").AddComponent<UnityEngine.EventSystems.EventSystem>();
            foreach (string phrase in new[] { "小猫帮我保护星球", "Help our planet", "<hello>小猫123" })
            {
                console.Open(); console.InputField.text = string.Empty;
                yield return null;
                foreach (char character in phrase)
                    console.InputField.ProcessEvent(new Event { type = EventType.KeyDown, character = character });
                console.InputField.ForceLabelUpdate();
                Canvas.ForceUpdateCanvases(); yield return null;
                Assert.That(console.InputField.text, Is.EqualTo(phrase));
                AssertInputVisible(console.InputField);
                console.Opener.onClick.Invoke();
                console.Open(); yield return null;
                AssertInputVisible(console.InputField);
            }
        }

        private static void CaptureCompanionPreview(Camera camera, RenderTexture texture, string name)
        {
            RenderTexture previous = RenderTexture.active;
            camera.Render(); camera.Render(); camera.Render(); RenderTexture.active = texture;
            var pixels = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0); pixels.Apply();
            Color32[] colors = pixels.GetPixels32(); int visible = 0;
            for (int i = 0; i < colors.Length; i += 8)
                if (!colors[i].Equals(colors[0])) visible++;
            Assert.That(visible, Is.GreaterThan(100), "UI preview must contain visible graphics, not just a clear camera.");
            const string previews = "Library/TestArtifacts/Companion";
            System.IO.Directory.CreateDirectory(previews);
            System.IO.File.WriteAllBytes(previews + "/companion-" + name + ".png", pixels.EncodeToPNG());
            Object.Destroy(pixels); RenderTexture.active = previous;
        }
    }
}
#endif
