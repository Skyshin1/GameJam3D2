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
            public int FirstWave;
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
        private int surgeSpawned;
        private readonly HashSet<EnemyConfig> introducedTypes = new HashSet<EnemyConfig>();
        private readonly List<EnemyController> collisionEnemies = new List<EnemyController>();
        private readonly Dictionary<EnemyController, (int version, Vector3 position)> previousEnemies = new Dictionary<EnemyController, (int, Vector3)>();
        private readonly Dictionary<TurretHealth, Vector3> previousTurrets = new Dictionary<TurretHealth, Vector3>();

        public float ElapsedTime { get; private set; }
        public int CurrentWave { get; private set; }
        public bool IsSurgeActive { get; private set; }
        public float SecondsUntilNextWave => Mathf.Max(0f, endlessConfig.WavePeriod * (CurrentWave + 1) - ElapsedTime);
        public float SurgeSecondsRemaining => IsSurgeActive ? Mathf.Max(0f, endlessConfig.SurgeDuration - (ElapsedTime - CurrentWave * endlessConfig.WavePeriod)) : 0f;
        public event System.Action<EnemyConfig> FirstEnemySpawned;
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

        public Sprite GetWaveIcon(int wave)
        {
            EnemyTypePool debut = enemyTypes.Find(type => type.FirstWave == wave);
            return debut != null ? debut.Config.WaveIcon : null;
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
            CurrentWave = 0;
            IsSurgeActive = false;
            surgeSpawned = 0;
            introducedTypes.Clear();
            collisionEnemies.Clear();
            previousEnemies.Clear();
            previousTurrets.Clear();
        }

        private void BuildEnemyPools(EnemyConfig fallbackConfig, Transform poolRoot)
        {
            enemyTypes.Clear();
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

                    AddEnemyType(entry.Enemy, entry.SpawnWeight, entry.PrewarmCount, entry.FirstWave, poolRoot);
                }
            }

            if (enemyTypes.Count == 0 && fallbackConfig != null && fallbackConfig.Prefab != null)
            {
                AddEnemyType(fallbackConfig, 1f, endlessConfig.EnemyPrewarmCount, 1, poolRoot);
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

        private void AddEnemyType(EnemyConfig config, float weight, int prewarmCount, int firstWave, Transform poolRoot)
        {
            EnemyTypePool typePool = CreateEnemyTypePool(config, weight, prewarmCount, poolRoot);
            typePool.FirstWave = firstWave;
            enemyTypes.Add(typePool);
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
            if (Time.timeScale <= 0f) return;
            if (gameFlow == null || !gameFlow.IsPlaying || enemyTypes.Count == 0)
            {
                return;
            }

            ElapsedTime += Time.deltaTime;
            int wave = Mathf.FloorToInt(ElapsedTime / endlessConfig.WavePeriod);
            if (wave > CurrentWave)
            {
                CurrentWave = wave;
                IsSurgeActive = true;
                surgeSpawned = 0;
            }
            if (IsSurgeActive && ElapsedTime >= CurrentWave * endlessConfig.WavePeriod + endlessConfig.SurgeDuration)
            {
                IsSurgeActive = false;
                spawnTimer = 0f;
            }
            if (IsSurgeActive)
            {
                int quota = endlessConfig.GetSurgeCount(CurrentWave);
                float surgeElapsed = ElapsedTime - CurrentWave * endlessConfig.WavePeriod;
                int batches = Mathf.Max(1, endlessConfig.SurgeBatches);
                int batchIndex = Mathf.Clamp(Mathf.FloorToInt(surgeElapsed / endlessConfig.SurgeDuration * batches), 0, batches - 1);
                int scheduled = Mathf.CeilToInt(quota * (batchIndex + 1) / (float)batches);
                while (surgeSpawned < scheduled)
                {
                    if (registry.Count >= endlessConfig.MaxAliveEnemies) break;
                    SpawnOne(true);
                    surgeSpawned++;
                }
            }
            else
            {
                spawnTimer -= Time.deltaTime;
                if (spawnTimer <= 0f)
                {
                    if (registry.Count < endlessConfig.MaxAliveEnemies) SpawnOne(false);
                    spawnTimer += endlessConfig.GetRegularInterval(CurrentWave);
                }
            }
        }

        private void SpawnOne(bool surge)
        {
            float healthMultiplier = endlessConfig.GetWaveHealthMultiplier(Mathf.Max(1, CurrentWave));
            bool guaranteedDebut = surge && surgeSpawned == 0 && (CurrentWave == 2 || CurrentWave == 3);
            if (!guaranteedDebut && ShouldSpawnBoss())
            {
                SpawnEnemy(bossType, healthMultiplier, 1f, true);
                normalEnemiesSinceBoss = 0;
                return;
            }
            EnemyTypePool typePool = guaranteedDebut
                ? enemyTypes.Find(t => t.FirstWave == CurrentWave)
                : null;
            if (typePool == null) typePool = SelectEnemyType();
            SpawnEnemy(typePool, healthMultiplier, 1f, false);
            normalEnemiesSinceBoss++;
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
            float weight = 0f;
            foreach (EnemyTypePool type in enemyTypes) if (type.FirstWave <= CurrentWave) weight += type.Weight;
            float roll = Random.value * weight;

            for (int i = 0; i < enemyTypes.Count; i++)
            {
                if (enemyTypes[i].FirstWave > CurrentWave) continue;
                roll -= enemyTypes[i].Weight;
                if (roll <= 0f)
                {
                    return enemyTypes[i];
                }
            }

            return enemyTypes[0];
        }

        private void SpawnEnemy(
            EnemyTypePool typePool,
            float healthMultiplier,
            float speedMultiplier,
            bool isBoss)
        {
            EnemyController enemy = typePool.Pool.Get();

            Vector3 direction = Random.onUnitSphere;
            if (typePool.Config.TurretCollisionOnly)
            {
                var available = new List<TurretHealth>();
                foreach (TurretHealth turret in turretRegistry.Turrets)
                    if (turret != null && turret.gameObject.activeInHierarchy && turret.IsAlive) available.Add(turret);
                if (available.Count > 0)
                {
                    TurretHealth turret = available[Random.Range(0, available.Count)];
                    OrbitRingController ring = turret.GetComponentInParent<OrbitRingController>();
                    Vector3 normal = ring != null ? ring.transform.up : Vector3.up;
                    direction = Vector3.ProjectOnPlane(turret.transform.position - core.transform.position, normal);
                    if (direction.sqrMagnitude < 0.001f) direction = Vector3.ProjectOnPlane(Random.onUnitSphere, normal);
                    direction.Normalize();
                }
            }

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
            if (typePool.Config.TurretCollisionOnly) collisionEnemies.Add(enemy);

            if (isBoss)
            {
                aliveBossCount++;
            }

            TotalSpawned++;
            if (introducedTypes.Add(typePool.Config)) FirstEnemySpawned?.Invoke(typePool.Config);
        }

        private void ReleaseEnemy(EnemyTypePool typePool, EnemyController enemy)
        {
            registry.Unregister(enemy);
            collisionEnemies.Remove(enemy);
            previousEnemies.Remove(enemy);

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

        private void LateUpdate()
        {
            if (Time.timeScale <= 0f || gameFlow == null || !gameFlow.IsPlaying) return;
            for (int i = collisionEnemies.Count - 1; i >= 0; i--)
            {
                EnemyController enemy = collisionEnemies[i];
                if (enemy == null || !enemy.IsAlive || enemy.Definition == null || !enemy.Definition.TurretCollisionOnly) continue;
                Vector3 end = enemy.transform.position;
                Vector3 start = previousEnemies.TryGetValue(enemy, out var prior) && prior.version == enemy.SpawnVersion ? prior.position : end;
                foreach (TurretHealth turret in turretRegistry.Turrets)
                {
                    if (turret == null || !turret.gameObject.activeInHierarchy || !turret.IsAlive) continue;
                    Vector3 turretEnd = turret.transform.position;
                    Vector3 turretStart = previousTurrets.TryGetValue(turret, out Vector3 position) ? position : turretEnd;
                    Vector3 relativeStart = start - turretStart;
                    Vector3 relativeStep = (end - start) - (turretEnd - turretStart);
                    float fraction = relativeStep.sqrMagnitude > 0.000001f ? Mathf.Clamp01(-Vector3.Dot(relativeStart, relativeStep) / relativeStep.sqrMagnitude) : 0f;
                    if ((relativeStart + relativeStep * fraction).sqrMagnitude <= enemy.Definition.TurretCollisionRadius * enemy.Definition.TurretCollisionRadius)
                    {
                        enemy.DestroyByTurretCollision();
                        break;
                    }
                }
                if (enemy.IsAlive) previousEnemies[enemy] = (enemy.SpawnVersion, end);
            }
            foreach (TurretHealth turret in turretRegistry.Turrets)
                if (turret != null && turret.gameObject.activeInHierarchy && turret.IsAlive)
                    previousTurrets[turret] = turret.transform.position;
                else if (turret != null) previousTurrets.Remove(turret);
        }
    }
}
