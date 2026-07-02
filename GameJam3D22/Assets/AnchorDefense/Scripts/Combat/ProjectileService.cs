using UnityEngine;

namespace AnchorDefense
{
    public sealed class ProjectileService
    {
        private readonly TurretConfig config;
        private readonly ComponentPool<ProjectileController> projectilePool;
        private readonly ComponentPool<PooledParticleEffect> muzzlePool;

        public ProjectileService(TurretConfig turretConfig, Transform poolRoot, int prewarmCount)
        {
            config = turretConfig;
            projectilePool = new ComponentPool<ProjectileController>(
                () => Object.Instantiate(config.ProjectilePrefab),
                poolRoot,
                prewarmCount);

            if (config.MuzzleEffectPrefab != null)
            {
                muzzlePool = new ComponentPool<PooledParticleEffect>(
                    () => Object.Instantiate(config.MuzzleEffectPrefab),
                    poolRoot,
                    24);
            }
        }

        public void Fire(Vector3 origin, EnemyController target)
        {
            if (target == null || !target.IsAlive)
            {
                return;
            }

            ProjectileController projectile = projectilePool.Get();
            projectile.Launch(
                origin,
                target,
                config.Damage,
                config.ProjectileSpeed,
                config.ProjectileHitRadius,
                config.ProjectileLifetime,
                projectilePool.Release);

            if (muzzlePool != null)
            {
                PooledParticleEffect muzzle = muzzlePool.Get();
                muzzle.PlayBurst(origin, config.ProjectileColor, 5, 0.28f, muzzlePool.Release);
            }
        }
    }
}
