using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AnchorDefense.Tests
{
    public sealed partial class AICommandProtocolPlayModeTests
    {
        private const string SpinFirstRing = "第一星环一直转";
        private AICompanionReply SuccessfulSpinReply() => new AICompanionReply
        {
            Success = true, Kind = "command",
            Command = Command(Group("星环", Item("rotate_ring", "01", SpinFirstRing, "spin")))
        };

        [TestCase("network")]
        [TestCase("invalid_json")]
        [TestCase("long_text")]
        [TestCase("exception")]
        public void FailedOptionalCommentaryCannotOverwriteSuccessfulExecution(string failure)
        {
            var mock = new CompanionMock { Respond = request => request.Purpose == CompanionPurpose.PlayerTurn
                ? Task.FromResult(SuccessfulSpinReply()) : failure == "exception"
                    ? Task.FromException<AICompanionReply>(new InvalidOperationException("Commentary failure"))
                    : Task.FromResult(failure == "long_text"
                        ? new AICompanionReply { Success = true, Kind = "chat", Text = new string('星', 250) }
                        : AICompanionReply.Failure(failure)) };
            var companion = Companion(mock, out _);
            var spoken = new List<CompanionSpeech>(); companion.Speech += spoken.Add;
            var finished = new List<bool>(); companion.CommandExecutionFinished += finished.Add;
            companion.Submit(SpinFirstRing).GetAwaiter().GetResult();
            Assert.That(rings[0].IsCommandRotating, Is.True);
            Assert.That(wallet.AvailableKills, Is.Zero, "Charge only the actual command once.");
            Assert.That(finished, Is.EqualTo(new[] { true }));
            Assert.That(spoken.Count, Is.EqualTo(1));
            string actualResult = spoken[0].Text;
            if (failure == "exception") LogAssert.Expect(LogType.Warning, "Companion request failed: InvalidOperationException");
            companion.Tick(0);
            Assert.That(mock.Requests[1].Purpose, Is.EqualTo(CompanionPurpose.CommandResult));
            Assert.That(spoken.Count, Is.EqualTo(1), "Optional commentary errors must not become player-facing errors.");
            Assert.That(spoken[0].Text, Is.EqualTo(actualResult));
            Assert.That(wallet.AvailableKills, Is.Zero);
            Assert.That(rings[0].IsCommandRotating, Is.True);
            Assert.That(companion.IsBusy, Is.False);
        }

        [Test]
        public void FailedOriginalRequestStillReportsAnErrorWithoutExecuting()
        {
            var mock = new CompanionMock { Respond = request => Task.FromResult(AICompanionReply.Failure("network")) };
            var companion = Companion(mock, out _);
            var spoken = new List<CompanionSpeech>(); companion.Speech += spoken.Add;
            companion.Submit(SpinFirstRing).GetAwaiter().GetResult();
            companion.Tick(0);
            Assert.That(spoken.Count, Is.EqualTo(1));
            Assert.That(spoken[0].SystemMessage, Is.True);
            Assert.That(spoken[0].Text, Does.Contain("连接暂时不可用"));
            Assert.That(mock.Requests.Count, Is.EqualTo(1));
            Assert.That(wallet.AvailableKills, Is.EqualTo(10));
            Assert.That(rings[0].IsCommandRotating, Is.False);
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator SuccessfulCommandClosesInputEvenWhileOptionalCommentaryIsPending()
        {
            var pending = new TaskCompletionSource<AICompanionReply>();
            var mock = new CompanionMock { Respond = request => request.Purpose == CompanionPurpose.PlayerTurn
                ? Task.FromResult(SuccessfulSpinReply()) : pending.Task };
            var companion = Companion(mock, out _);
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AnchorDefense/Prefabs/UI/AICommandConsoleUI.prefab");
            var root = UnityEngine.Object.Instantiate(prefab); owned.Add(root);
            var console = root.GetComponent<AICommandConsoleController>();
            var view = root.GetComponent<AICompanionView>();
            console.Initialize(service, null, flow); console.AttachCompanion(companion); view.Initialize(companion, console, null);
            console.Open();
            companion.Submit(SpinFirstRing).GetAwaiter().GetResult();
            companion.Tick(0);
            Assert.That(companion.IsBusy, Is.False, "Optional narration must not lock input.");
            yield return new WaitForSecondsRealtime(3.3f);
            Assert.That(console.IsOpen, Is.False);
            pending.SetResult(AICompanionReply.Failure("invalid_json"));
            yield return null;
            Assert.That(console.IsOpen, Is.False);
        }
#endif
    }
}
