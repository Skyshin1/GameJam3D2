using System.Collections.Generic;
using UnityEngine;

namespace AnchorDefense
{
    public sealed class ProjectileService
    {
        private readonly TurretConfig config;
        private readonly Dictionary<TurretProjectileType, ComponentPool<ProjectileController>> pools =
            new Dictionary<TurretProjectileType, ComponentPool<ProjectileController>>();
        private readonly List<ProjectileController> activeProjectiles = new List<ProjectileController>(128);
        private readonly ComponentPool<PooledParticleEffect> muzzlePool;
        private readonly ComponentPool<PooledParticleEffect> fusionEffectPool;

        public int ActiveProjectileCount => activeProjectiles.Count;
        public int SuccessfulFusionCount { get; private set; }
        public float LastFusedDamage { get; private set; }

        public ProjectileService(TurretConfig turretConfig, Transform poolRoot, int prewarmCount)
        {
            config = turretConfig;
            ProjectileController projectileB = config.ProjectileBPrefab != null
                ? config.ProjectileBPrefab : config.ProjectilePrefab;
            ProjectileController fusedProjectile = config.FusedProjectilePrefab != null
                ? config.FusedProjectilePrefab : config.ProjectilePrefab;
            pools[TurretProjectileType.A] = CreatePool(config.ProjectilePrefab, poolRoot, Mathf.Max(1, prewarmCount / 2));
            pools[TurretProjectileType.B] = CreatePool(projectileB, poolRoot, Mathf.Max(1, prewarmCount / 3));
            pools[TurretProjectileType.Fused] = CreatePool(fusedProjectile, poolRoot, Mathf.Max(1, prewarmCount / 6));

            if (config.MuzzleEffectPrefab != null)
            {
                muzzlePool = new ComponentPool<PooledParticleEffect>(
                    () => Object.Instantiate(config.MuzzleEffectPrefab), poolRoot, 24);
            }
            if (config.FusionEffectPrefab != null)
            {
                fusionEffectPool = new ComponentPool<PooledParticleEffect>(
                    () => Object.Instantiate(config.FusionEffectPrefab), poolRoot, 16);
            }
        }

        public void Fire(Vector3 origin, EnemyController target, float damage)
        {
            Fire(origin, target, damage, TurretProjectileType.A);
        }

        public void Fire(Vector3 origin, EnemyController target, float damage, TurretProjectileType type)
        {
            Fire(origin, target, damage, type, 1f, 1f);
        }

        public void Fire(Vector3 origin, EnemyController target, float damage, TurretProjectileType type,
            float speedMultiplier, float hitRadiusMultiplier)
        {
            if (target == null || !target.IsAlive)
            {
                return;
            }
            TurretProjectileType safeType = type == TurretProjectileType.Fused ? TurretProjectileType.A : type;
            Spawn(origin, target, damage, safeType,
                config.ProjectileSpeed * Mathf.Max(0.1f, speedMultiplier),
                config.ProjectileHitRadius * Mathf.Max(0.1f, hitRadiusMultiplier),
                config.ProjectileLifetime);
            SpawnMuzzle(origin, GetProjectileColor(safeType));
        }

        private static ComponentPool<ProjectileController> CreatePool(
            ProjectileController prefab, Transform poolRoot, int prewarmCount)
        {
            return new ComponentPool<ProjectileController>(
                () => Object.Instantiate(prefab), poolRoot, prewarmCount);
        }

        private void Spawn(Vector3 origin, EnemyController target, float damage,
            TurretProjectileType type, float speed, float hitRadius, float lifetime)
        {
            ComponentPool<ProjectileController> pool = pools[type];
            ProjectileController projectile = pool.Get();
            projectile.Launch(origin, target, damage, speed, hitRadius, lifetime, type,
                ReleaseProjectile, HandleProjectileMoved);
            activeProjectiles.Add(projectile);
        }

        private void HandleProjectileMoved(ProjectileController source)
        {
            if (source == null || !source.IsFlying || source.ProjectileType == TurretProjectileType.Fused)
            {
                return;
            }

            TurretProjectileType requiredType = source.ProjectileType == TurretProjectileType.A
                ? TurretProjectileType.B : TurretProjectileType.A;
            float fusionRadiusSqr = config.FusionRadius * config.FusionRadius;
            for (int i = activeProjectiles.Count - 1; i >= 0; i--)
            {
                ProjectileController other = activeProjectiles[i];
                if (other == null || other == source || !other.IsFlying || other.ProjectileType != requiredType)
                {
                    continue;
                }
                if ((source.transform.position - other.transform.position).sqrMagnitude > fusionRadiusSqr)
                {
                    continue;
                }

                EnemyController target = SelectFusionTarget(source, other);
                if (target == null)
                {
                    return;
                }

                Vector3 fusionPosition = (source.transform.position + other.transform.position) * 0.5f;
                float fusedDamage = (source.Damage + other.Damage) * config.FusionDamageMultiplier;
                LastFusedDamage = fusedDamage;
                source.ReleaseForFusion();
                other.ReleaseForFusion();
                Spawn(fusionPosition, target, fusedDamage, TurretProjectileType.Fused,
                    config.ProjectileSpeed * config.FusedSpeedMultiplier,
                    config.ProjectileHitRadius * config.FusedHitRadiusMultiplier,
                    config.ProjectileLifetime);
                SpawnFusionEffect(fusionPosition);
                SuccessfulFusionCount++;
                return;
            }
        }

        private static EnemyController SelectFusionTarget(ProjectileController a, ProjectileController b)
        {
            bool aValid = a.Target != null && a.Target.IsAlive;
            bool bValid = b.Target != null && b.Target.IsAlive;
            if (!aValid) return bValid ? b.Target : null;
            if (!bValid) return a.Target;
            Vector3 position = (a.transform.position + b.transform.position) * 0.5f;
            float aDistance = (a.Target.transform.position - position).sqrMagnitude;
            float bDistance = (b.Target.transform.position - position).sqrMagnitude;
            return aDistance <= bDistance ? a.Target : b.Target;
        }

        private void ReleaseProjectile(ProjectileController projectile)
        {
            if (projectile == null)
            {
                return;
            }
            activeProjectiles.Remove(projectile);
            pools[projectile.ProjectileType].Release(projectile);
        }

        private void SpawnMuzzle(Vector3 position, Color color)
        {
            if (muzzlePool == null)
            {
                return;
            }
            PooledParticleEffect muzzle = muzzlePool.Get();
            muzzle.PlayBurst(position, color, 5, 0.28f, muzzlePool.Release);
        }

        private void SpawnFusionEffect(Vector3 position)
        {
            if (fusionEffectPool == null)
            {
                return;
            }
            PooledParticleEffect effect = fusionEffectPool.Get();
            effect.PlayBurst(position, config.FusedProjectileColor, 12, 0.5f, fusionEffectPool.Release);
        }

        private Color GetProjectileColor(TurretProjectileType type)
        {
            switch (type)
            {
                case TurretProjectileType.B: return config.ProjectileBColor;
                case TurretProjectileType.Fused: return config.FusedProjectileColor;
                default: return config.ProjectileColor;
            }
        }
    }
}
