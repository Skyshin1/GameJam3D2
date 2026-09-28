using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace AnchorDefense.Tests
{
    public sealed partial class AICommandProtocolPlayModeTests
    {
        private void UseVisualMarker()
        {
            var template = Create("Marker Template");
            template.AddComponent<AICommandFieldMarker>();
            template.SetActive(false);
            typeof(AICommandConfig).GetField("<FieldMarkerPrefab>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(config, template);
        }

        [Test]
        public void ActorCueUsesTheMainVisualInsteadOfASmallBaseMesh()
        {
            var actor = Create("Actor With Base");
            var smallBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            smallBase.transform.SetParent(actor.transform, false);
            smallBase.transform.localScale = Vector3.one * 0.1f;
            var mainVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mainVisual.transform.SetParent(actor.transform, false);
            mainVisual.transform.localPosition = Vector3.up;
            mainVisual.transform.localScale = Vector3.one * 1.5f;
            Renderer selected = AICommandActorFeedback.VisualFor(actor.transform);
            Assert.That(selected, Is.SameAs(mainVisual.GetComponent<Renderer>()));
            Assert.That(AICommandActorFeedback.FrontPosition(actor.transform, selected, null).y, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator ScanAndStarburstEndWhileTwinklingStarsRemainAndFadeWithTheField()
        {
            var marker = Create("Scan Marker").AddComponent<AICommandFieldMarker>();
            marker.Initialize(new[] { strike }, 7f, 0f, camera, 2f);
            Assert.That(marker.transform.Find("Command Mist"), Is.Null);
            Assert.That(marker.GetComponentsInChildren<ParticleSystem>(), Is.Empty);
            var scan = marker.transform.Find("Command Starburst").GetComponent<MeshRenderer>();
            var sweep = marker.transform.Find("Command Scan").GetComponent<MeshRenderer>();
            var stars = marker.transform.Find("Command Starlight").GetComponent<MeshRenderer>();
            Assert.That(scan.enabled, Is.True);
            Assert.That(sweep.enabled, Is.True);
            float initialHeight = sweep.transform.localPosition.y;
            yield return new WaitForSeconds(0.35f);
            Assert.That(sweep.enabled, Is.True);
            Assert.That(sweep.transform.localPosition.y, Is.LessThan(initialHeight));
            yield return new WaitForSeconds(0.65f);
            Assert.That(scan.enabled, Is.False);
            Assert.That(sweep.enabled, Is.False);
            Assert.That(stars.enabled, Is.True);
            Assert.That(stars.GetComponent<MeshFilter>().sharedMesh.colors, Has.Some.Matches<Color>(color => color.a > 0f));
            var corners = System.Array.FindAll(marker.GetComponentsInChildren<LineRenderer>(), line => line.transform.parent == marker.transform);
            Assert.That(corners, Has.Length.EqualTo(24));
            foreach (var line in corners)
            {
                Assert.That(float.IsNaN(line.startColor.a), Is.False);
                Assert.That(line.startColor.a, Is.GreaterThan(0f));
            }
            yield return new WaitForSeconds(1.15f);
            Assert.That(marker.GetComponentsInChildren<LineRenderer>()[0].startColor.a, Is.Zero);
            Assert.That(stars.enabled, Is.False);
        }

        [Test]
        public void LegacyCartoonBadgeStaysHiddenAfterSkillActivation()
        {
            var marker = Create("No Cartoon Marker").AddComponent<AICommandFieldMarker>();
            var badge = new GameObject("Legacy Cartoon Badge");
            badge.transform.SetParent(marker.transform, false);
            var icon = badge.AddComponent<SpriteRenderer>();
            typeof(AICommandFieldMarker).GetField("badgeRoot", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(marker, badge.transform);
            typeof(AICommandFieldMarker).GetField("badgeIcon", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(marker, icon);
            marker.Initialize(new[] { strike, repair }, 7f, 0.3f, camera);
            marker.NotifyActivation(repair);
            Assert.That(badge.activeSelf, Is.False);
            Assert.That(icon.enabled, Is.False);
        }

        [UnityTest]
        public IEnumerator SlowCueIsReusedAndFadesWhenTheEnemyLeavesTheField()
        {
            UseVisualMarker();
            var target = EnemyAt(Vector3.zero);
            var result = Submit(Command(Group("区域", Item("slow_field", "C01", "减速 C01"))), "减速 C01");
            Assert.That(result.Succeeded, Is.True, result.Message);
            yield return new WaitForSeconds(0.15f);
            var field = Object.FindObjectOfType<AICommandField>();
            var cues = field.GetComponentsInChildren<AICommandActorFeedback>();
            Assert.That(cues, Has.Length.EqualTo(1));
            Assert.That(cues[0].Style, Is.EqualTo(3));
            var firstCue = cues[0];
            yield return new WaitForSeconds(0.2f);
            Assert.That(field.GetComponentsInChildren<AICommandActorFeedback>()[0], Is.SameAs(firstCue));
            target.transform.position = Vector3.one * 100f;
            yield return new WaitForSeconds(0.35f);
            Assert.That(field.GetComponentsInChildren<AICommandActorFeedback>(), Is.Empty);
            Assert.That(field, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator HealingCueOnlyAppearsWhenHealthActuallyIncreasesAndCleansUpWithTheField()
        {
            UseVisualMarker();
            var turret = Create("Healing Target").AddComponent<TurretHealth>();
            turret.Initialize(new TurretRuntimeStats(Asset<TurretConfig>()));
            turrets.Register(turret);
            var result = Submit(Command(Group("区域", Item("repair_pulse", "C01", "治疗 C01"))), "治疗 C01");
            Assert.That(result.Succeeded, Is.True, result.Message);
            yield return new WaitForSeconds(0.1f);
            var field = Object.FindObjectOfType<AICommandField>();
            Assert.That(field.GetComponentsInChildren<AICommandActorFeedback>(), Is.Empty);
            turret.TakeDamage(new DamageInfo(10f, turret.transform.position, null));
            float before = turret.CurrentHealth;
            yield return new WaitForSeconds(0.15f);
            Assert.That(turret.CurrentHealth, Is.GreaterThan(before));
            Assert.That(field.GetComponentsInChildren<AICommandActorFeedback>(), Has.Length.EqualTo(1));
            Assert.That(field.GetComponentsInChildren<AICommandActorFeedback>()[0].Style, Is.EqualTo(2));
            service.Dispose();
            yield return null;
            Assert.That(Object.FindObjectsOfType<AICommandActorFeedback>(), Is.Empty);
        }

        [UnityTest]
        public IEnumerator HitFlashSurvivesADeadEnemyButExpiresPromptly()
        {
            UseVisualMarker();
            var target = EnemyAt(Vector3.zero);
            var result = Submit(Command(Group("区域", Item("anchor_strike", "C01", "攻击 C01"))), "攻击 C01");
            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(target.IsAlive, Is.False);
            Assert.That(Object.FindObjectsOfType<AICommandActorFeedback>(), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectOfType<AICommandActorFeedback>().Style, Is.EqualTo(1));
            yield return new WaitForSeconds(0.4f);
            Assert.That(Object.FindObjectsOfType<AICommandActorFeedback>(), Is.Empty);
        }

        [UnityTest]
        public IEnumerator ReusingAPooledEnemyInvalidatesItsPreviousStatusCue()
        {
            var target = EnemyAt(Vector3.zero);
            var cue = Create("Pooling Cue").AddComponent<AICommandActorFeedback>();
            cue.Initialize(target.transform, target, camera, 3, Color.cyan, target.transform.position, 1f, 1f, null);
            target.Initialize(Asset<EnemyConfig>(), null, 1f, 0f, null, null, null);
            yield return null;
            yield return null;
            Assert.That(cue == null, Is.True);
        }

        [UnityTest]
        public IEnumerator MarkerWaitsForTheActualSecondRegionActivationAcrossAnInterleavedRing()
        {
            UseVisualMarker();
            var result = Submit(Command(Group("区域", Item("anchor_strike", "C01", "攻击 C01")),
                Group("星环", Item("rotate_ring", "01", "操作第一星环", "adaptive")),
                Group("区域", Item("repair_pulse", "C01", "治疗 C01"))), "攻击 C01，操作第一星环，治疗 C01");
            Assert.That(result.Succeeded, Is.True, result.Message);
            var marker = Object.FindObjectOfType<AICommandFieldMarker>();
            var index = typeof(AICommandFieldMarker).GetField("shownOperation", BindingFlags.Instance | BindingFlags.NonPublic);
            yield return new WaitForSeconds(0.4f);
            Assert.That(result.Operations[2].Executed, Is.False);
            Assert.That((int)index.GetValue(marker), Is.Zero);
            yield return new WaitForSeconds(0.25f);
            Assert.That(result.Operations[2].Executed, Is.True);
            Assert.That((int)index.GetValue(marker), Is.EqualTo(1));
        }
    }
}
