using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AnchorDefense.Tests
{
    public sealed class AnchorDefensePlayModeTests
    {
        [Test]
        public void HoverSoundGlobalCooldownSuppressesImmediateRepeatedEntries()
        {
            UiHoverSoundPlayer.ResetHoverSoundCooldown();

            Assert.That(UiHoverSoundPlayer.ShouldPlayHoverSound(10f, 0.15f), Is.True);
            Assert.That(UiHoverSoundPlayer.ShouldPlayHoverSound(10.05f, 0.15f), Is.False);
            Assert.That(UiHoverSoundPlayer.ShouldPlayHoverSound(10.16f, 0.15f), Is.True);
        }

[Test]
        public void TurretFireSoundLimiterCapsShortBurstOverlap()
        {
            TurretController.ResetFireSoundLimiter();

            Assert.That(TurretController.TryGetFireSoundVolumeScale(20f, 0.2f, 3, 0.6f, out float firstScale), Is.True);
            Assert.That(firstScale, Is.EqualTo(1f).Within(0.001f));
            Assert.That(TurretController.TryGetFireSoundVolumeScale(20.01f, 0.2f, 3, 0.6f, out float secondScale), Is.True);
            Assert.That(secondScale, Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(TurretController.TryGetFireSoundVolumeScale(20.02f, 0.2f, 3, 0.6f, out float thirdScale), Is.True);
            Assert.That(thirdScale, Is.EqualTo(0.36f).Within(0.001f));
            Assert.That(TurretController.TryGetFireSoundVolumeScale(20.03f, 0.2f, 3, 0.6f, out _), Is.False);
            Assert.That(TurretController.TryGetFireSoundVolumeScale(20.21f, 0.2f, 3, 0.6f, out float nextScale), Is.True);
            Assert.That(nextScale, Is.EqualTo(1f).Within(0.001f));
        }

[Test]
        public void EnemyAttackSoundLimiterKeepsAttackAudioLight()
        {
            EnemyController.ResetAttackSoundLimiter();

            Assert.That(EnemyController.DefaultAttackSoundVolume, Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(EnemyController.ShouldPlayAttackSound(30f, 0.18f, 2), Is.True);
            Assert.That(EnemyController.ShouldPlayAttackSound(30.01f, 0.18f, 2), Is.True);
            Assert.That(EnemyController.ShouldPlayAttackSound(30.02f, 0.18f, 2), Is.False);
            Assert.That(EnemyController.ShouldPlayAttackSound(30.19f, 0.18f, 2), Is.True);
        }


[Test]
        public void SlowSelfRotatorDefaultsToGentleLocalYaw()
        {
            var rotator = new GameObject("Rotator").AddComponent<SlowSelfRotator>();

            Assert.That(rotator.DegreesPerSecond, Is.EqualTo(6f).Within(0.001f));
            Assert.That(rotator.LocalAxis, Is.EqualTo(Vector3.up));

            Object.DestroyImmediate(rotator.gameObject);
        }



        [UnityTest]
        public IEnumerator MainMenuLoadsThreeDimensionalGameplayThroughLoadingScreen()
        {
            AsyncOperation menuLoad = SceneManager.LoadSceneAsync("MainMenu");
            while (!menuLoad.isDone)
            {
                yield return null;
            }

            Assert.That(Object.FindObjectOfType<MainMenuController>(), Is.Not.Null);
            SettingsMenuController menuSettings = Object.FindObjectOfType<SettingsMenuController>(true);
            Assert.That(menuSettings, Is.Not.Null);
            Assert.That(Object.FindObjectOfType<InputSystemUIInputModule>(), Is.Not.Null);
            menuSettings.Open();
            VerifyDropdownReadability();
            VerifySteppedSliders();
            menuSettings.Close();

            SceneLoadRequest.TargetScene = "Gameplay";
            SceneManager.LoadScene("Loading");
            float timeout = Time.realtimeSinceStartup + 8f;
            while (SceneManager.GetActiveScene().name != "Gameplay" && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Gameplay"));
            Assert.That(Object.FindObjectOfType<GameBootstrap>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator ThreeDimensionalGameplayBootsSpawnsAndUpgrades()
        {
            yield return VerifyPlayableLoop("Gameplay");
        }

        private static IEnumerator VerifyPlayableLoop(string sceneName)
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
            Assert.That(Object.FindObjectsOfType<TurretSlot>(true).Length, Is.EqualTo(24));
            Assert.That(Object.FindObjectOfType<CoreHealth>(), Is.Not.Null);
            Assert.That(Object.FindObjectOfType<HudController>(), Is.Not.Null);
            UpgradeTreeController upgradeTree = Object.FindObjectOfType<UpgradeTreeController>(true);
            Assert.That(upgradeTree, Is.Not.Null);
            AudioSource upgradeTreeAudio = upgradeTree.GetComponent<AudioSource>();
            Assert.That(upgradeTreeAudio, Is.Not.Null);
            Assert.That(upgradeTreeAudio.clip, Is.Not.Null);
            Assert.That(upgradeTreeAudio.clip.name, Is.EqualTo("升级"));
            Assert.That(Object.FindObjectOfType<GameInputController>(), Is.Not.Null);
            Assert.That(Object.FindObjectOfType<SceneSettingsApplier>(), Is.Not.Null);
            Assert.That(Object.FindObjectOfType<PauseMenuController>(true), Is.Not.Null);
            SettingsMenuController gameplaySettings = Object.FindObjectOfType<SettingsMenuController>(true);
            Assert.That(gameplaySettings, Is.Not.Null);
            Assert.That(Object.FindObjectOfType<InputSystemUIInputModule>(), Is.Not.Null);
            gameplaySettings.Open();
            VerifyDropdownReadability();
            VerifySteppedSliders();
            gameplaySettings.Close();
            Assert.That(bootstrap.KillWallet, Is.Not.Null);
            Assert.That(bootstrap.TurretStats, Is.Not.Null);
            Assert.That(bootstrap.TurretRegistry, Is.Not.Null);
            Assert.That(bootstrap.UpgradeSystem, Is.Not.Null);

            CubeZoneGridController zoneGrid = Object.FindObjectOfType<CubeZoneGridController>();
            Assert.That(zoneGrid, Is.Not.Null);
            CubeZoneVolume[] zoneVolumes = Object.FindObjectsOfType<CubeZoneVolume>(true);
            Assert.That(zoneVolumes.Length, Is.EqualTo(8));
            for (int i = 0; i < zoneVolumes.Length; i++)
            {
                Assert.That(zoneVolumes[i].gameObject.layer, Is.EqualTo(CubeZoneGridController.ZoneRaycastLayer));
            }
            Assert.That(Object.FindObjectOfType<CubeZoneAssignmentController>(true), Is.Not.Null);
            Assert.That(zoneGrid.Config.AvailableEffects.Length, Is.EqualTo(3));
            CubeZoneEffectDefinition blueZoneEffect = zoneGrid.GetAssignedEffect(0);
            CubeZoneEffectDefinition redZoneEffect = zoneGrid.GetAssignedEffect(1);
            CubeZoneEffectDefinition greenZoneEffect = zoneGrid.Config.AvailableEffects[2];
            Assert.That(blueZoneEffect.EffectType, Is.EqualTo(CubeZoneEffectType.TurretFireRateBoost));
            Assert.That(redZoneEffect.EffectType, Is.EqualTo(CubeZoneEffectType.EnemySlowAndDamage));
            Assert.That(greenZoneEffect.EffectType, Is.EqualTo(CubeZoneEffectType.TurretDamageBoost));
            Assert.That(greenZoneEffect.UnlockRequirement, Is.Not.Null);
            zoneGrid.AssignEffect(0, redZoneEffect);
            Assert.That(zoneGrid.GetAssignedEffect(0), Is.EqualTo(redZoneEffect));
            zoneGrid.AssignEffect(0, blueZoneEffect);
            yield return null;
            TurretController[] zoneTurrets = Object.FindObjectsOfType<TurretController>();
            for (int i = 0; i < zoneTurrets.Length; i++)
            {
                int zoneIndex = zoneGrid.GetZoneIndex(zoneTurrets[i].transform.position);
                CubeZoneEffectDefinition effect = zoneGrid.GetAssignedEffect(zoneIndex);
                float expectedMultiplier = effect != null && effect.EffectType == CubeZoneEffectType.TurretFireRateBoost
                    ? effect.TurretFireIntervalMultiplier : 1f;
                Assert.That(zoneTurrets[i].ZoneFireIntervalMultiplier,
                    Is.EqualTo(expectedMultiplier).Within(0.001f));
            }

            TurretController planarAimTurret = Object.FindObjectOfType<TurretController>();
            Vector3 ringNormal = planarAimTurret.transform.parent.up;
            planarAimTurret.ApplyPlanarVisualAim(planarAimTurret.transform.forward + ringNormal * 4f);
            Assert.That(Mathf.Abs(Vector3.Dot(planarAimTurret.transform.forward, ringNormal)), Is.LessThan(0.001f));

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

            Assert.That(Object.FindObjectsOfType<DirectionalSpriteRenderer>().Length, Is.EqualTo(0));

            EndlessEnemySpawner spawner = Object.FindObjectOfType<EndlessEnemySpawner>();
            Assert.That(spawner, Is.Not.Null);
            Assert.That(spawner.EnemyTypeCount, Is.EqualTo(2));
            Assert.That(spawner.HasRangedEnemyType, Is.True);
            EnemyConfig rangedEnemy = null;
            for (int i = 0; i < spawner.EnemyTypeCount; i++)
            {
                EnemyConfig candidate = spawner.GetEnemyTypeConfig(i);
                if (candidate != null && candidate.AttackMode == EnemyAttackMode.RangedTurret)
                {
                    rangedEnemy = candidate;
                    break;
                }
            }
            Assert.That(rangedEnemy, Is.Not.Null);
            Assert.That(rangedEnemy.ProjectilePrefab, Is.Not.Null);
            yield return VerifyEnemyProjectileDamagesTurret(bootstrap, rangedEnemy);
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
            killTarget.transform.position = zoneGrid.transform.TransformPoint(new Vector3(3f, -3f, -3f));
            yield return null;
            Assert.That(killTarget.ZoneSpeedMultiplier, Is.EqualTo(redZoneEffect.EnemySpeedMultiplier).Within(0.001f));
            Assert.That(killTarget.ZoneDamagePerSecond, Is.EqualTo(redZoneEffect.EnemyDamagePerSecond).Within(0.001f));
            SingleSpriteBillboardVisual billboardVisual = killTarget.GetComponentInChildren<SingleSpriteBillboardVisual>();
            Assert.That(billboardVisual, Is.Not.Null);
            Assert.That(billboardVisual.TargetRenderer, Is.Not.Null);
            yield return null;
            Assert.That(Vector3.Dot(billboardVisual.transform.forward, Camera.main.transform.forward), Is.GreaterThan(0.999f));

            TurretController[] firingTurrets = Object.FindObjectsOfType<TurretController>();
            for (int i = 0; i < firingTurrets.Length; i++)
            {
                firingTurrets[i].enabled = false;
            }
            ProjectileService fusionService = bootstrap.ProjectileService;
            Assert.That(fusionService, Is.Not.Null);
            int fusionCountBefore = fusionService.SuccessfulFusionCount;
            Vector3 fusionOrigin = killTarget.transform.position + Camera.main.transform.up * 4f;
            fusionService.Fire(fusionOrigin, killTarget, 1f, TurretProjectileType.A);
            fusionService.Fire(fusionOrigin, killTarget, 1f, TurretProjectileType.B);
            float fusionTimeout = Time.realtimeSinceStartup + 1f;
            while (fusionService.SuccessfulFusionCount == fusionCountBefore &&
                   Time.realtimeSinceStartup < fusionTimeout)
            {
                yield return null;
            }
            Assert.That(fusionService.SuccessfulFusionCount, Is.EqualTo(fusionCountBefore + 1));
            Assert.That(fusionService.LastFusedDamage, Is.EqualTo(2.7f).Within(0.01f));
            for (int i = 0; i < firingTurrets.Length; i++)
            {
                firingTurrets[i].enabled = true;
            }
            int killsBeforeDamage = bootstrap.KillWallet.TotalKills;
            killTarget.TakeDamage(new DamageInfo(100000f, killTarget.transform.position, bootstrap.gameObject));
            Assert.That(bootstrap.KillWallet.TotalKills, Is.EqualTo(killsBeforeDamage + 1));

            VerifyUpgradeProgression(bootstrap);
            Assert.That(Object.FindObjectsOfType<TurretController>().Length, Is.EqualTo(24));

            TurretHealth disabledTurret = Object.FindObjectOfType<TurretHealth>();
            disabledTurret.TakeDamage(new DamageInfo(disabledTurret.MaxHealth + 1f, disabledTurret.transform.position, bootstrap.gameObject));
            Assert.That(disabledTurret.IsAlive, Is.False);
            Assert.That(disabledTurret.DisabledRemaining, Is.GreaterThan(9.9f));
            spawner.enabled = false;
            Time.timeScale = 20f;
            float recoveryTimeout = Time.realtimeSinceStartup + 1.5f;
            while (!disabledTurret.IsAlive && Time.realtimeSinceStartup < recoveryTimeout)
            {
                yield return null;
            }
            Time.timeScale = 1f;
            Assert.That(disabledTurret.IsAlive, Is.True);
            Assert.That(disabledTurret.CurrentHealth, Is.EqualTo(disabledTurret.MaxHealth).Within(0.01f));
        }

        private static IEnumerator VerifyEnemyProjectileDamagesTurret(GameBootstrap bootstrap, EnemyConfig rangedEnemy)
        {
            TurretHealth target = bootstrap.TurretRegistry.FindNearestOperational(Vector3.zero);
            Assert.That(target, Is.Not.Null);
            float healthBefore = target.CurrentHealth;
            EnemyProjectileController projectile = Object.Instantiate(rangedEnemy.ProjectilePrefab);
            Vector3 origin = target.transform.position - target.transform.forward * 2f;
            bool released = false;
            projectile.Launch(origin, target.transform.position - origin, rangedEnemy.ProjectileDamage,
                20f, rangedEnemy.ProjectileHitRadius, 2f, bootstrap.TurretRegistry,
                item =>
                {
                    released = true;
                    item.gameObject.SetActive(false);
                });
            float timeout = Time.realtimeSinceStartup + 1f;
            while (!released && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }
            Assert.That(released, Is.True);
            Assert.That(target.CurrentHealth, Is.LessThan(healthBefore));
            Object.Destroy(projectile.gameObject);
        }

        private static void VerifyDropdownReadability()
        {
            Dropdown[] dropdowns = Object.FindObjectsOfType<Dropdown>(true);
            Assert.That(dropdowns.Length, Is.GreaterThanOrEqualTo(5));
            for (int i = 0; i < dropdowns.Length; i++)
            {
                Dropdown dropdown = dropdowns[i];
                Assert.That(dropdown.options.Count, Is.GreaterThan(0), dropdown.name);
                Assert.That(dropdown.template, Is.Not.Null, dropdown.name);
                Assert.That(dropdown.itemText, Is.Not.Null, dropdown.name);
                Assert.That(dropdown.captionText.verticalOverflow, Is.EqualTo(VerticalWrapMode.Overflow), dropdown.name);
                Assert.That(dropdown.itemText.verticalOverflow, Is.EqualTo(VerticalWrapMode.Overflow), dropdown.name);
                Image background = dropdown.template.GetComponent<Image>();
                Assert.That(background, Is.Not.Null, dropdown.name);
                float backgroundLuminance = background.color.grayscale;
                float textLuminance = dropdown.itemText.color.grayscale;
                Assert.That(textLuminance - backgroundLuminance, Is.GreaterThan(0.35f), dropdown.name);
            }
        }

        private static void VerifySteppedSliders()
        {
            Slider[] sliders = Object.FindObjectsOfType<Slider>(true);
            Assert.That(sliders.Length, Is.GreaterThanOrEqualTo(6));
            for (int i = 0; i < sliders.Length; i++)
            {
                SliderStepQuantizer quantizer = sliders[i].GetComponent<SliderStepQuantizer>();
                Assert.That(quantizer, Is.Not.Null, sliders[i].name);
                Assert.That(quantizer.IntervalCount, Is.EqualTo(10), sliders[i].name);
                float original = sliders[i].value;
                quantizer.SetValueWithoutNotify(Mathf.Lerp(sliders[i].minValue, sliders[i].maxValue, 0.36f));
                float normalized = Mathf.InverseLerp(sliders[i].minValue, sliders[i].maxValue, sliders[i].value);
                Assert.That(normalized, Is.EqualTo(0.4f).Within(0.001f), sliders[i].name);
                quantizer.SetValueWithoutNotify(original);
            }
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
            Assert.That(bootstrap.TurretStats.DisableDuration, Is.EqualTo(10f).Within(0.01f));
        }
    }
}
