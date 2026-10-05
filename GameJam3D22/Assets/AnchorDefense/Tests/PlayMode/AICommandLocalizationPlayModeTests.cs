using NUnit.Framework;
using System.Collections;
using UnityEngine.TestTools;

namespace AnchorDefense.Tests
{
    public sealed partial class AICommandProtocolPlayModeTests
    {
        [Test]
        public void LanguageChoiceIsSavedWithoutResettingOtherSettings()
        {
            float originalBrightness = GameSettingsService.Current.brightness;
            GameLocalization.SetLanguage(GameLanguage.English);
            Assert.That(GameSettingsService.Current.language, Is.EqualTo(GameLanguage.English));
            Assert.That(GameSettingsService.Current.brightness, Is.EqualTo(originalBrightness));
            Assert.That(UnityEngine.PlayerPrefs.GetString("AnchorDefense.GameSettings"), Does.Contain("\"language\":1"));
            GameLocalization.SetLanguage(GameLanguage.SimplifiedChinese);
            Assert.That(GameSettingsService.Current.language, Is.EqualTo(GameLanguage.SimplifiedChinese));
        }

        [Test]
        public void EnglishRequestUsesEnglishPromptAndExecutesExistingZoneSkill()
        {
            GameLocalization.SetLanguage(GameLanguage.English);
            const string source = "attack C01";
            var provider = new MockAICommandProvider(Command(Group("区域", Item("anchor_strike", "C01", source))));
            service = new AICommandService(config, provider, wallet, upgrades, grid, core, camera, flow, rings);
            AICommandExecutionResult result = service.Submit(source).GetAwaiter().GetResult();
            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.Operations[0].ZoneId, Is.Zero);
            Assert.That(wallet.AvailableKills, Is.Zero);
            Assert.That(provider.LastRequest.SystemPrompt, Does.Contain("Zone abilities:").And.Contain("Anchor Strike"));
            Assert.That(GameLocalization.AIResultMessage(result), Does.Not.Contain("星核"));
        }

        [Test]
        public void EnglishInputStillWorksWithChineseInterface()
        {
            GameLocalization.SetLanguage(GameLanguage.SimplifiedChinese);
            const string source = "attack C01";
            var provider = new MockAICommandProvider(Command(Group("区域", Item("anchor_strike", "C01", source))));
            service = new AICommandService(config, provider, wallet, upgrades, grid, core, camera, flow, rings);
            AICommandExecutionResult result = service.Submit(source).GetAwaiter().GetResult();
            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(provider.LastRequest.SystemPrompt, Does.Contain("English keywords:"));
        }

        [Test]
        public void EnglishUpgradeAndRingControlKeepExistingCostsAndSafety()
        {
            GameLocalization.SetLanguage(GameLanguage.English);
            var node = Node("turret.damage.01", "锚定增幅", 2);
            SetUpgrades(node);
            AICommandExecutionResult upgrade = Submit(Command(Group("升级",
                Item("purchase_upgrade", node.Id, "upgrade turret damage"))), "upgrade turret damage");
            Assert.That(upgrade.Succeeded, Is.True, upgrade.Message);
            Assert.That(wallet.AvailableKills, Is.EqualTo(8));
            service.Dispose();
            service = null;

            AICommandExecutionResult denied = Submit(Command(Group("星环",
                Item("rotate_ring", "01", "don't rotate the first ring", "adaptive"))),
                "don't rotate the first ring");
            Assert.That(denied.Succeeded, Is.False);
            Assert.That(wallet.AvailableKills, Is.EqualTo(8));
            Assert.That(rings[0].IsCommandRotating, Is.False);
        }

        [Test]
        public void EnglishSpatialAndSelectionWordsAreRecognizedLocally()
        {
            Assert.That(AICommandValidator.HasRingControlIntent("rotate the first ring continuously"), Is.True);
            Assert.That(AICommandValidator.HasRingControlIntent("attack enemies near the first ring"), Is.False);
            Assert.That(RingRotationSkillEffect.TryReadRepeatCount("turn the first ring 3 times", out int count), Is.True);
            Assert.That(count, Is.EqualTo(3));
            AITargetSelector enemies = AITargetSelector.FromSource("strike the zone with the most enemies");
            Assert.That(enemies?.Key, Is.EqualTo("enemy_count:desc"));
            AITargetSelector turrets = AITargetSelector.FromSource("repair the most damaged turrets");
            Assert.That(turrets?.Key, Is.EqualTo("missing_health:desc"));
        }

        [UnityTest]
        public IEnumerator EnglishContinuousRingCommandKeepsTurningUntilStopped()
        {
            GameLocalization.SetLanguage(GameLanguage.English);
            const string start = "keep rotating the first ring clockwise";
            AICommandExecutionResult started = Submit(Command(Group("星环",
                Item("rotate_ring", "01", start, "adaptive"))), start);
            Assert.That(started.Succeeded, Is.True, started.Message);
            yield return new UnityEngine.WaitForSeconds(0.3f);
            Assert.That(rings[0].IsCommandRotating, Is.True);

            service.Dispose();
            service = null;
            const string stop = "stop the first ring";
            AICommandExecutionResult stopped = Submit(Command(Group("星环",
                Item("rotate_ring", "01", stop, "stop"))), stop);
            Assert.That(stopped.Succeeded, Is.True, stopped.Message);
            yield return new UnityEngine.WaitForSeconds(0.3f);
            Assert.That(rings[0].IsCommandRotating, Is.False);
        }
    }
}
