using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace AnchorDefense.Tests
{
    public sealed partial class AICommandProtocolPlayModeTests
    {
        [TestCase("Tell me about the alien invaders", true)]
        [TestCase("Can you tell me about the alien invaders?", true)]
        [TestCase("Please explain how to rotate the rings", true)]
        [TestCase("How do I attack C01?", true)]
        [TestCase("What does the turret upgrade do?", true)]
        [TestCase("给我介绍一下外星入侵者", true)]
        [TestCase("告诉我怎么攻击 C01", true)]
        [TestCase("讲个笑话", true)]
        [TestCase("Can you take over the rings?", false)]
        [TestCase("Attack C01 and heal C02", false)]
        [TestCase("帮我接管所有轨道", false)]
        [TestCase("升级炮塔", false)]
        [TestCase("Tell me about the aliens, then rotate the first ring", false)]
        [TestCase("Tell me a joke and attack C01", false)]
        [TestCase("介绍一下敌人，然后攻击 C01", false)]
        public void ConversationGuardSeparatesInformationFromAction(string input, bool expected)
        {
            Assert.That(CompanionConversationIntent.IsConversationOnly(input), Is.EqualTo(expected));
        }

        [TestCase("Tell me about the alien invaders", GameLanguage.English)]
        [TestCase("介绍一下外星入侵者", GameLanguage.SimplifiedChinese)]
        public void EnemyQuestionUsesChatPromptAndIsFree(string input, GameLanguage language)
        {
            GameLocalization.SetLanguage(language);
            string answer = language == GameLanguage.English ? "They threaten our core. I'll help you defend it." : "它们正逼近核心，我陪你一起守住这里。";
            var mock = new CompanionMock { Respond = request => Task.FromResult(new AICompanionReply
                { Success = true, Kind = "chat", Text = answer }) };
            var companion = Companion(mock, out _);
            wallet.TrySpend(10);
            var spoken = new List<CompanionSpeech>(); companion.Speech += spoken.Add;
            companion.Submit(input).GetAwaiter().GetResult();
            Assert.That(mock.Requests.Count, Is.EqualTo(1));
            string prompt = mock.Requests[0].Messages[0].content;
            Assert.That(prompt, Does.Contain("CURRENT PLAYER TURN IS CONVERSATION ONLY"));
            Assert.That(prompt, Does.Contain("WORLD FACTS"));
            Assert.That(prompt, Does.Not.Contain("BEGIN CONDITIONAL COMMAND SCHEMA"));
            Assert.That(spoken.Count, Is.EqualTo(1));
            Assert.That(spoken[0].Text, Is.EqualTo(answer));
            Assert.That(spoken[0].SystemMessage, Is.False);
            Assert.That(wallet.AvailableKills, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MisclassifiedQuestionRetriesOnceAndNeverExecutes(bool failsAgain)
        {
            int requests = 0;
            var mock = new CompanionMock { Respond = request => Task.FromResult(++requests == 1 || failsAgain
                ? SuccessfulSpinReply() : new AICompanionReply { Success = true, Kind = "chat", Text = "转动星环可以调整炮塔的位置。" }) };
            var companion = Companion(mock, out _);
            var finished = new List<bool>(); companion.CommandExecutionFinished += finished.Add;
            var spoken = new List<CompanionSpeech>(); companion.Speech += spoken.Add;
            companion.Submit("告诉我第一星环一直转是什么意思").GetAwaiter().GetResult();
            Assert.That(requests, Is.EqualTo(2));
            Assert.That(mock.Requests[1].Messages[0].content, Does.Contain("Your previous response was misclassified"));
            Assert.That(finished, Is.Empty, "A question must never enter command execution, even for a valid source span.");
            Assert.That(wallet.AvailableKills, Is.EqualTo(10));
            Assert.That(rings[0].IsCommandRotating, Is.False);
            Assert.That(spoken.Count, Is.EqualTo(1));
            Assert.That(spoken[0].SystemMessage, Is.EqualTo(failsAgain));
            Assert.That(spoken[0].Text, Does.Not.Contain("指令"));
            Assert.That(companion.IsBusy, Is.False);
            companion.Tick(0);
            Assert.That(requests, Is.EqualTo(2), "No execution feedback or endless retries.");
        }

        [UnityEngine.TestTools.UnityTest]
        public System.Collections.IEnumerator ConversationRetryCannotPublishAfterGameEnds()
        {
            int requests = 0;
            var pending = new TaskCompletionSource<AICompanionReply>();
            var mock = new CompanionMock { Respond = request => ++requests == 1
                ? Task.FromResult(SuccessfulSpinReply()) : pending.Task };
            var companion = Companion(mock, out _);
            var spoken = new List<CompanionSpeech>(); companion.Speech += spoken.Add;
            Task submission = companion.Submit("Tell me about the alien invaders");
            typeof(AICompanionService).GetMethod("Stop", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic).Invoke(companion, null);
            pending.SetResult(new AICompanionReply { Success = true, Kind = "chat", Text = "Here is a story." });
            yield return new UnityEngine.WaitUntil(() => submission.IsCompleted);
            submission.GetAwaiter().GetResult();
            Assert.That(spoken, Is.Empty);
            Assert.That(wallet.AvailableKills, Is.EqualTo(10));
        }

        [Test]
        public void GameplayAfterConversationStillUsesPlannerAndChargesOnce()
        {
            int requests = 0;
            var mock = new CompanionMock { Respond = request => Task.FromResult(++requests == 1
                ? new AICompanionReply { Success = true, Kind = "chat", Text = "我们一起守护核心。" } : SuccessfulSpinReply()) };
            var companion = Companion(mock, out _);
            companion.Submit("Tell me about the alien invaders").GetAwaiter().GetResult();
            companion.Submit(SpinFirstRing).GetAwaiter().GetResult();
            Assert.That(mock.Requests[1].Messages[0].content, Does.Contain("BEGIN CONDITIONAL COMMAND SCHEMA"));
            Assert.That(rings[0].IsCommandRotating, Is.True);
            Assert.That(wallet.AvailableKills, Is.Zero);
        }
    }
}
