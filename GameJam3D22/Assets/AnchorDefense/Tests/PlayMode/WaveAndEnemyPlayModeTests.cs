using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AnchorDefense.Tests
{
    public sealed class WaveAndEnemyPlayModeTests
    {
        [Test]
        public void WaveConfigUsesFixedCadenceAndControlledScaling()
        {
            var config = UnityEditor.AssetDatabase.LoadAssetAtPath<EndlessModeConfig>("Assets/AnchorDefense/Configs/EndlessModeConfig.asset");
            Assert.That(config.WavePeriod, Is.EqualTo(30f));
            Assert.That(config.SurgeDuration, Is.EqualTo(10f));
            Assert.That(config.SurgeBatches, Is.EqualTo(5));
            Assert.That(config.GetSurgeCount(1), Is.EqualTo(24));
            Assert.That(config.GetSurgeCount(3), Is.EqualTo(26));
            Assert.That(config.GetWaveHealthMultiplier(1), Is.EqualTo(1f));
            Assert.That(config.GetWaveHealthMultiplier(3), Is.EqualTo(1.16f).Within(0.001f));
            Assert.That(config.GetRegularInterval(1), Is.EqualTo(1.05f * 0.97f).Within(0.001f));
            Assert.That(config.GetRegularInterval(1000), Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(config.EnemyTypes[2].FirstWave, Is.EqualTo(2));
            Assert.That(config.EnemyTypes[3].FirstWave, Is.EqualTo(3));
            EnemyConfig runner = config.EnemyTypes[2].Enemy;
            EnemyConfig collision = config.EnemyTypes[3].Enemy;
            Assert.That(runner.MaxHealth, Is.EqualTo(8f));
            Assert.That(runner.MoveSpeed, Is.EqualTo(4.8f));
            Assert.That(runner.CoreDamage, Is.EqualTo(8f));
            Assert.That(runner.AttackMode, Is.EqualTo(EnemyAttackMode.ContactCore));
            Assert.That(runner.WaveIcon, Is.Not.Null);
            Assert.That(collision.MoveSpeed, Is.EqualTo(1f));
            Assert.That(collision.CoreDamage, Is.EqualTo(10f));
            Assert.That(collision.TurretCollisionOnly, Is.True);
            Assert.That(collision.WaveIcon, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator SurgesArriveAtThirtySecondBoundariesAndNewEnemiesDebut()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var spawner = Object.FindObjectOfType<EndlessEnemySpawner>();
            Assert.That(spawner, Is.Not.Null);
            Assert.That(spawner.CurrentWave, Is.EqualTo(0));
            Assert.That(spawner.IsSurgeActive, Is.False);
            Assert.That(spawner.SecondsUntilNextWave, Is.GreaterThan(28f));
            RectTransform ribbon = GameObject.Find("Cat Transit Beacon")?.GetComponent<RectTransform>();
            Assert.That(ribbon, Is.Not.Null);
            Assert.That(ribbon.anchorMin.x, Is.Zero.Within(0.001f));
            Assert.That(ribbon.anchorMax.x, Is.Zero.Within(0.001f));
            Assert.That(ribbon.sizeDelta.x, Is.EqualTo(320f).Within(0.001f));
            Assert.That(ribbon.anchoredPosition.x, Is.LessThan(24f));
            Assert.That(Object.FindObjectOfType<HudController>().transform.Find("Controls").gameObject.activeSelf, Is.False);
            Assert.That(ribbon.Find("Night Signal/Cat Porthole/Cat Observer").GetComponent<UnityEngine.UI.Image>().sprite, Is.Not.Null);
            var signalTitle = ribbon.Find("Night Signal/Transit Whisper").GetComponent<UnityEngine.UI.Text>();
            Assert.That(signalTitle.text, Does.Match("猫咪|cat"));
            object pauseOwner = new object();
            float frozenTime = spawner.ElapsedTime;
            GamePause.Set(pauseOwner, true);
            yield return new WaitForSecondsRealtime(0.05f);
            Assert.That(spawner.ElapsedTime, Is.EqualTo(frozenTime).Within(0.001f));
            GamePause.Set(pauseOwner, false);
            var elapsed = typeof(EndlessEnemySpawner).GetField("<ElapsedTime>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            var update = typeof(EndlessEnemySpawner).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            var registryField = typeof(EndlessEnemySpawner).GetField("registry", BindingFlags.Instance | BindingFlags.NonPublic);
            var registry = (EnemyRegistry)registryField.GetValue(spawner);
            var settings = UnityEditor.AssetDatabase.LoadAssetAtPath<EndlessModeConfig>("Assets/AnchorDefense/Configs/EndlessModeConfig.asset");
            var introductions = new System.Collections.Generic.List<EnemyConfig>();
            spawner.FirstEnemySpawned += introductions.Add;
            for (int wave = 1; wave <= 3; wave++)
            {
                int beforeSurge = spawner.TotalSpawned;
                elapsed.SetValue(spawner, wave * 30f);
                update.Invoke(spawner, null);
                Assert.That(spawner.CurrentWave, Is.EqualTo(wave));
                Assert.That(spawner.IsSurgeActive, Is.True);
                if (wave == 1)
                {
                    yield return null;
                    Assert.That(signalTitle.text, Does.Match("跃迁|transit"));
                    Assert.That(signalTitle.text, Does.Not.Contain("高压"));
                }
                if (wave >= 2)
                {
                    EnemyConfig expected = spawner.GetEnemyTypeConfig(wave);
                    Assert.That(registry.ActiveEnemies.Any(e => e != null && e.Definition == expected), Is.True);
                }
                elapsed.SetValue(spawner, wave * 30f + 9.8f);
                update.Invoke(spawner, null);
                Assert.That(spawner.TotalSpawned - beforeSurge, Is.EqualTo(settings.GetSurgeCount(wave)));
                elapsed.SetValue(spawner, wave * 30f + 10f);
                update.Invoke(spawner, null);
                Assert.That(spawner.IsSurgeActive, Is.False);
                Assert.That(spawner.SecondsUntilNextWave, Is.EqualTo(20f).Within(0.2f));
                Assert.That(spawner.TotalSpawned - beforeSurge, Is.EqualTo(settings.GetSurgeCount(wave) + 1));
            }
            Assert.That(introductions.Count(type => type == spawner.GetEnemyTypeConfig(2)), Is.EqualTo(1));
            Assert.That(introductions.Count(type => type == spawner.GetEnemyTypeConfig(3)), Is.EqualTo(1));
            SceneManager.LoadScene("Gameplay");
            yield return null;
            Assert.That(Object.FindObjectOfType<EndlessEnemySpawner>().CurrentWave, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator CollisionEnemyIgnoresDamageAndLivingTurretContactPaysOnePoint()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var spawner = Object.FindObjectOfType<EndlessEnemySpawner>();
            var bootstrap = Object.FindObjectOfType<GameBootstrap>();
            var elapsed = typeof(EndlessEnemySpawner).GetField("<ElapsedTime>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            var update = typeof(EndlessEnemySpawner).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            var late = typeof(EndlessEnemySpawner).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            var registry = (EnemyRegistry)typeof(EndlessEnemySpawner).GetField("registry", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(spawner);
            elapsed.SetValue(spawner, 90f);
            update.Invoke(spawner, null);
            EnemyConfig definition = spawner.GetEnemyTypeConfig(3);
            EnemyController enemy = null;
            foreach (EnemyController candidate in registry.ActiveEnemies)
                if (candidate != null && candidate.IsAlive && candidate.Definition == definition) enemy = candidate;
            Assert.That(enemy, Is.Not.Null);
            float health = enemy.CurrentHealth;
            enemy.TakeDamage(new DamageInfo(100000f, enemy.transform.position, spawner.gameObject));
            enemy.SetZoneEffect(0.5f, 100000f);
            typeof(EnemyController).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(enemy, null);
            var damage = UnityEditor.AssetDatabase.LoadAssetAtPath<AreaDamageSkillEffect>("Assets/AnchorDefense/Configs/AI/Effects/AnchorStrikeDamage.asset");
            var context = new ActiveSkillContext(0, null, bootstrap.transform, new[] { enemy }, new TurretHealth[0]);
            damage.Execute(context);
            Assert.That(enemy.CurrentHealth, Is.EqualTo(health));
            Assert.That(enemy.EffectiveMoveSpeed, Is.EqualTo(definition.MoveSpeed * 0.5f).Within(0.01f));
            var push = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyRepulsionSkillEffect>("Assets/AnchorDefense/Configs/AI/Effects/RepulsionWavePush.asset");
            Vector3 beforePush = enemy.transform.position;
            push.Execute(context);
            Assert.That(Vector3.Distance(enemy.transform.position, beforePush), Is.GreaterThan(0.1f));
            TurretHealth turret = bootstrap.TurretRegistry.FindNearestOperational(Vector3.zero);
            Assert.That(turret, Is.Not.Null);
            bool canTarget = (bool)typeof(TurretController).GetMethod("IsTargetValid", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(turret.GetComponent<TurretController>(), new object[] { enemy });
            Assert.That(canTarget, Is.False);
            int before = bootstrap.KillWallet.TotalKills;
            int firstVersion = enemy.SpawnVersion;
            enemy.transform.position = turret.transform.position + Vector3.right * 2f;
            late.Invoke(spawner, null);
            enemy.transform.position = turret.transform.position - Vector3.right * 2f;
            late.Invoke(spawner, null);
            Assert.That(enemy.IsAlive, Is.False);
            Assert.That(bootstrap.KillWallet.TotalKills, Is.EqualTo(before + 1));
            Assert.That(turret.IsAlive, Is.True);

            var pools = (System.Collections.IList)typeof(EndlessEnemySpawner).GetField("enemyTypes", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(spawner);
            var spawn = typeof(EndlessEnemySpawner).GetMethod("SpawnEnemy", BindingFlags.Instance | BindingFlags.NonPublic);
            spawn.Invoke(spawner, new object[] { pools[3], 1f, 1f, false });
            EnemyController reused = null;
            foreach (EnemyController candidate in registry.ActiveEnemies)
                if (candidate != null && candidate.IsAlive && candidate.Definition == definition) reused = candidate;
            Assert.That(reused, Is.SameAs(enemy));
            Assert.That(reused.SpawnVersion, Is.EqualTo(firstVersion + 1));
            Vector3 turretOriginal = turret.transform.position;
            Vector3 isolated = new Vector3(100f, 100f, 100f);
            turret.transform.position = isolated;
            turret.gameObject.SetActive(false);
            reused.transform.position = isolated;
            late.Invoke(spawner, null);
            Assert.That(reused.IsAlive, Is.True);
            turret.gameObject.SetActive(true);
            late.Invoke(spawner, null);
            Assert.That(reused.IsAlive, Is.False);
            Assert.That(bootstrap.KillWallet.TotalKills, Is.EqualTo(before + 2));

            turret.transform.position = turretOriginal;
            late.Invoke(spawner, null);
            spawn.Invoke(spawner, new object[] { pools[3], 1f, 1f, false });
            foreach (EnemyController candidate in registry.ActiveEnemies)
                if (candidate != null && candidate.IsAlive && candidate.Definition == definition) reused = candidate;
            reused.transform.position = isolated;
            late.Invoke(spawner, null);
            turret.transform.position = isolated;
            late.Invoke(spawner, null);
            Assert.That(reused.IsAlive, Is.False);
            Assert.That(bootstrap.KillWallet.TotalKills, Is.EqualTo(before + 3));
            turret.transform.position = turretOriginal;
        }
    }
}
