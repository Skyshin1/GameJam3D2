using UnityEngine;

namespace AnchorDefense
{
    public sealed class EnemyProjectileService
    {
        private readonly EnemyConfig config;
        private readonly TurretRegistry targets;
        private readonly ComponentPool<EnemyProjectileController> pool;

        public EnemyProjectileService(EnemyConfig enemyConfig, TurretRegistry turretRegistry, Transform poolRoot, int prewarmCount)
        {
            config = enemyConfig;
            targets = turretRegistry;
            if (config.ProjectilePrefab != null)
            {
                pool = new ComponentPool<EnemyProjectileController>(
                    () => Object.Instantiate(config.ProjectilePrefab), poolRoot, prewarmCount);
            }
        }

        public void Fire(Vector3 origin, Vector3 direction)
        {
            if (pool == null)
            {
                return;
            }
            EnemyProjectileController projectile = pool.Get();
            projectile.Launch(origin, direction, config.ProjectileDamage, config.ProjectileSpeed,
                config.ProjectileHitRadius, config.ProjectileLifetime, targets, pool.Release);
        }
    }
}
