using System;
using UnityEngine;

namespace AnchorDefense
{
    public sealed class CubeZoneGridController : MonoBehaviour
    {
        [SerializeField] private CubeZoneConfig config;
        [SerializeField] private CubeZoneVolume[] zoneVolumes;

        private readonly CubeZoneEffectDefinition[] assignments =
            new CubeZoneEffectDefinition[CubeZoneConfig.ZoneCount];
        private EnemyRegistry enemyRegistry;
        private TurretRegistry turretRegistry;
        private bool initialized;

        public CubeZoneConfig Config => config;
        public event Action<int, CubeZoneEffectDefinition> AssignmentChanged;

        public void Configure(CubeZoneConfig zoneConfig, CubeZoneVolume[] volumes)
        {
            config = zoneConfig;
            zoneVolumes = volumes;
        }

        public void Initialize(EnemyRegistry enemies, TurretRegistry turrets, Transform core)
        {
            enemyRegistry = enemies;
            turretRegistry = turrets;
            if (core != null)
            {
                transform.position = core.position;
            }
            for (int i = 0; i < assignments.Length; i++)
            {
                assignments[i] = config != null ? config.GetDefaultEffect(i) : null;
            }
            RefreshAllVisuals();
            initialized = config != null && enemyRegistry != null && turretRegistry != null;
        }

        public CubeZoneEffectDefinition GetAssignedEffect(int zoneIndex)
        {
            return zoneIndex >= 0 && zoneIndex < assignments.Length ? assignments[zoneIndex] : null;
        }

        public void AssignEffect(int zoneIndex, CubeZoneEffectDefinition effect)
        {
            if (zoneIndex < 0 || zoneIndex >= assignments.Length)
            {
                return;
            }
            assignments[zoneIndex] = effect;
            RefreshVisual(zoneIndex);
            AssignmentChanged?.Invoke(zoneIndex, effect);
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }
            ApplyTurretEffects();
            ApplyEnemyEffects();
        }

        private void ApplyTurretEffects()
        {
            var turrets = turretRegistry.Turrets;
            for (int i = 0; i < turrets.Count; i++)
            {
                TurretHealth health = turrets[i];
                if (health == null)
                {
                    continue;
                }
                CubeZoneEffectDefinition effect = GetEffectAtPosition(health.transform.position);
                float intervalMultiplier = effect != null && effect.EffectType == CubeZoneEffectType.TurretFireRateBoost
                    ? effect.TurretFireIntervalMultiplier : 1f;
                health.GetComponent<TurretController>()?.SetZoneFireIntervalMultiplier(intervalMultiplier);
            }
        }

        private void ApplyEnemyEffects()
        {
            var enemies = enemyRegistry.ActiveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyController enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }
                CubeZoneEffectDefinition effect = GetEffectAtPosition(enemy.transform.position);
                if (effect != null && effect.EffectType == CubeZoneEffectType.EnemySlowAndDamage)
                {
                    enemy.SetZoneEffect(effect.EnemySpeedMultiplier, effect.EnemyDamagePerSecond);
                }
                else
                {
                    enemy.SetZoneEffect(1f, 0f);
                }
            }
        }

        private CubeZoneEffectDefinition GetEffectAtPosition(Vector3 worldPosition)
        {
            int index = GetZoneIndex(worldPosition);
            return index >= 0 ? assignments[index] : null;
        }

        public int GetZoneIndex(Vector3 worldPosition)
        {
            if (config == null)
            {
                return -1;
            }
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            float extent = config.GridHalfExtent;
            if (Mathf.Abs(local.x) > extent || Mathf.Abs(local.y) > extent || Mathf.Abs(local.z) > extent)
            {
                return -1;
            }
            int index = local.x >= 0f ? 1 : 0;
            if (local.y >= 0f) index |= 2;
            if (local.z >= 0f) index |= 4;
            return index;
        }

        private void RefreshAllVisuals()
        {
            for (int i = 0; i < CubeZoneConfig.ZoneCount; i++)
            {
                RefreshVisual(i);
            }
        }

        private void RefreshVisual(int zoneIndex)
        {
            if (zoneVolumes == null)
            {
                return;
            }
            for (int i = 0; i < zoneVolumes.Length; i++)
            {
                if (zoneVolumes[i] != null && zoneVolumes[i].ZoneIndex == zoneIndex)
                {
                    zoneVolumes[i].SetEffect(assignments[zoneIndex]);
                    return;
                }
            }
        }
    }
}
