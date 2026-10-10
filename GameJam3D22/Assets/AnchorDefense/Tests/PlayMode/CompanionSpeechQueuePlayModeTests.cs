#if UNITY_EDITOR
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace AnchorDefense.Tests
{
    public sealed partial class AICommandProtocolPlayModeTests
    {
        private AICompanionView QueuedCompanion(out AICompanionService companion, out CatCourierLayout layout,
            out UpgradeNodeDefinition node)
        {
            var recommended = Node("damage", "伤害强化", 2); SetUpgrades(recommended); node = recommended;
            var mock = new CompanionMock { Respond = request => Task.FromResult(new AICompanionReply
                { Success = true, Kind = "recommendation", NodeId = recommended.Id, Text = "先加强火力吧。" }) };
            companion = Companion(mock, out _); companion.Tick(30);
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AnchorDefense/Prefabs/UI/AICommandConsoleUI.prefab");
            var root = Object.Instantiate(prefab); owned.Add(root);
            var console = root.GetComponent<AICommandConsoleController>();
            var view = root.GetComponent<AICompanionView>(); layout = root.GetComponent<CatCourierLayout>();
            var settings = Object.Instantiate(view.Config); owned.Add(settings);
            settings.typewriterEnabled = false; settings.bubbleSeconds = 7;
            typeof(AICompanionView).GetField("config", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(view, settings);
            console.Initialize(service, null, flow); console.AttachCompanion(companion);
            view.Initialize(companion, console, null); layout.reducedMotion = true;
            return view;
        }

        private static void CallSpeechView(AICompanionView view, string method, params object[] args) =>
            typeof(AICompanionView).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, args);

        [TestCase(true)]
        [TestCase(false)]
        public void JokeAndRecommendationWaitForEachOtherAndUseTheirOwnPaperColor(bool jokeFirst)
        {
            var view = QueuedCompanion(out var companion, out var layout, out var node);
            var text = layout.bubble.GetComponentInChildren<TMP_Text>(true);
            var paper = layout.bubble.GetComponentInChildren<CourierPaperGraphic>(true);
            Color ordinary = paper.color;
            var joke = new CompanionSpeech { Text = "外星人堵车啦。", Purpose = CompanionPurpose.Chatter, Emotion = CompanionEmotion.Happy };
            var recommendation = new CompanionSpeech { Text = "先加强火力吧。", Purpose = CompanionPurpose.Recommendation,
                Recommendation = node, Emotion = CompanionEmotion.Thinking };
            var first = jokeFirst ? joke : recommendation; var second = jokeFirst ? recommendation : joke;
            CallSpeechView(view, "ReceiveSpeech", first);
            CallSpeechView(view, "ReceiveSpeech", second);
            Assert.That(text.text, Is.EqualTo(first.Text));
            Assert.That(paper.color, Is.EqualTo(jokeFirst ? view.Config.proactiveChatterBubbleColor : ordinary));
            Assert.That(layout.portrait.SpeechEmotion, Is.EqualTo(first.Emotion));
            CallSpeechView(view, "AdvanceBubble", Time.unscaledTime + 6);
            Assert.That(text.text, Is.EqualTo(first.Text), "Allow the full reading interval.");
            CallSpeechView(view, "AdvanceBubble", Time.unscaledTime + 8);
            Assert.That(text.text, Is.EqualTo(second.Text));
            Assert.That(paper.color, Is.EqualTo(jokeFirst ? ordinary : view.Config.proactiveChatterBubbleColor));
            Assert.That(companion.SpeechPresentationBusy, Is.True);
            CallSpeechView(view, "AdvanceBubble", Time.unscaledTime + 9);
            Assert.That(layout.bubble.gameObject.activeSelf, Is.False);
            Assert.That(companion.SpeechPresentationBusy, Is.False);
        }

        [Test]
        public void PlayerReplyInterruptsButRetainsUnfinishedAmbientMessage()
        {
            var view = QueuedCompanion(out _, out var layout, out _);
            var paper = layout.bubble.GetComponentInChildren<CourierPaperGraphic>(true);
            Color ordinary = paper.color;
            CallSpeechView(view, "ReceiveSpeech", new CompanionSpeech { Text = "外星人堵车啦。", Purpose = CompanionPurpose.Chatter });
            CallSpeechView(view, "ReceiveSpeech", new CompanionSpeech { Text = "这是你点的笑话。", Purpose = CompanionPurpose.PlayerTurn });
            var text = layout.bubble.GetComponentInChildren<TMP_Text>(true);
            Assert.That(text.text, Is.EqualTo("这是你点的笑话。"));
            Assert.That(paper.color, Is.EqualTo(ordinary), "Only unsolicited chatter gets the special color.");
            CallSpeechView(view, "AdvanceBubble", Time.unscaledTime + 8);
            Assert.That(text.text, Is.EqualTo("外星人堵车啦。"));
        }

        [Test]
        public void ExpiredRecommendationIsNotRevivedFromTheDisplayQueue()
        {
            var view = QueuedCompanion(out var companion, out var layout, out var node);
            CallSpeechView(view, "ReceiveSpeech", new CompanionSpeech { Text = "外星人堵车啦。", Purpose = CompanionPurpose.Chatter });
            CallSpeechView(view, "ReceiveSpeech", new CompanionSpeech { Text = "先加强火力吧。", Purpose = CompanionPurpose.Recommendation, Recommendation = node });
            Assert.That(upgrades.TryPurchase(node), Is.True);
            CallSpeechView(view, "AdvanceBubble", Time.unscaledTime + 8);
            Assert.That(layout.bubble.gameObject.activeSelf, Is.False);
            Assert.That(companion.SpeechPresentationBusy, Is.False);
        }

        [Test]
        public void HistoryAndRescueHoldQueuedSpeechAndLanguageChangeClearsIt()
        {
            var view = QueuedCompanion(out _, out var layout, out _);
            layout.historyPanel.gameObject.SetActive(true);
            CallSpeechView(view, "ReceiveSpeech", new CompanionSpeech { Text = "外星人堵车啦。", Purpose = CompanionPurpose.Chatter });
            CallSpeechView(view, "AdvanceBubble", Time.unscaledTime + 8);
            Assert.That(layout.bubble.gameObject.activeSelf, Is.False);
            layout.historyPanel.gameObject.SetActive(false);
            CallSpeechView(view, "OnRescue", true);
            CallSpeechView(view, "AdvanceBubble", Time.unscaledTime + 9);
            Assert.That(layout.bubble.gameObject.activeSelf, Is.False);
            CallSpeechView(view, "OnRescue", false);
            CallSpeechView(view, "RefreshLabels");
            CallSpeechView(view, "AdvanceBubble", Time.unscaledTime + 10);
            Assert.That(layout.bubble.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void ReadingLocksAutomaticRequestsUntilTheBubbleIsFinished()
        {
            var node = Node("damage", "伤害强化", 2); SetUpgrades(node);
            var mock = new CompanionMock { Respond = request => Task.FromResult(new AICompanionReply
                { Success = true, Kind = "recommendation", NodeId = node.Id, Text = "先加强火力吧。" }) };
            var companion = Companion(mock, out _);
            companion.SpeechPresentationBusy = true; companion.Tick(31);
            Assert.That(mock.Requests, Is.Empty);
            companion.SpeechPresentationBusy = false; companion.Tick(.1f);
            Assert.That(mock.Requests.Count, Is.EqualTo(1));
            Assert.That(mock.Requests[0].Purpose, Is.EqualTo(CompanionPurpose.Recommendation));
        }

        [Test]
        public void RecommendationDoesNotResetChatterAndOverdueJokeGetsTheNextTurn()
        {
            var node = Node("damage", "伤害强化", 2); SetUpgrades(node);
            var mock = new CompanionMock { Respond = request => Task.FromResult(new AICompanionReply
            {
                Success = true, Kind = request.Purpose == CompanionPurpose.Recommendation ? "recommendation" : "chat",
                NodeId = node.Id, Text = "我陪着你。"
            }) };
            var companion = Companion(mock, out _);
            var timer = typeof(AICompanionService).GetField("nextChatter", BindingFlags.NonPublic | BindingFlags.Instance);
            float dueBeforeRecommendation = (float)timer.GetValue(companion);
            companion.Tick(30);
            Assert.That(mock.Requests.Count, Is.EqualTo(1));
            Assert.That((float)timer.GetValue(companion), Is.EqualTo(dueBeforeRecommendation));
            companion.SpeechPresentationBusy = true;
            companion.Tick(31);
            Assert.That(mock.Requests.Count, Is.EqualTo(1));
            companion.SpeechPresentationBusy = false;
            companion.Tick(.1f);
            Assert.That(mock.Requests[1].Purpose, Is.EqualTo(CompanionPurpose.Chatter),
                "Even if another recommendation is due, the waiting chatter must get a turn.");
        }

        [Test]
        public void ChatterCyclesThroughJokesLoreAndEncouragementWithShortIntervals()
        {
            var mock = new CompanionMock(); var companion = Companion(mock, out _);
            Assert.That(companion.Config.chatterMinimum, Is.EqualTo(4));
            Assert.That(companion.Config.chatterMaximum, Is.EqualTo(7));
            companion.Config.recommendationInterval = 1000;
            string[] intents = { "different from recent jokes", "supplied world lore", "different from recent jokes",
                "situational encouragement", "different from recent jokes", "supplied world lore" };
            for (int i = 0; i < 12; i++) companion.Tick(8);
            Assert.That(mock.Requests.Count, Is.EqualTo(12));
            for (int i = 0; i < mock.Requests.Count; i++)
            {
                var request = mock.Requests[i];
                Assert.That(request.Purpose, Is.EqualTo(CompanionPurpose.Chatter));
                Assert.That(request.Messages[request.Messages.Length - 1].content, Does.Contain(intents[i % intents.Length]));
            }
            var saved = UnityEditor.AssetDatabase.LoadAssetAtPath<AICompanionConfig>("Assets/AnchorDefense/Configs/AI/AICompanionConfig.asset");
            Assert.That(saved.chatterSequence, Is.EqualTo(companion.Config.chatterSequence));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FailedWorldLoreTurnIsRetriedInsteadOfSkipped(bool oversized)
        {
            var mock = new CompanionMock();
            mock.Respond = request => Task.FromResult(mock.Requests.Count == 2
                ? oversized ? new AICompanionReply { Success = true, Kind = "chat", Text = new string('星', 300) }
                    : AICompanionReply.Failure()
                : new AICompanionReply { Success = true, Kind = "chat", Text = "我们的星球由我们一起守护。" });
            var companion = Companion(mock, out _);
            for (int i = 0; i < 4; i++) companion.Tick(8);
            foreach (int index in new[] { 1, 2 })
            {
                var request = mock.Requests[index];
                Assert.That(request.Messages[request.Messages.Length - 1].content, Does.Contain("supplied world lore"));
            }
            var next = mock.Requests[3];
            Assert.That(next.Messages[next.Messages.Length - 1].content, Does.Contain("different from recent jokes"));
        }

        [Test]
        public void MissingChatterSequenceStillIncludesWorldLore()
        {
            var mock = new CompanionMock(); var companion = Companion(mock, out _);
            companion.Config.chatterSequence = new CompanionChatterTopic[0];
            companion.Tick(8); companion.Tick(8);
            var request = mock.Requests[1];
            Assert.That(request.Messages[request.Messages.Length - 1].content, Does.Contain("supplied world lore"));
        }

        [Test]
        public void ParkedMouseCannotKeepTheBubbleAndConversationLockedForever()
        {
            var view = QueuedCompanion(out var companion, out var layout, out _);
            CallSpeechView(view, "ReceiveSpeech", new CompanionSpeech { Text = "外星人堵车啦。", Purpose = CompanionPurpose.Chatter });
            var hideField = typeof(AICompanionView).GetField("hideAt", BindingFlags.Instance | BindingFlags.NonPublic);
            float originalDeadline = (float)hideField.GetValue(view);
            for (int i = 0; i < 60; i++) CallSpeechView(view, "ExtendReadingForHover", 1f, true);
            Assert.That((float)hideField.GetValue(view), Is.EqualTo(originalDeadline + 12).Within(.01f));
            CallSpeechView(view, "AdvanceBubble", originalDeadline + 13);
            Assert.That(layout.bubble.gameObject.activeSelf, Is.False);
            Assert.That(companion.SpeechPresentationBusy, Is.False);
        }

        [Test]
        public void TwentyMinuteRunWithAllUpgradesBoughtStillProducesSlightlyLongerChatter()
        {
            GameLocalization.SetLanguage(GameLanguage.SimplifiedChinese);
            var node = Node("damage", "伤害强化", 1); SetUpgrades(node);
            upgrades.TryPurchase(node);
            string joke = new string('星', 60);
            var mock = new CompanionMock { Respond = request => Task.FromResult(new AICompanionReply
                { Success = true, Kind = "chat", Text = joke, Summary = "守护星球。" }) };
            var companion = Companion(mock, out _);
            int spoken = 0, lateSpoken = 0;
            float simulatedTime = 0, busyUntil = 0;
            companion.Speech += speech =>
            {
                spoken++; if (simulatedTime >= 900) lateSpoken++;
                busyUntil = simulatedTime + 9; // Typewriter plus reading time.
                companion.SpeechPresentationBusy = true;
            };
            for (int i = 0; i < 2400; i++)
            {
                simulatedTime += .5f;
                companion.SpeechPresentationBusy = simulatedTime < busyUntil;
                companion.Tick(.5f);
            }
            Assert.That(spoken, Is.GreaterThan(80));
            Assert.That(lateSpoken, Is.GreaterThan(20), "The final five minutes must not go silent.");
            foreach (var request in mock.Requests)
            {
                Assert.That(request.Purpose, Is.EqualTo(CompanionPurpose.Chatter));
                Assert.That(request.Messages.Length, Is.LessThanOrEqualTo(22));
                Assert.That(request.Messages[0].content, Does.Contain("Chatter request MUST return kind=chat"));
                Assert.That(request.Messages[0].content, Does.Not.Contain("node=damage"));
            }
        }

        [Test]
        public void FailedAndRejectedChatterExposeDifferentDiagnosticReasons()
        {
            var mock = new CompanionMock { Respond = request => Task.FromResult(AICompanionReply.Failure("network=ProtocolError, HTTP=429")) };
            var companion = Companion(mock, out _);
            companion.Tick(8);
            Assert.That(companion.DialogueDiagnostics, Does.Contain("HTTP=429"));
            mock.Respond = request => Task.FromResult(new AICompanionReply { Success = true, Kind = "chat", Text = new string('星', 250) });
            companion.Tick(8);
            Assert.That(companion.DialogueDiagnostics, Does.Contain("回复被拦截"));
            companion.ConversationOpen = true;
            Assert.That(companion.DialogueDiagnostics, Does.Contain("玩家输入框仍然打开"));
            companion.ConversationOpen = false; companion.SpeechPresentationBusy = true;
            companion.SpeechPresentationReason = "等待显示队列（2 条）";
            Assert.That(companion.DialogueDiagnostics, Does.Contain("等待显示队列"));
        }
    }
}
#endif
