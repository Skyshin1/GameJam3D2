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
            marker.Initialize(new[] { repair }, 7f, 0f, camera, 2f);
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
                // The boundary must stay readable after its short entrance flash.
                Assert.That(line.startColor.a, Is.GreaterThan(0.5f));
                Assert.That(line.sharedMaterial, Is.Not.Null);
            }
            yield return new WaitForSeconds(1.15f);
            Assert.That(marker.GetComponentsInChildren<LineRenderer>()[0].startColor.a, Is.Zero);
            Assert.That(stars.enabled, Is.False);
        }

        [Test]
        public void AttackVolumeFormsAtTheRealBoundsThenCollapsesAndDisables()
        {
            var marker = Create("Disintegration Marker").AddComponent<AICommandFieldMarker>();
            marker.Initialize(new[] { strike }, 7f, 0f, camera, 6f, true);
            Assert.That(marker.transform.Find("Command Disintegration"), Is.Null,
                "A prepared command must not play the attack before activation.");
            marker.NotifyActivation(strike);
            var volume = marker.transform.Find("Command Disintegration");
            var shell = volume.Find("Disintegration Shell").GetComponent<MeshRenderer>();
            var coreVisual = volume.Find("Singularity Core").GetComponent<MeshRenderer>();
            var scan = marker.transform.Find("Command Scan").GetComponent<MeshRenderer>();
            var age = typeof(AICommandFieldMarker).GetField("elapsed", BindingFlags.Instance | BindingFlags.NonPublic);
            var present = typeof(AICommandFieldMarker).GetMethod("RefreshPresentation", BindingFlags.Instance | BindingFlags.NonPublic);
            void At(float time) { age.SetValue(marker, time); present.Invoke(marker, null); }

            At(0.12f);
            Assert.That(shell.transform.localScale.x, Is.LessThan(1f));
            Assert.That(coreVisual.enabled, Is.True);
            Assert.That(scan.enabled, Is.False);
            At(1.05f);
            Assert.That(marker.transform.Find("Command Starburst").GetComponent<MeshRenderer>().enabled, Is.False);
            Assert.That(marker.transform.Find("Command Starlight").gameObject.activeSelf, Is.False);
            Assert.That(shell.transform.localScale.x, Is.EqualTo(7f).Within(0.001f));
            Assert.That(Quaternion.Angle(shell.transform.localRotation, Quaternion.identity), Is.LessThan(0.01f));
            Assert.That(scan.enabled, Is.True);
            Assert.That(scan.transform.localPosition.y, Is.InRange(-3.5f, 3.5f));
            At(2f);
            Assert.That(shell.enabled, Is.True);
            Assert.That(coreVisual.enabled, Is.False);
            Assert.That(scan.enabled, Is.False);
            Assert.That(volume.GetComponentsInChildren<MeshRenderer>(), Has.Length.EqualTo(2));
            Assert.That(volume.GetComponentsInChildren<Collider>(), Is.Empty);
            Assert.That(volume.GetComponentsInChildren<Light>(), Is.Empty);
            // A later heal must not restart or erase an already active attack animation.
            marker.NotifyActivation(repair);
            At(5.65f);
            Assert.That(shell.transform.localScale.x, Is.LessThan(4f));
            Assert.That(coreVisual.enabled, Is.True);
            At(6.1f);
            Assert.That(shell.enabled, Is.False);
            Assert.That(coreVisual.enabled, Is.False);
        }

        [UnityTest]
        public IEnumerator AttackAnimationFreezesOnPauseAndReleasesItsMeshesWithTheMarker()
        {
            var marker = Create("Paused Disintegration").AddComponent<AICommandFieldMarker>();
            marker.Initialize(new[] { strike }, 7f, 0f, camera, 6f);
            yield return null;
            var shell = marker.transform.Find("Command Disintegration/Disintegration Shell");
            Mesh mesh = shell.GetComponent<MeshFilter>().sharedMesh;
            Vector3 scale = shell.localScale;
            Quaternion rotation = shell.localRotation;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(0.08f);
            Assert.That(shell.localScale, Is.EqualTo(scale));
            Assert.That(shell.localRotation, Is.EqualTo(rotation));
            Object.Destroy(marker.gameObject);
            yield return null;
            yield return null;
            Assert.That(mesh == null, Is.True);
            Time.timeScale = 1f;
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
