using UnityEngine;

namespace AnchorDefense
{
    public sealed class TurretController : MonoBehaviour
    {
        [SerializeField] private Transform firePoint;
        [SerializeField] private DirectionalSpriteRenderer directionalVisual;
        [SerializeField] private TurretHealth health;

        private TurretRuntimeStats runtimeStats;
        private EnemyRegistry registry;
        private ProjectileService projectileService;
        private EnemyController currentTarget;
        private float cooldown;

        public void Initialize(
            TurretRuntimeStats turretStats,
            EnemyRegistry enemyRegistry,
            ProjectileService projectiles,
            TurretHitVfxService hitVfx = null)
        {
            runtimeStats = turretStats;
            registry = enemyRegistry;
            projectileService = projectiles;
            health = health != null ? health : GetComponent<TurretHealth>();
            health?.Initialize(runtimeStats, hitVfx != null ? hitVfx.SpawnHit : null);
            cooldown = Random.Range(0f, runtimeStats.FireInterval);
        }

        public TurretHealth Health => health != null ? health : GetComponent<TurretHealth>();

        public void ConfigureFirePoint(Transform projectileOrigin)
        {
            firePoint = projectileOrigin;
        }

        public void ConfigureDirectionalVisual(DirectionalSpriteRenderer visual)
        {
            directionalVisual = visual;
        }

        public void ConfigureHealth(TurretHealth turretHealth)
        {
            health = turretHealth;
        }

        private void Update()
        {
            if (runtimeStats == null || firePoint == null || (health != null && !health.IsAlive))
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
                directionalVisual?.SetWorldDirection(direction);
                transform.rotation = Quaternion.LookRotation(direction.normalized, transform.parent.up);
            }

            if (cooldown <= 0f)
            {
                projectileService.Fire(firePoint.position, currentTarget, runtimeStats.Damage);
                cooldown = runtimeStats.FireInterval;
            }
        }

        private bool IsTargetValid(EnemyController target)
        {
            return target != null && target.IsAlive &&
                   (target.transform.position - transform.position).sqrMagnitude <= runtimeStats.Range * runtimeStats.Range;
        }

        private EnemyController FindNearestTarget()
        {
            EnemyController nearest = null;
            float nearestSqrDistance = runtimeStats.Range * runtimeStats.Range;
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
