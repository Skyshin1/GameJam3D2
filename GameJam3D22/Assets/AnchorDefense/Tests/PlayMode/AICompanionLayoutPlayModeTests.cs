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
            Vector2Int[] sizes = { new Vector2Int(1920, 1080), new Vector2Int(1920, 1200), new Vector2Int(2560, 1080) };
            foreach (bool english in new[] { false, true })
            {
                GameLocalization.SetLanguage(english ? GameLanguage.English : GameLanguage.SimplifiedChinese);
                foreach (Vector2Int size in sizes)
                {
                    rect.sizeDelta = size; preview.orthographicSize = size.y * 0.5f;
                    var texture = new RenderTexture(size.x, size.y, 24); owned.Add(texture);
                    preview.targetTexture = texture;
                    view.GetType().GetMethod("Show", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(view, new object[] {
                        new CompanionSpeech { Text = english ? "Our planet is counting on us. Those invaders picked the wrong team!" : "我们的星球可不是软柿子。一起把这些外星捣蛋鬼赶回去！", Purpose = CompanionPurpose.Chatter } });
                    Canvas.ForceUpdateCanvases(); yield return null;
                    view.GetType().GetMethod("AdvanceBubble", BindingFlags.NonPublic | BindingFlags.Instance)
                        .Invoke(view, new object[] { Time.unscaledTime + 60f });
                    TMP_Text speech = root.transform.Find("Companion Chat Bubble/Companion Speech").GetComponent<TMP_Text>();
                    speech.ForceMeshUpdate(); Assert.That(speech.isTextOverflowing, Is.False);
                    CaptureCompanionPreview(preview, texture, $"bubble-{(english ? "en" : "zh")}-{size.x}x{size.y}");
                    view.GetType().GetMethod("OnRescue", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(view, new object[] { true });
                    console.ShowCompanionMessage(english ? "The core is in trouble. Want me to clear the invaders?" : "核心快撑不住了，要我帮你把敌人清走吗？", false);
                    Canvas.ForceUpdateCanvases(); yield return null;
                    CaptureCompanionPreview(preview, texture, $"rescue-{(english ? "en" : "zh")}-{size.x}x{size.y}");
                    foreach (TMP_Text text in root.transform.Find("Companion Rescue Choices").GetComponentsInChildren<TMP_Text>())
                    { text.ForceMeshUpdate(); Assert.That(text.isTextOverflowing, Is.False, text.name); }
                    view.GetType().GetMethod("OnRescue", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(view, new object[] { false });
                    preview.targetTexture = null; texture.Release();
                }
            }
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
            show.Invoke(view, new object[] { new CompanionSpeech { Text = message, Purpose = CompanionPurpose.Chatter } });
            Assert.That(text.text, Is.EqualTo(message), "Keep complete text for stable wrapping.");
            Assert.That(text.maxVisibleCharacters, Is.Zero);
            advance.Invoke(view, new object[] { start + 0.5f });
            Assert.That(text.maxVisibleCharacters, Is.EqualTo(5));
            advance.Invoke(view, new object[] { start + 20f });
            Assert.That(text.maxVisibleCharacters, Is.EqualTo(int.MaxValue));
            advance.Invoke(view, new object[] { start + 26f });
            Assert.That(bubble.activeSelf, Is.True, "Reading time starts after the final character.");
            advance.Invoke(view, new object[] { start + 28f });
            Assert.That(bubble.activeSelf, Is.False);
            show.Invoke(view, new object[] { new CompanionSpeech { Text = message, Purpose = CompanionPurpose.Chatter } });
            Assert.That(text.maxVisibleCharacters, Is.Zero, "Replacement starts a fresh reveal.");
            typeof(AICompanionView).GetMethod("OnConsoleOpen", flags).Invoke(view, new object[] { true });
            advance.Invoke(view, new object[] { start + 50f });
            Assert.That(bubble.activeSelf, Is.False, "Cancelled bubbles must not reappear.");
            settings.typewriterEnabled = false;
            show.Invoke(view, new object[] { new CompanionSpeech { Text = message } });
            Assert.That(text.maxVisibleCharacters, Is.EqualTo(int.MaxValue));
            settings.typewriterEnabled = true;
            show.Invoke(view, new object[] { new CompanionSpeech { Text = message, SystemMessage = true } });
            Assert.That(text.maxVisibleCharacters, Is.EqualTo(int.MaxValue), "System errors are immediate.");
        }

        private static void CaptureCompanionPreview(Camera camera, RenderTexture texture, string name)
        {
            RenderTexture previous = RenderTexture.active;
            camera.Render(); RenderTexture.active = texture;
            var pixels = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0); pixels.Apply();
            Color32[] colors = pixels.GetPixels32(); int visible = 0;
            for (int i = 0; i < colors.Length; i += 8)
                if (!colors[i].Equals(colors[0])) visible++;
            Assert.That(visible, Is.GreaterThan(100), "UI preview must contain visible graphics, not just a clear camera.");
            System.IO.File.WriteAllBytes("Temp/companion-" + name + ".png", pixels.EncodeToPNG());
            Object.Destroy(pixels); RenderTexture.active = previous;
        }
    }
}
#endif
