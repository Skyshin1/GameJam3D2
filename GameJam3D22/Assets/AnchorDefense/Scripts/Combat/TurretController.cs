using UnityEngine;

namespace AnchorDefense
{
    public sealed class TurretController : MonoBehaviour
    {
        [SerializeField] private Transform firePoint;

        private TurretConfig config;
        private EnemyRegistry registry;
        private ProjectileService projectileService;
        private EnemyController currentTarget;
        private float cooldown;

        public void Initialize(
            TurretConfig turretConfig,
            EnemyRegistry enemyRegistry,
            ProjectileService projectiles)
        {
            config = turretConfig;
            registry = enemyRegistry;
            projectileService = projectiles;
            cooldown = Random.Range(0f, config.FireInterval);
        }

        public void ConfigureFirePoint(Transform projectileOrigin)
        {
            firePoint = projectileOrigin;
        }

        private void Update()
        {
            if (config == null || firePoint == null)
            {
                return;
            }

            cooldown -= Time.deltaTime;
            if (!IsTargetValid(currentTarget))
            {
                currentTarget = FindNearestTarget();
            }

            if (currentTarget == null)
            {
                return;
            }

            Vector3 direction = currentTarget.transform.position - transform.position;
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized, transform.parent.up);
            }

            if (cooldown <= 0f)
            {
                projectileService.Fire(firePoint.position, currentTarget);
                cooldown = config.FireInterval;
            }
        }

        private bool IsTargetValid(EnemyController target)
        {
            return target != null && target.IsAlive &&
                   (target.transform.position - transform.position).sqrMagnitude <= config.Range * config.Range;
        }

        private EnemyController FindNearestTarget()
        {
            EnemyController nearest = null;
            float nearestSqrDistance = config.Range * config.Range;
            var enemies = registry.ActiveEnemies;

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyController enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                float sqrDistance = (enemy.transform.position - transform.position).sqrMagnitude;
                if (sqrDistance < nearestSqrDistance)
                {
                    nearest = enemy;
                    nearestSqrDistance = sqrDistance;
                }
            }

            return nearest;
        }
    }
}
