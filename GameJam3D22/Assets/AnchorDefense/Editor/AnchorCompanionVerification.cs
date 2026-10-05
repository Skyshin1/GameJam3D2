using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnchorDefense.Editor
{
    // Explicit opt-in only: a menu action or a local Temp request runs the offline tests.
    [InitializeOnLoad]
    public static class AnchorCompanionVerification
    {
        private const string Request = "Temp/AnchorCompanionTests.request";
        private const string Status = "Temp/AnchorCompanionTests.status";
        private const string LiveRequest = "Temp/AnchorCompanionLive.request";
        private static TestRunnerApi runner;
        static AnchorCompanionVerification()
        {
            EditorApplication.update += Poll;
            EditorApplication.delayCall += Register;
        }
        private static void Register()
        {
            if (runner != null) return;
            runner = ScriptableObject.CreateInstance<TestRunnerApi>();
            runner.RegisterCallbacks(new Results());
        }
        private static void Poll()
        {
            if (File.Exists("Temp/AnchorCompanionTests.stop") && SessionState.GetBool("AnchorCompanionTests", false))
            {
                File.Delete("Temp/AnchorCompanionTests.stop");
                EditorApplication.isPlaying = false;
                File.WriteAllText(Status, "Stopped an interrupted verification run; scene will be restored by Test Runner.");
                return;
            }
            if ((!File.Exists(Request) && !File.Exists(LiveRequest)) || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode) return;
            // A file request can arrive while auto-refresh is off. Never test stale assemblies.
            if (!SessionState.GetBool("AnchorCompanionVerificationRefreshed", false))
            {
                SessionState.SetBool("AnchorCompanionVerificationRefreshed", true);
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                return;
            }
            SessionState.SetBool("AnchorCompanionVerificationRefreshed", false);
            bool live = File.Exists(LiveRequest) && !File.Exists(Request);
            File.Delete(live ? LiveRequest : Request);
            RunTests(live);
        }
        [MenuItem("Tools/Anchor Defense/AI/Run Offline Companion Tests")]
        public static void Run() => RunTests(false);
        private static void RunTests(bool live)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                { File.WriteAllText(Status, "Blocked: save the open scene before running tests."); return; }
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.scene.isDirty)
            { File.WriteAllText(Status, "Blocked: save the open prefab before running tests."); return; }
            AnchorCompanionInstaller.Install();
            CatCourierInstaller.Install();
            Register();
            SessionState.SetBool("AnchorCompanionTests", true);
            SessionState.SetBool("AnchorCompanionLiveAuthorized", live);
            File.WriteAllText(Status, live ? "Running explicitly authorized live smoke test." : "Running offline PlayMode tests.");
            Filter filter = new Filter { testMode = TestMode.PlayMode };
            if (live) filter.testNames = new[] { "AnchorDefense.Tests.AICommandProtocolPlayModeTests.CompanionLiveDeepSeekSmoke" };
            else filter.groupNames = new[] { "^AnchorDefense.Tests.AICommandProtocolPlayModeTests\\.(?!CompanionLive)" };
            runner.Execute(new ExecutionSettings(filter));
        }
        private sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor test) { }
            public void TestStarted(ITestAdaptor test)
            {
                if (SessionState.GetBool("AnchorCompanionTests", false))
                    File.WriteAllText(Status, "Running: " + test.FullName);
            }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                if (!SessionState.GetBool("AnchorCompanionTests", false)) return;
                SessionState.SetBool("AnchorCompanionTests", false);
                SessionState.SetBool("AnchorCompanionLiveAuthorized", false);
                File.WriteAllText("Temp/AnchorCompanionTests.xml", result.ToXml().OuterXml);
                File.WriteAllText(Status, $"{result.ResultState}: passed={result.PassCount}, failed={result.FailCount}, skipped={result.SkipCount}\n{result.Message}");
            }
        }
    }
}
