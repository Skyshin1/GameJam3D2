#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnchorDefense.Tests
{
    public sealed partial class AICommandProtocolPlayModeTests
    {
        [TestCase(true, false)]
        [TestCase(false, false)]
        [TestCase(true, true)]
        [TestCase(false, true)]
        public void PurchaseEmotionUsesActualChoiceEvenWithWrongModelEmotionOrOffline(bool followed, bool offline)
        {
            var first = Node("damage", "伤害强化", 2);
            var second = Node("health", "生命强化", 2);
            SetUpgrades(first, second);
            var mock = new CompanionMock { Respond = request => Task.FromResult(
                request.Purpose == CompanionPurpose.Recommendation
                    ? new AICompanionReply { Success = true, Kind = "recommendation", NodeId = first.Id, Text = "加强火力，守住这波。" }
                    : offline ? AICompanionReply.Failure() : new AICompanionReply
                        { Success = true, Kind = "chat", Text = "我陪着你。", Emotion = followed ? CompanionEmotion.Helpless : CompanionEmotion.Happy }) };
            var companion = Companion(mock, out _);
            CompanionSpeech last = null; companion.Speech += speech => last = speech;
            companion.Tick(30f);
            Assert.That(upgrades.TryPurchase(followed ? first : second), Is.True);
            companion.Tick(0);
            Assert.That(last.Purpose, Is.EqualTo(CompanionPurpose.PurchaseFeedback));
            Assert.That(last.Emotion, Is.EqualTo(followed ? CompanionEmotion.Happy : CompanionEmotion.Helpless));
            Assert.That(last.Text, Is.Not.Empty);
        }

        [Test]
        public void EmotionMetadataIsOptionalAndDoesNotRelaxCommandEnvelope()
        {
            const string old = "{\"kind\":\"chat\",\"text\":\"小心！\",\"node_id\":null,\"decision\":null,\"command\":null,\"summary\":\"\"}";
            Assert.That(AICompanionReply.TryParse(old, out var reply), Is.True);
            Assert.That(reply.Emotion, Is.Null);
            string prefix = old.Substring(0, old.Length - 1) + ",\"emotion\":";
            Assert.That(AICompanionReply.TryParse(prefix + "\"worried\"}", out reply), Is.True);
            Assert.That(reply.Emotion, Is.EqualTo(CompanionEmotion.Worried));
            Assert.That(AICompanionReply.TryParse(prefix + "\"invented\"}", out reply), Is.True);
            Assert.That(CompanionEmotions.Resolve(new CompanionSpeech { Text = reply.Text, Emotion = reply.Emotion }), Is.EqualTo(CompanionEmotion.Worried));
            Assert.That(AICompanionReply.TryParse(prefix + "42}", out _), Is.False);
            Assert.That(AICompanionReply.TryParse(prefix + "\"happy\",\"execute\":true}", out _), Is.False);
            Assert.That(CompanionEmotions.Resolve(new CompanionSpeech { Text = "小心！", Emotion = CompanionEmotion.Happy }), Is.EqualTo(CompanionEmotion.Happy));
        }

        [TestCase(CompanionEmotion.Happy)]
        [TestCase(CompanionEmotion.Helpless)]
        [TestCase(CompanionEmotion.Worried)]
        [TestCase(CompanionEmotion.Thinking)]
        [TestCase(CompanionEmotion.Curious)]
        [TestCase(CompanionEmotion.Neutral)]
        public void ChatPublishesReplyEmotionRatherThanPlayerInputEmotion(CompanionEmotion emotion)
        {
            var mock = new CompanionMock { Respond = request => Task.FromResult(new AICompanionReply
                { Success = true, Kind = "chat", Text = "我陪着你。", Emotion = emotion }) };
            var companion = Companion(mock, out _);
            CompanionSpeech shown = null; companion.Speech += speech => shown = speech;
            companion.Submit("我很担心").GetAwaiter().GetResult();
            Assert.That(shown.Emotion, Is.EqualTo(emotion));
            Assert.That(CompanionEmotions.Resolve(shown), Is.EqualTo(emotion));
        }

        [UnityTest]
        public IEnumerator PortraitHoldsEmotionThroughReadingAndTransitionsSettle()
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AnchorDefense/Prefabs/UI/AICommandConsoleUI.prefab");
            var root = Object.Instantiate(prefab); owned.Add(root);
            var portrait = root.GetComponent<CatCourierLayout>().portrait;
            portrait.PresentEmotion(CompanionEmotion.Happy);
            yield return new WaitForSecondsRealtime(.08f);
            Assert.That(portrait.transform.localScale, Is.Not.EqualTo(Vector3.one));
            yield return new WaitForSecondsRealtime(1.8f);
            Assert.That(portrait.SpeechEmotion, Is.EqualTo(CompanionEmotion.Happy));
            Assert.That(portrait.GetComponent<Image>().sprite, Is.SameAs(portrait.expressions[4]));
            Assert.That(Vector3.Distance(portrait.transform.localScale, Vector3.one), Is.LessThan(.001f));
            portrait.PresentEmotion(CompanionEmotion.Helpless);
            Assert.That(portrait.GetComponent<Image>().sprite, Is.SameAs(portrait.helplessExpression));
            portrait.reducedMotion = true;
            portrait.PresentEmotion(CompanionEmotion.Worried);
            yield return null;
            Assert.That(portrait.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(portrait.GetComponent<Image>().sprite, Is.SameAs(portrait.expressions[5]));
            portrait.ClearSpeechEmotion();
            Assert.That(portrait.SpeechEmotion, Is.Null);
        }

        [Test]
        public void LongReplyKeepsCompactSizeAndScrollsToTheFinalLine()
        {
            var companion = Companion(new CompanionMock(), out _);
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AnchorDefense/Prefabs/UI/AICommandConsoleUI.prefab");
            var root = Object.Instantiate(prefab); owned.Add(root);
            var console = root.GetComponent<AICommandConsoleController>();
            var view = root.GetComponent<AICompanionView>();
            var layout = root.GetComponent<CatCourierLayout>();
            console.Initialize(service, null, flow); console.AttachCompanion(companion); view.Initialize(companion, console, null);
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            root.GetComponent<CanvasScaler>().enabled = false;
            ((RectTransform)root.transform).sizeDelta = new Vector2(960, 540);
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            var show = typeof(AICompanionView).GetMethod("Show", flags);
            var advance = typeof(AICompanionView).GetMethod("AdvanceBubble", flags);
            string message = "星星已经替你点亮了前方。无论这一波多么拥挤，我都会陪着你，慢慢找出守护星球的办法。";
            show.Invoke(view, new object[] { new CompanionSpeech { Text = message, Emotion = CompanionEmotion.Worried } });
            Canvas.ForceUpdateCanvases();
            Assert.That(layout.bubble.rect.height, Is.EqualTo(102));
            Assert.That(layout.bubble.rect.width, Is.EqualTo(layout.AvailableBubbleWidth));
            var speech = layout.bubble.GetComponentInChildren<TMP_Text>();
            advance.Invoke(view, new object[] { Time.unscaledTime + 100 });
            speech.ForceMeshUpdate();
            Assert.That(speech.textInfo.pageCount, Is.EqualTo(1));
            Assert.That(speech.maxVisibleCharacters, Is.EqualTo(int.MaxValue));
            Assert.That(layout.bubble.GetComponentInChildren<ScrollRect>().enabled, Is.True);
            string longMessage = string.Concat(System.Linq.Enumerable.Repeat(message, 15));
            show.Invoke(view, new object[] { new CompanionSpeech { Text = longMessage } });
            Canvas.ForceUpdateCanvases();
            var scroll = layout.bubble.GetComponentInChildren<ScrollRect>();
            Assert.That(scroll.enabled, Is.True);
            Assert.That(layout.bubble.rect.height, Is.LessThanOrEqualTo(layout.AvailableBubbleHeight));
            Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(1f).Within(.01f));
            var events = UnityEngine.EventSystems.EventSystem.current ?? Create("Scroll Events").AddComponent<UnityEngine.EventSystems.EventSystem>();
            scroll.OnScroll(new UnityEngine.EventSystems.PointerEventData(events) { scrollDelta = new Vector2(0, -1) });
            Assert.That(scroll.content.anchoredPosition.y, Is.GreaterThan(0), "Mouse wheel scrolls the current reply.");
            advance.Invoke(view, new object[] { Time.unscaledTime + 1000 });
            speech.ForceMeshUpdate();
            Assert.That(speech.text, Is.EqualTo(longMessage));
            Assert.That(speech.textInfo.pageCount, Is.EqualTo(1));
            scroll.verticalNormalizedPosition = 0;
            Assert.That(scroll.content.anchoredPosition.y, Is.GreaterThan(0));
            show.Invoke(view, new object[] { new CompanionSpeech { Text = "好耶！", Emotion = CompanionEmotion.Happy } });
            Assert.That(scroll.content.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(scroll.enabled, Is.False);
            Assert.That(layout.bubble.rect.height, Is.EqualTo(102));
            Assert.That(scroll.verticalScrollbar.gameObject.activeSelf, Is.False);
            companion.Memory.Add("assistant", longMessage);
            layout.historyButton.GetComponent<Button>().onClick.Invoke();
            layout.ApplyLayout(); Canvas.ForceUpdateCanvases();
            Assert.That(layout.historyPanel.gameObject.activeSelf, Is.True);
            Assert.That(layout.historyPanel.rect.height, Is.EqualTo(102));
            Assert.That(layout.historyPanel.rect.width, Is.EqualTo(layout.bubble.rect.width));
            Assert.That(layout.historyPanel.GetComponentInChildren<ScrollRect>().content.rect.height,
                Is.GreaterThan(layout.historyPanel.rect.height));
            Assert.That(layout.cat.gameObject.activeInHierarchy, Is.True);
            Assert.That(layout.cat.localScale.x, Is.GreaterThan(.9f));
        }

        [UnityTest]
        public IEnumerator PortraitCanBeResetBeforeAwakeWithoutBecomingInvisible()
        {
            var obj = Create("Not Yet Awoken Portrait"); obj.SetActive(false);
            obj.AddComponent<RectTransform>(); obj.AddComponent<Image>();
            obj.transform.localScale = new Vector3(.9f, .9f, 1);
            var portrait = obj.AddComponent<CatCourierPortrait>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            // Scene bootstrap initializes the view at order -1000, before the child's Awake.
            typeof(CatCourierPortrait).GetMethod("OnDisable", flags).Invoke(portrait, null);
            portrait.ClearSpeechEmotion();
            Assert.That(obj.transform.localScale, Is.EqualTo(new Vector3(.9f, .9f, 1)));
            obj.SetActive(true); yield return null;
            Assert.That(obj.transform.localScale.x, Is.GreaterThan(.8f));
            portrait.PresentEmotion(CompanionEmotion.Helpless);
            obj.SetActive(false); obj.SetActive(true); yield return null;
            Assert.That(obj.transform.localScale.x, Is.GreaterThan(.8f));
        }

        [Test]
        public void PortraitRecoversAnAlreadyCollapsedInstance()
        {
            var obj = Create("Collapsed Portrait"); obj.SetActive(false);
            obj.AddComponent<RectTransform>(); obj.AddComponent<Image>();
            obj.transform.localScale = Vector3.zero;
            var portrait = obj.AddComponent<CatCourierPortrait>();
            portrait.ClearSpeechEmotion();
            Assert.That(obj.transform.localScale, Is.EqualTo(Vector3.one));
        }
    }
}
#endif
