using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AnchorDefense.Tests
{
    public sealed partial class AICommandProtocolPlayModeTests
    {
        private sealed class CompanionMock : IAICompanionProvider
        {
            public bool IsConfigured => true;
            public readonly List<AICompanionRequest> Requests = new List<AICompanionRequest>();
            public Func<AICompanionRequest, Task<AICompanionReply>> Respond;
            public Task<AICompanionReply> RequestAsync(AICompanionRequest request, CancellationToken token)
            {
                Requests.Add(request);
                return Respond != null ? Respond(request) : Task.FromResult(new AICompanionReply
                    { Success = true, Kind = "chat", Text = "稳住，我们一起守好这里。", Summary = "本局守护星球。" });
            }
        }
        private AICompanionService Companion(IAICompanionProvider provider, out CoreHealth health)
        {
            if (upgrades == null) SetUpgrades();
            health = core.gameObject.AddComponent<CoreHealth>();
            health.Initialize(Asset<CoreConfig>());
            service = new AICommandService(config, new MockAICommandProvider(Command(Group("区域", Item("anchor_strike", "C01", "攻击 C01")))),
                wallet, upgrades, grid, core, camera, flow, rings);
            var settings = Asset<AICompanionConfig>(); settings.rescueVisualSeconds = 0.01f;
            var companion = Create("Companion").AddComponent<AICompanionService>();
            companion.Initialize(settings, provider, service, upgrades, health, enemies, turrets, flow, null);
            companion.enabled = false;
            return companion;
        }

        [Test]
        public void CompanionRecommendsOnlyAffordableNodesAndEvaluatesOnePurchase()
        {
            var first = Node("turret.damage.01", "伤害强化", 2);
            var second = Node("turret.health.01", "生命强化", 2);
            SetUpgrades(first, second);
            var mock = new CompanionMock();
            mock.Respond = request => Task.FromResult(new AICompanionReply { Success = true,
                Kind = request.Purpose == CompanionPurpose.Recommendation ? "recommendation" : "chat",
                NodeId = first.Id, Text = "先加强火力，敌人有点多。" });
            var companion = Companion(mock, out _);
            companion.Tick(31f);
            Assert.That(companion.RecommendedNode, Is.SameAs(first));
            Assert.That(upgrades.TryPurchase(second), Is.True);
            companion.Tick(0f);
            Assert.That(mock.Requests.Count, Is.EqualTo(2));
            Assert.That(mock.Requests[1].Purpose, Is.EqualTo(CompanionPurpose.PurchaseFeedback));
            Assert.That(mock.Requests[1].Messages[mock.Requests[1].Messages.Length - 1].content, Does.Contain("Gently tease"));
            upgrades.TryPurchase(first); companion.Tick(0f);
            Assert.That(mock.Requests.Count, Is.EqualTo(2));
        }

        [Test]
        public void CompanionNoCandidatesSkipsRecommendationAndRejectsInventedNode()
        {
            var mock = new CompanionMock();
            var companion = Companion(mock, out _);
            companion.ConversationOpen = true; companion.Tick(31f);
            Assert.That(mock.Requests, Is.Empty);
            companion.ConversationOpen = false;
            var node = Node("test.valid", "有效升级", 1);
            upgrades.Config.Configure(new[] { node });
            mock.Respond = request => Task.FromResult(new AICompanionReply { Success = true, Kind = "recommendation", NodeId = "invented", Text = "不存在的推荐" });
            companion.Tick(31f);
            Assert.That(companion.RecommendedNode, Is.Null);
            Assert.That(wallet.AvailableKills, Is.EqualTo(10));
        }

        [UnityTest]
        public IEnumerator CompanionRescueClearsActorsOnceWithoutRewardsOrHealing()
        {
            var companion = Companion(new CompanionMock(), out var health);
            int released = 0;
            var enemy = Create("Rescue Enemy").AddComponent<EnemyController>();
            enemy.Initialize(Asset<EnemyConfig>(), health, 1f, 1f,
                e => { released++; enemies.Unregister(e); e.gameObject.SetActive(false); }, null, null);
            enemy.Killed += e => wallet.RegisterKill(); enemies.Register(enemy);
            int projectilesReleased = 0;
            var projectile = Create("Hostile Shot").AddComponent<EnemyProjectileController>();
            projectile.Launch(Vector3.one * 20f, Vector3.forward, 2f, 1f, 0.1f, 10f, turrets,
                p => { projectilesReleased++; p.gameObject.SetActive(false); });
            health.ApplyDamage(health.MaxHealth * 0.71f);
            Assert.That(companion.RescuePending, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            float before = health.CurrentHealth;
            health.ApplyDamage(100f);
            Assert.That(health.CurrentHealth, Is.EqualTo(before));
            companion.ResolveRescue(true);
            companion.ResolveRescue(true);
            yield return new WaitForSecondsRealtime(0.08f);
            Assert.That(released, Is.EqualTo(1)); Assert.That(projectilesReleased, Is.EqualTo(1));
            Assert.That(wallet.TotalKills, Is.EqualTo(10)); Assert.That(wallet.AvailableKills, Is.EqualTo(10));
            Assert.That(health.CurrentHealth, Is.EqualTo(before));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            health.ApplyDamage(1f);
            Assert.That(companion.RescuePending, Is.False);
        }

        [Test]
        public void CompanionDeclineReleasesOnlyItsPauseAndWorksAfterNetworkFailure()
        {
            var mock = new CompanionMock { Respond = r => Task.FromResult(AICompanionReply.Failure()) };
            var companion = Companion(mock, out var health);
            health.ApplyDamage(health.MaxHealth * 0.71f);
            object otherWindow = new object(); GamePause.Set(otherWindow, true);
            companion.ResolveRescue(false);
            Assert.That(companion.RescuePending, Is.False);
            Assert.That(Time.timeScale, Is.Zero);
            GamePause.Set(otherWindow, false);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator CompanionPreemptsStaleChatterAndRejectsAmbiguousRescueAnswer()
        {
            var pending = new TaskCompletionSource<AICompanionReply>();
            var mock = new CompanionMock { Respond = r => r.Purpose == CompanionPurpose.Chatter ? pending.Task :
                Task.FromResult(new AICompanionReply { Success = true, Kind = "rescue", Decision = "unclear", Text = "需要我帮忙清场吗？" }) };
            var companion = Companion(mock, out var health);
            var shown = new List<string>(); companion.Speech += s => shown.Add(s.Text);
            companion.Tick(20f);
            health.ApplyDamage(health.MaxHealth * 0.71f);
            pending.SetResult(new AICompanionReply { Success = true, Kind = "chat", Text = "过期闲聊" });
            yield return null;
            companion.Submit("也许吧").GetAwaiter().GetResult();
            Assert.That(companion.RescuePending, Is.True);
            Assert.That(shown, Does.Not.Contain("过期闲聊"));
            companion.ResolveRescue(false);
        }

        [Test]
        public void CompanionChatIsFreeAndHistoryContainsActualPlayerAndAssistantText()
        {
            var mock = new CompanionMock();
            var companion = Companion(mock, out _);
            wallet.TrySpend(10);
            companion.Submit("讲个笑话").GetAwaiter().GetResult();
            companion.Submit("我刚才说了什么？").GetAwaiter().GetResult();
            Assert.That(wallet.AvailableKills, Is.Zero);
            Assert.That(companion.Memory.Archive.Count, Is.EqualTo(4));
            Assert.That(Array.Exists(mock.Requests[1].Messages, m => m.role == "user" && m.content == "讲个笑话"), Is.True);
            Assert.That(Array.Exists(mock.Requests[1].Messages, m => m.role == "assistant"), Is.True);
        }

        [Test]
        public void CompanionJsonEnvelopeKeepsExistingCommandValidation()
        {
            string json = "{\"kind\":\"command\",\"text\":\"\",\"node_id\":null,\"decision\":null,\"command\":" + RegionJson + ",\"summary\":\"\"}";
            Assert.That(AICompanionReply.TryParse(json, out var reply), Is.True);
            Assert.That(reply.Command.commands[0].operations[0].operate.action, Is.EqualTo("anchor_strike"));
            Assert.That(AICompanionReply.TryParse(json.Replace("\"summary\":\"\"", "\"summary\":\"\",\"invented\":1"), out _), Is.False);
        }

        [Test]
        public void CompanionRecommendationExpiresAndFailedPurchaseNeverConsumesFeedback()
        {
            var node = Node("damage", "伤害强化", 2); SetUpgrades(node);
            var mock = new CompanionMock { Respond = r => Task.FromResult(new AICompanionReply
                { Success = true, Kind = "recommendation", NodeId = node.Id, Text = "加强火力，守住这波。" }) };
            var companion = Companion(mock, out _); companion.Tick(30f);
            var inaccessible = Node("locked", "尚未开放", 100);
            Assert.That(upgrades.TryPurchase(inaccessible), Is.False);
            Assert.That(companion.RecommendedNode, Is.SameAs(node));
            companion.ConversationOpen = true; companion.Tick(61f);
            Assert.That(companion.RecommendedNode, Is.Null);
            upgrades.TryPurchase(node); companion.Tick(0f);
            Assert.That(mock.Requests.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator CompanionDiscardsRecommendationThatBecameUnaffordableInFlight()
        {
            var node = Node("damage", "伤害强化", 2); SetUpgrades(node);
            var pending = new TaskCompletionSource<AICompanionReply>();
            var mock = new CompanionMock { Respond = r => pending.Task };
            var companion = Companion(mock, out _); companion.Tick(30f);
            wallet.TrySpend(10);
            pending.SetResult(new AICompanionReply { Success = true, Kind = "recommendation", NodeId = node.Id, Text = "先加强火力吧。" });
            yield return null;
            Assert.That(companion.RecommendedNode, Is.Null);
            Assert.That(companion.Memory.Archive, Is.Empty);
        }

        [Test]
        public void CompanionFollowingRecommendationProducesEncouragement()
        {
            var node = Node("damage", "伤害强化", 2); SetUpgrades(node);
            var mock = new CompanionMock { Respond = r => Task.FromResult(new AICompanionReply
                { Success = true, Kind = r.Purpose == CompanionPurpose.Recommendation ? "recommendation" : "chat", NodeId = node.Id, Text = "这下更有底气了！" }) };
            var companion = Companion(mock, out _); companion.Tick(30f);
            upgrades.TryPurchase(node); companion.Tick(0f);
            Assert.That(mock.Requests[1].Messages[mock.Requests[1].Messages.Length - 1].content, Does.Contain("encouragement"));
        }

        [Test]
        public void CompanionPauseDoesNotAccumulateChatterAndNewRunHasNoHistory()
        {
            var mock = new CompanionMock(); var companion = Companion(mock, out _);
            object window = new object(); GamePause.Set(window, true);
            companion.Tick(100f); Assert.That(mock.Requests, Is.Empty);
            GamePause.Set(window, false); companion.Tick(1f); Assert.That(mock.Requests, Is.Empty);
            companion.Tick(18f); Assert.That(mock.Requests.Count, Is.EqualTo(1));
            Assert.That(new CompanionMemory().Archive, Is.Empty);
            flow.EndGame(); companion.Tick(100f); Assert.That(mock.Requests.Count, Is.EqualTo(1));
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator CompanionGameOverDiscardsLatePlayerResponse()
        {
            var pending = new TaskCompletionSource<AICompanionReply>();
            var companion = Companion(new CompanionMock { Respond = r => pending.Task }, out var health);
            var task = companion.Submit("攻击 C01");
            flow.EndGame();
            pending.SetResult(new AICompanionReply { Success = true, Kind = "command", Command = Command(Group("区域", Item("anchor_strike", "C01", "攻击 C01"))) });
            while (!task.IsCompleted) yield return null;
            Assert.That(wallet.AvailableKills, Is.EqualTo(10));
            Assert.That(companion.IsRunning, Is.False);
            Assert.That(companion.RescuePending, Is.False);
        }

        [UnityTest, Explicit("Paid DeepSeek smoke test: only run with explicit developer authorization.")]
        public IEnumerator CompanionLiveDeepSeekSmoke()
        {
            if (!UnityEditor.SessionState.GetBool("AnchorCompanionLiveAuthorized", false))
                Assert.Ignore("Paid API is disabled in automatic test runs.");
            var liveConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<AICommandConfig>("Assets/AnchorDefense/Configs/AI/AICommandConfig.asset");
            var live = new DeepSeekCompanionProvider(liveConfig);
            Assert.That(live.IsConfigured, Is.True, "Local DeepSeek credentials are not configured.");
            var node = Node("turret.damage.01", "伤害强化", 2); SetUpgrades(node);
            var mock = new CompanionMock();
            mock.Respond = async request =>
            {
                if (request.Purpose == CompanionPurpose.RescueQuestion || request.Purpose == CompanionPurpose.RescueResult)
                    return new AICompanionReply { Success = true, Kind = "chat", Text = "Need a hand?" };
                var reply = await live.RequestAsync(request, CancellationToken.None);
                System.IO.File.AppendAllText("Temp/AnchorCompanionLiveSamples.txt", request.Purpose + ": " + reply.Kind + " / " + reply.Decision + " / " + reply.Text + "\n");
                return reply;
            };
            System.IO.File.WriteAllText("Temp/AnchorCompanionLiveSamples.txt", "Explicit live smoke test; generated speech only, no keys or prompts.\n");
            var companion = Companion(mock, out var health);
            var chat = companion.Submit("你好，讲一个很短的星球笑话。");
            while (!chat.IsCompleted) yield return null;
            Assert.That(companion.Memory.Archive.Count, Is.EqualTo(2), "Chinese chat was not displayed.");
            companion.Tick(30f);
            float deadline = Time.realtimeSinceStartup + 18f;
            while (companion.RecommendedNode == null && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(companion.RecommendedNode, Is.SameAs(node), "Live recommendation must select an affordable real node.");
            GameLocalization.SetLanguage(GameLanguage.English);
            Assert.That(GameLocalization.IsEnglish, Is.True);
            health.ApplyDamage(health.MaxHealth * 0.71f);
            var answer = companion.Submit("No thanks, don't clear the enemies. I can handle it.");
            while (!answer.IsCompleted) yield return null;
            Assert.That(companion.RescuePending, Is.False, "English negative answer must decline, not accept or stall.");
            Assert.That(wallet.AvailableKills, Is.EqualTo(10));
            var englishChat = companion.Submit("Tell me a tiny space joke.");
            while (!englishChat.IsCompleted) yield return null;
            string lastSpeech = companion.Memory.Archive[companion.Memory.Archive.Count - 1].content;
            Assert.That(System.Text.RegularExpressions.Regex.IsMatch(lastSpeech, @"[\u4e00-\u9fff]"), Is.False, "English interface must receive English dialogue.");
        }
#endif
    }
}
