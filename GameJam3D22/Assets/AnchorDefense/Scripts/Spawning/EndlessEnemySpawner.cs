using System.Collections.Generic;
using UnityEngine;

namespace AnchorDefense
{
    public sealed class EndlessEnemySpawner : MonoBehaviour
    {
        private sealed class EnemyTypePool
        {
            public EnemyConfig Config;
            public float Weight;
            public ComponentPool<EnemyController> Pool;
            public VfxService Vfx;
            public EnemyProjectileService Projectiles;
        }

        [Header("Boss Spawn")]
        [SerializeField] private EnemyConfig bossConfig;
        [SerializeField, Min(1)] private int bossEveryNormalEnemyCount = 60;
        [SerializeField, Min(1)] private int bossPrewarmCount = 2;
        [SerializeField, Min(0)] private int maxAliveBossCount = 1;

        private readonly List<EnemyTypePool> enemyTypes = new List<EnemyTypePool>();

        private EndlessModeConfig endlessConfig;
        private EnemyRegistry registry;
        private TurretRegistry turretRegistry;
        private CoreHealth core;
        private GameFlowController gameFlow;
        private KillResourceWallet killWallet;

        private EnemyTypePool bossType;
        private int normalEnemiesSinceBoss;
        private int aliveBossCount;

        private float spawnTimer;
        private float totalWeight;

        public float ElapsedTime { get; private set; }
        public int TotalSpawned { get; private set; }
        public int EnemyTypeCount => enemyTypes.Count;

        public bool HasRangedEnemyType
        {
            get
            {
                for (int i = 0; i < enemyTypes.Count; i++)
                {
                    EnemyAttackMode mode = enemyTypes[i].Config.AttackMode;
                    if (mode == EnemyAttackMode.RangedTurret ||
                        mode == EnemyAttackMode.OrbitTurretBoss)
                    {
                        return true;
                    }
                }

                return bossType != null &&
                       bossType.Config != null &&
                       bossType.Config.AttackMode == EnemyAttackMode.OrbitTurretBoss;
            }
        }

        public EnemyConfig GetEnemyTypeConfig(int index)
        {
            return index >= 0 && index < enemyTypes.Count ? enemyTypes[index].Config : null;
        }

        public void Initialize(
            EndlessModeConfig modeConfig,
            EnemyConfig fallbackConfig,
            EnemyRegistry enemyRegistry,
            TurretRegistry turretTargets,
            CoreHealth targetCore,
            GameFlowController flow,
            KillResourceWallet wallet,
            Transform poolRoot)
        {
            endlessConfig = modeConfig;
            registry = enemyRegistry;
            turretRegistry = turretTargets;
            core = targetCore;
            gameFlow = flow;
            killWallet = wallet;

            BuildEnemyPools(fallbackConfig, poolRoot);

            spawnTimer = 0.35f;
            ElapsedTime = 0f;
            TotalSpawned = 0;
            normalEnemiesSinceBoss = 0;
            aliveBossCount = 0;
        }

        private void BuildEnemyPools(EnemyConfig fallbackConfig, Transform poolRoot)
        {
            enemyTypes.Clear();
            totalWeight = 0f;
            bossType = null;
            normalEnemiesSinceBoss = 0;
            aliveBossCount = 0;

            if (endlessConfig.HasEnemyRoster)
            {
                EnemySpawnEntry[] entries = endlessConfig.EnemyTypes;

                for (int i = 0; i < entries.Length; i++)
                {
                    EnemySpawnEntry entry = entries[i];

                    if (entry == null ||
                        entry.Enemy == null ||
                        entry.Enemy.Prefab == null ||
                        entry.SpawnWeight <= 0f)
                    {
                        continue;
                    }

                    AddEnemyType(entry.Enemy, entry.SpawnWeight, entry.PrewarmCount, poolRoot);
                }
            }

            if (enemyTypes.Count == 0 && fallbackConfig != null && fallbackConfig.Prefab != null)
            {
                AddEnemyType(fallbackConfig, 1f, endlessConfig.EnemyPrewarmCount, poolRoot);
            }

            if (bossConfig != null && bossConfig.Prefab != null)
            {
                bossType = CreateEnemyTypePool(
                    bossConfig,
                    0f,
                    bossPrewarmCount,
                    poolRoot);
            }
        }

        private void AddEnemyType(EnemyConfig config, float weight, int prewarmCount, Transform poolRoot)
        {
            EnemyTypePool typePool = CreateEnemyTypePool(config, weight, prewarmCount, poolRoot);

            enemyTypes.Add(typePool);
            totalWeight += weight;
        }

        private EnemyTypePool CreateEnemyTypePool(
            EnemyConfig config,
            float weight,
            int prewarmCount,
            Transform poolRoot)
        {
            EnemyAttackMode mode = config.AttackMode;

            var typePool = new EnemyTypePool
            {
                Config = config,
                Weight = weight,
                Vfx = new VfxService(config, poolRoot),
                Projectiles = mode == EnemyAttackMode.RangedTurret ||
                              mode == EnemyAttackMode.OrbitTurretBoss
                    ? new EnemyProjectileService(config, turretRegistry, poolRoot, Mathf.Max(6, prewarmCount / 2))
                    : null
            };

            typePool.Pool = new ComponentPool<EnemyController>(
                () => CreateEnemy(typePool),
                poolRoot,
                prewarmCount);

            return typePool;
        }

        private void Update()
        {
            if (gameFlow == null || !gameFlow.IsPlaying || enemyTypes.Count == 0)
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
                if (ShouldSpawnBoss())
                {
                    SpawnEnemy(bossType, healthMultiplier, speedMultiplier, true);
                    normalEnemiesSinceBoss = 0;
                    continue;
                }

                EnemyTypePool typePool = SelectEnemyType();
                SpawnEnemy(typePool, healthMultiplier, speedMultiplier, false);
                normalEnemiesSinceBoss++;
            }
        }

        private bool ShouldSpawnBoss()
        {
            return bossType != null &&
                   maxAliveBossCount > 0 &&
                   aliveBossCount < maxAliveBossCount &&
                   bossEveryNormalEnemyCount > 0 &&
                   normalEnemiesSinceBoss >= bossEveryNormalEnemyCount;
        }

        private EnemyTypePool SelectEnemyType()
        {
            float roll = Random.value * totalWeight;

            for (int i = 0; i < enemyTypes.Count; i++)
            {
                roll -= enemyTypes[i].Weight;
                if (roll <= 0f)
                {
                    return enemyTypes[i];
                }
            }

            return enemyTypes[enemyTypes.Count - 1];
        }

        private void SpawnEnemy(
            EnemyTypePool typePool,
            float healthMultiplier,
            float speedMultiplier,
            bool isBoss)
        {
            EnemyController enemy = typePool.Pool.Get();

            Vector3 direction = Random.onUnitSphere;

            if (isBoss && typePool.Config.AttackMode == EnemyAttackMode.OrbitTurretBoss)
            {
                Vector3 axis = typePool.Config.OrbitAxis.sqrMagnitude > 0.001f
                    ? typePool.Config.OrbitAxis.normalized
                    : Vector3.up;

                direction = Vector3.ProjectOnPlane(Random.onUnitSphere, axis);

                if (direction.sqrMagnitude <= 0.001f)
                {
                    direction = Vector3.ProjectOnPlane(Vector3.forward, axis);
                }

                if (direction.sqrMagnitude <= 0.001f)
                {
                    direction = Vector3.right;
                }

                direction.Normalize();

                enemy.transform.position =
                    core.transform.position +
                    direction * typePool.Config.OrbitRadius +
                    axis * typePool.Config.OrbitHeight;

                Vector3 tangent = Vector3.Cross(axis, direction);
                if (tangent.sqrMagnitude <= 0.001f)
                {
                    tangent = -direction;
                }

                enemy.transform.rotation = Quaternion.LookRotation(tangent.normalized, axis);
            }
            else
            {
                float radius = Random.Range(endlessConfig.MinSpawnRadius, endlessConfig.MaxSpawnRadius);

                enemy.transform.position = core.transform.position + direction * radius;
                enemy.transform.rotation = Quaternion.LookRotation(-direction, Vector3.up);
            }

            enemy.Initialize(
                typePool.Config,
                core,
                healthMultiplier,
                speedMultiplier,
                released => ReleaseEnemy(typePool, released),
                typePool.Vfx.SpawnHit,
                typePool.Vfx.SpawnDeath,
                turretRegistry,
                typePool.Projectiles);

            registry.Register(enemy);

            if (isBoss)
            {
                aliveBossCount++;
            }

            TotalSpawned++;
        }

        private void ReleaseEnemy(EnemyTypePool typePool, EnemyController enemy)
        {
            registry.Unregister(enemy);

            if (typePool == bossType && aliveBossCount > 0)
            {
                aliveBossCount--;
            }

            typePool.Pool.Release(enemy);
        }

        private EnemyController CreateEnemy(EnemyTypePool typePool)
        {
            EnemyController enemy = Instantiate(typePool.Config.Prefab);
            enemy.Killed += HandleEnemyKilled;
            return enemy;
        }

        private void HandleEnemyKilled(EnemyController enemy)
        {
            killWallet?.RegisterKill();
        }
    }
}