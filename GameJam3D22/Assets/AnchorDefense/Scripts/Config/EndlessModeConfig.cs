using UnityEngine;

namespace AnchorDefense
{
    [System.Serializable]
    public sealed class EnemySpawnEntry
    {
        [SerializeField] private EnemyConfig enemy;
        [SerializeField, Min(0f)] private float spawnWeight = 1f;
        [SerializeField, Min(0)] private int prewarmCount = 16;
        [SerializeField, Min(1)] private int firstWave = 1;

        public EnemyConfig Enemy => enemy;
        public float SpawnWeight => spawnWeight;
        public int PrewarmCount => prewarmCount;
        public int FirstWave => Mathf.Max(1, firstWave);
    }

    [CreateAssetMenu(menuName = "Anchor Defense/Endless Mode Config", fileName = "EndlessModeConfig")]
    public sealed class EndlessModeConfig : ScriptableObject
    {
        [field: Header("Spawn Area")]
        [field: SerializeField, Min(1f)] public float MinSpawnRadius { get; private set; } = 15f;
        [field: SerializeField, Min(1f)] public float MaxSpawnRadius { get; private set; } = 18f;

        [field: Header("Difficulty")]
        [field: SerializeField, Min(0.03f)] public float StartingInterval { get; private set; } = 1.05f;
        [field: SerializeField, Min(0.6f)] public float MinimumInterval { get; private set; } = 0.6f;
        [field: SerializeField, Min(1)] public int MaxAliveEnemies { get; private set; } = 350;
        [field: SerializeField, Min(0)] public int EnemyPrewarmCount { get; private set; } = 70;
        [field: SerializeField, Min(0)] public int ProjectilePrewarmCount { get; private set; } = 90;

        [field: Header("Timed Waves")]
        [field: SerializeField, Min(1f)] public float WavePeriod { get; private set; } = 30f;
        [field: SerializeField, Min(1f)] public float SurgeDuration { get; private set; } = 10f;
        [field: SerializeField, Min(1)] public int SurgeBatches { get; private set; } = 5;
        [field: SerializeField, Min(1)] public int FirstSurgeCount { get; private set; } = 24;
        [field: SerializeField, Min(0)] public int SurgeIncreasePerWave { get; private set; } = 1;
        [field: SerializeField, Range(0f, 0.5f)] public float IntervalReductionPerWave { get; private set; } = 0.03f;
        [field: SerializeField, Min(0f)] public float HealthIncreasePerWave { get; private set; } = 0.08f;

        public float GetRegularInterval(int completedWaves) =>
            Mathf.Max(Mathf.Max(0.6f, MinimumInterval), StartingInterval * Mathf.Pow(1f - IntervalReductionPerWave, Mathf.Max(0, completedWaves)));
        public float GetWaveHealthMultiplier(int wave) => 1f + Mathf.Max(0, wave - 1) * HealthIncreasePerWave;
        public int GetSurgeCount(int wave) => FirstSurgeCount + Mathf.Max(0, wave - 1) * SurgeIncreasePerWave;

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

    }
}
