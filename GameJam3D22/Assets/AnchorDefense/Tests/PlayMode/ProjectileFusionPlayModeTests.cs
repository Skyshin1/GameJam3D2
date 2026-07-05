using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AnchorDefense.Tests
{
    public sealed class ProjectileFusionPlayModeTests
    {
        [UnityTest]
        public IEnumerator ConfiguredPairFusesIntoConfiguredResult()
        {
            GameObject projectilePrefabObject = new GameObject("Test Projectile Prefab");
            ProjectileController projectilePrefab = projectilePrefabObject.AddComponent<ProjectileController>();

            ProjectileDefinition projectileA = CreateDefinition("a", projectilePrefab, Color.cyan, 1f);
            ProjectileDefinition projectileB = CreateDefinition("b", projectilePrefab, Color.magenta, 1f);
            ProjectileDefinition fused = CreateDefinition("fused", projectilePrefab, Color.yellow, 1.5f);

            var recipe = new ProjectileFusionRecipe();
            recipe.Configure(
                projectileA, projectileB, fused,
                1.5f, 1.35f, 1.1f, 1.5f, 1f,
                null, Color.yellow);
            ProjectileFusionConfig fusionConfig = ScriptableObject.CreateInstance<ProjectileFusionConfig>();
            fusionConfig.Configure(new[] { recipe });

            TurretConfig turretConfig = ScriptableObject.CreateInstance<TurretConfig>();
            turretConfig.ConfigureProjectileSystem(projectileA, fusionConfig);

            EnemyConfig enemyConfig = ScriptableObject.CreateInstance<EnemyConfig>();
            GameObject enemyObject = new GameObject("Test Enemy");
            EnemyController enemy = enemyObject.AddComponent<EnemyController>();
            enemy.transform.position = Vector3.forward * 20f;
            enemy.Initialize(enemyConfig, null, 100f, 1f, _ => { }, null, null);

            GameObject poolRootObject = new GameObject("Test Projectile Pool");
            var service = new ProjectileService(turretConfig, poolRootObject.transform, 3);
            service.Fire(Vector3.zero, enemy, 1f, projectileA);
            service.Fire(Vector3.zero, enemy, 1f, projectileB);

            float timeout = Time.realtimeSinceStartup + 1f;
            while (service.SuccessfulFusionCount == 0 && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            Assert.That(service.SuccessfulFusionCount, Is.EqualTo(1));
            Assert.That(service.LastFusedDamage, Is.EqualTo(2.7f).Within(0.01f));

            ProjectileController[] spawnedProjectiles = Object.FindObjectsOfType<ProjectileController>();
            for (int i = 0; i < spawnedProjectiles.Length; i++)
            {
                if (spawnedProjectiles[i] != projectilePrefab)
                {
                    Object.Destroy(spawnedProjectiles[i].gameObject);
                }
            }
            Object.Destroy(projectilePrefabObject);
            Object.Destroy(enemyObject);
            Object.Destroy(poolRootObject);
            Object.Destroy(projectileA);
            Object.Destroy(projectileB);
            Object.Destroy(fused);
            Object.Destroy(fusionConfig);
            Object.Destroy(turretConfig);
            Object.Destroy(enemyConfig);
        }

        private static ProjectileDefinition CreateDefinition(
            string id,
            ProjectileController prefab,
            Color color,
            float visualScale)
        {
            ProjectileDefinition definition = ScriptableObject.CreateInstance<ProjectileDefinition>();
            definition.Configure(id, id, prefab, color, 1f, 1f, 1f, 1f, visualScale);
            return definition;
        }
    }
}
