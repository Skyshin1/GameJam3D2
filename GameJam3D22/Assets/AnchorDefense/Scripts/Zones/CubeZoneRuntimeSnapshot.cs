using UnityEngine;

namespace AnchorDefense
{
    public readonly struct CubeZoneRuntimeSnapshot
    {
        public CubeZoneRuntimeSnapshot(int zoneId, Vector3Int gridPosition, Vector3 center,
            int enemyCount, int turretCount, int damagedTurretCount, float missingTurretHealth,
            CubeZoneEffectDefinition persistentEffect)
        {
            ZoneId = zoneId;
            GridPosition = gridPosition;
            Center = center;
            EnemyCount = enemyCount;
            TurretCount = turretCount;
            DamagedTurretCount = damagedTurretCount;
            MissingTurretHealth = missingTurretHealth;
            PersistentEffect = persistentEffect;
        }

        public int ZoneId { get; }
        public Vector3Int GridPosition { get; }
        public Vector3 Center { get; }
        public int EnemyCount { get; }
        public int TurretCount { get; }
        public int DamagedTurretCount { get; }
        public float MissingTurretHealth { get; }
        public CubeZoneEffectDefinition PersistentEffect { get; }
    }
}
