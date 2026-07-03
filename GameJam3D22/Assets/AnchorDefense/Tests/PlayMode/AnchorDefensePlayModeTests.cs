using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AnchorDefense.Tests
{
    public sealed class AnchorDefensePlayModeTests
    {
        [UnityTest]
        public IEnumerator ExistingThreeDimensionalVersionBootsAndSpawnsEnemies()
        {
            yield return VerifyPlayableLoop("Gameplay", false);
        }

        [UnityTest]
        public IEnumerator DirectionalSpriteVersionBootsAndSpawnsEnemies()
        {
            yield return VerifyPlayableLoop("Gameplay_DirectionalSprites", true);
        }

        private static IEnumerator VerifyPlayableLoop(string sceneName, bool expectDirectionalSprites)
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
            Assert.That(loadOperation, Is.Not.Null);
            while (!loadOperation.isDone)
            {
                yield return null;
            }
            yield return null;

            GameBootstrap bootstrap = Object.FindObjectOfType<GameBootstrap>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(Object.FindObjectsOfType<OrbitRingController>().Length, Is.EqualTo(3));
            Assert.That(Object.FindObjectsOfType<TurretController>().Length, Is.EqualTo(18));
            Assert.That(Object.FindObjectOfType<CoreHealth>(), Is.Not.Null);
            Assert.That(Object.FindObjectOfType<HudController>(), Is.Not.Null);
            Assert.That(Object.FindObjectOfType<UpgradeTreeController>(true), Is.Not.Null);
            Assert.That(bootstrap.KillWallet, Is.Not.Null);
            Assert.That(bootstrap.TurretStats, Is.Not.Null);
            Assert.That(bootstrap.UpgradeSystem, Is.Not.Null);

            RingInputController inputController = Object.FindObjectOfType<RingInputController>();
            Assert.That(inputController, Is.Not.Null);
            Camera gameplayCamera = Camera.main;
            Vector3 fixedCameraPosition = gameplayCamera.transform.position;
            inputController.SetCameraOrbitMode(CameraOrbitMode.XYPlane);
            inputController.ApplyCameraOrbitDelta(new Vector2(30f, 0f));
            Assert.That(Vector3.Distance(gameplayCamera.transform.position, fixedCameraPosition), Is.GreaterThan(0.01f));
            Vector3 planarCameraPosition = gameplayCamera.transform.position;
            inputController.SetCameraOrbitMode(CameraOrbitMode.FreeOrbit360);
            inputController.ApplyCameraOrbitDelta(new Vector2(20f, 15f));
            Assert.That(Vector3.Distance(gameplayCamera.transform.position, planarCameraPosition), Is.GreaterThan(0.01f));
            inputController.SetCameraOrbitMode(CameraOrbitMode.Disabled);
            Assert.That(Vector3.Distance(gameplayCamera.transform.position, fixedCameraPosition), Is.LessThan(0.001f));

            int directionalVisualCount = Object.FindObjectsOfType<DirectionalSpriteRenderer>().Length;
            if (expectDirectionalSprites)
            {
                Assert.That(directionalVisualCount, Is.GreaterThanOrEqualTo(18));
            }
            else
            {
                Assert.That(directionalVisualCount, Is.EqualTo(0));
            }

            EndlessEnemySpawner spawner = Object.FindObjectOfType<EndlessEnemySpawner>();
            Assert.That(spawner, Is.Not.Null);
            float timeout = Time.realtimeSinceStartup + 5f;
            while (spawner.TotalSpawned == 0 && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }
            Assert.That(spawner.TotalSpawned, Is.GreaterThan(0));

            EnemyController killTarget = null;
            timeout = Time.realtimeSinceStartup + 2f;
            while (killTarget == null && Time.realtimeSinceStartup < timeout)
            {
                EnemyController[] enemies = Object.FindObjectsOfType<EnemyController>();
                for (int i = 0; i < enemies.Length; i++)
                {
                    if (enemies[i].IsAlive)
                    {
                        killTarget = enemies[i];
                        break;
                    }
                }
                if (killTarget == null)
                {
                    yield return null;
                }
            }
            Assert.That(killTarget, Is.Not.Null);
            int killsBeforeDamage = bootstrap.KillWallet.TotalKills;
            killTarget.TakeDamage(new DamageInfo(100000f, killTarget.transform.position, bootstrap.gameObject));
            Assert.That(bootstrap.KillWallet.TotalKills, Is.EqualTo(killsBeforeDamage + 1));

            VerifyUpgradeProgression(bootstrap);
            Assert.That(Object.FindObjectsOfType<TurretController>().Length, Is.EqualTo(24));
        }

        private static void VerifyUpgradeProgression(GameBootstrap bootstrap)
        {
            UpgradeSystem upgrades = bootstrap.UpgradeSystem;
            UpgradeTreeConfig config = upgrades.Config;
            UpgradeNodeDefinition inner = config.FindNode("ring.inner.capacity");
            UpgradeNodeDefinition middle = config.FindNode("ring.middle.capacity");
            UpgradeNodeDefinition outer = config.FindNode("ring.outer.capacity");
            UpgradeNodeDefinition damage = config.FindNode("turret.damage.01");
            UpgradeNodeDefinition interval = config.FindNode("turret.interval.01");
            UpgradeNodeDefinition health = config.FindNode("turret.health.01");

            Assert.That(upgrades.GetState(middle), Is.EqualTo(UpgradeNodeState.Locked));
            int killsBeforeGrant = bootstrap.KillWallet.AvailableKills;
            bootstrap.KillWallet.RegisterKill(100);

            float originalDamage = bootstrap.TurretStats.Damage;
            float originalInterval = bootstrap.TurretStats.FireInterval;
            float originalHealth = bootstrap.TurretStats.MaxHealth;

            Assert.That(upgrades.TryPurchase(inner), Is.True);
            Assert.That(upgrades.TryPurchase(middle), Is.True);
            Assert.That(upgrades.TryPurchase(outer), Is.True);
            Assert.That(upgrades.TryPurchase(damage), Is.True);
            Assert.That(upgrades.TryPurchase(interval), Is.True);
            Assert.That(upgrades.TryPurchase(health), Is.True);

            OrbitRingController[] rings = Object.FindObjectsOfType<OrbitRingController>();
            for (int i = 0; i < rings.Length; i++)
            {
                Assert.That(rings[i].ActiveTurretCount, Is.EqualTo(8));
            }

            Assert.That(bootstrap.TurretStats.Damage, Is.GreaterThan(originalDamage));
            Assert.That(bootstrap.TurretStats.FireInterval, Is.LessThan(originalInterval));
            Assert.That(bootstrap.TurretStats.MaxHealth, Is.GreaterThan(originalHealth));
            Assert.That(bootstrap.KillWallet.AvailableKills, Is.EqualTo(killsBeforeGrant + 4));

            TurretHealth turretHealth = Object.FindObjectOfType<TurretHealth>();
            Assert.That(turretHealth, Is.Not.Null);
            Assert.That(turretHealth.MaxHealth, Is.EqualTo(bootstrap.TurretStats.MaxHealth).Within(0.01f));
        }
    }
}
