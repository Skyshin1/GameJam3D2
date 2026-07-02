using UnityEngine;

namespace AnchorDefense
{
    public sealed class EndlessEnemySpawner : MonoBehaviour
    {
        private EndlessModeConfig endlessConfig;
        private EnemyConfig enemyConfig;
        private EnemyRegistry registry;
        private CoreHealth core;
        private GameFlowController gameFlow;
        private VfxService vfxService;
        private ComponentPool<EnemyController> pool;
        private float spawnTimer;

        public float ElapsedTime { get; private set; }
        public int TotalSpawned { get; private set; }

        public void Initialize(
            EndlessModeConfig modeConfig,
            EnemyConfig config,
            EnemyRegistry enemyRegistry,
            CoreHealth targetCore,
            GameFlowController flow,
            VfxService effects,
            Transform poolRoot)
        {
            endlessConfig = modeConfig;
            enemyConfig = config;
            registry = enemyRegistry;
            core = targetCore;
            gameFlow = flow;
            vfxService = effects;
            pool = new ComponentPool<EnemyController>(CreateEnemy, poolRoot, endlessConfig.EnemyPrewarmCount);
            spawnTimer = 0.35f;
            ElapsedTime = 0f;
            TotalSpawned = 0;
        }

        private void Update()
        {
            if (gameFlow == null || !gameFlow.IsPlaying)
            {
                return;
            }

            ElapsedTime += Time.deltaTime;
            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f)
            {
                return;
            }

            SpawnBatch();
            spawnTimer += endlessConfig.GetSpawnInterval(ElapsedTime);
        }

        private void SpawnBatch()
        {
            int availableSlots = endlessConfig.MaxAliveEnemies - registry.Count;
            int count = Mathf.Min(endlessConfig.GetBatchSize(ElapsedTime), availableSlots);
            float healthMultiplier = endlessConfig.GetHealthMultiplier(ElapsedTime);
            float speedMultiplier = endlessConfig.GetSpeedMultiplier(ElapsedTime);

            for (int i = 0; i < count; i++)
            {
                EnemyController enemy = pool.Get();
                Vector3 direction = Random.onUnitSphere;
                float radius = Random.Range(endlessConfig.MinSpawnRadius, endlessConfig.MaxSpawnRadius);
                enemy.transform.position = core.transform.position + direction * radius;
                enemy.transform.rotation = Quaternion.LookRotation(-direction, Vector3.up);
                enemy.Initialize(
                    enemyConfig,
                    core,
                    healthMultiplier,
                    speedMultiplier,
                    ReleaseEnemy,
                    vfxService.SpawnHit,
                    vfxService.SpawnDeath);
                registry.Register(enemy);
                TotalSpawned++;
            }
        }

        private void ReleaseEnemy(EnemyController enemy)
        {
            registry.Unregister(enemy);
            pool.Release(enemy);
        }

        private EnemyController CreateEnemy()
        {
            return Instantiate(enemyConfig.Prefab);
        }
    }
}
