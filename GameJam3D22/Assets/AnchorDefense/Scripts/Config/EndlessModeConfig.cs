using UnityEngine;

namespace AnchorDefense
{
    [System.Serializable]
    public sealed class EnemySpawnEntry
    {
        [SerializeField] private EnemyConfig enemy;
        [SerializeField, Min(0f)] private float spawnWeight = 1f;
        [SerializeField, Min(0)] private int prewarmCount = 16;

        public EnemyConfig Enemy => enemy;
        public float SpawnWeight => spawnWeight;
        public int PrewarmCount => prewarmCount;
    }

    [CreateAssetMenu(menuName = "Anchor Defense/Endless Mode Config", fileName = "EndlessModeConfig")]
    public sealed class EndlessModeConfig : ScriptableObject
    {
        [field: Header("Spawn Area")]
        [field: SerializeField, Min(1f)] public float MinSpawnRadius { get; private set; } = 15f;
        [field: SerializeField, Min(1f)] public float MaxSpawnRadius { get; private set; } = 18f;

        [field: Header("Difficulty")]
        [field: SerializeField, Min(0.03f)] public float StartingInterval { get; private set; } = 1.05f;
        [field: SerializeField, Min(0.03f)] public float MinimumInterval { get; private set; } = 0.12f;
        [field: SerializeField, Min(0f)] public float IntervalDecayPerSecond { get; private set; } = 0.012f;
        [field: SerializeField, Min(1)] public int StartingBatchSize { get; private set; } = 1;
        [field: SerializeField, Min(1f)] public float BatchIncreaseEverySeconds { get; private set; } = 35f;
        [field: SerializeField, Min(1)] public int MaxBatchSize { get; private set; } = 7;
        [field: SerializeField, Min(0f)] public float HealthIncreasePerMinute { get; private set; } = 0.3f;
        [field: SerializeField, Min(0f)] public float SpeedIncreasePerMinute { get; private set; } = 0.08f;
        [field: SerializeField, Min(1)] public int MaxAliveEnemies { get; private set; } = 350;
        [field: SerializeField, Min(0)] public int EnemyPrewarmCount { get; private set; } = 70;
        [field: SerializeField, Min(0)] public int ProjectilePrewarmCount { get; private set; } = 90;

        [field: Header("Enemy Roster")]
        [field: SerializeField] public EnemySpawnEntry[] EnemyTypes { get; private set; }

        public bool HasEnemyRoster
        {
            get
            {
                if (EnemyTypes == null)
                {
                    return false;
                }
                for (int i = 0; i < EnemyTypes.Length; i++)
                {
                    if (EnemyTypes[i] != null && EnemyTypes[i].Enemy != null && EnemyTypes[i].SpawnWeight > 0f)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public float GetSpawnInterval(float elapsedSeconds)
        {
            return Mathf.Max(MinimumInterval, StartingInterval * Mathf.Exp(-IntervalDecayPerSecond * elapsedSeconds));
        }

        public int GetBatchSize(float elapsedSeconds)
        {
            int increases = Mathf.FloorToInt(elapsedSeconds / BatchIncreaseEverySeconds);
            return Mathf.Min(MaxBatchSize, StartingBatchSize + increases);
        }

        public float GetHealthMultiplier(float elapsedSeconds)
        {
            return 1f + HealthIncreasePerMinute * elapsedSeconds / 60f;
        }

        public float GetSpeedMultiplier(float elapsedSeconds)
        {
            return 1f + SpeedIncreasePerMinute * elapsedSeconds / 60f;
        }
    }
}
