using System;
using UnityEngine;

namespace AnchorDefense
{
    public sealed class ProjectileController : MonoBehaviour, IPoolable
    {
        private EnemyController target;
        private Action<ProjectileController> releaseAction;
        [SerializeField] private TrailRenderer trail;
        [SerializeField] private DirectionalSpriteRenderer directionalVisual;
        private Vector3 direction;
        private float damage;
        private float speed;
        private float hitRadius;
        private float lifetime;
        private bool isFlying;
        private int targetSpawnVersion;

        public void Configure(TrailRenderer projectileTrail)
        {
            trail = projectileTrail;
        }

        public void ConfigureDirectionalVisual(DirectionalSpriteRenderer visual)
        {
            directionalVisual = visual;
        }

        public void Launch(
            Vector3 position,
            EnemyController newTarget,
            float projectileDamage,
            float projectileSpeed,
            float projectileHitRadius,
            float projectileLifetime,
            Action<ProjectileController> onRelease)
        {
            transform.position = position;
            target = newTarget;
            targetSpawnVersion = target != null ? target.SpawnVersion : 0;
            damage = projectileDamage;
            speed = projectileSpeed;
            hitRadius = projectileHitRadius;
            lifetime = projectileLifetime;
            releaseAction = onRelease;
            direction = target != null ? (target.transform.position - position).normalized : transform.forward;
            directionalVisual?.SetWorldDirection(direction);
            isFlying = true;
            trail?.Clear();
        }

        public void OnTakenFromPool()
        {
            isFlying = false;
        }

        public void OnReturnedToPool()
        {
            isFlying = false;
            target = null;
            targetSpawnVersion = 0;
            releaseAction = null;
            trail?.Clear();
        }

        private void Update()
        {
            if (!isFlying)
            {
                return;
            }

            lifetime -= Time.deltaTime;
            if (lifetime <= 0f)
            {
                Release();
                return;
            }

            if (target != null && target.IsAlive && target.SpawnVersion == targetSpawnVersion)
            {
                Vector3 toTarget = target.transform.position - transform.position;
                float distance = toTarget.magnitude;
                float travelDistance = speed * Time.deltaTime;
                direction = distance > 0.001f ? toTarget / distance : direction;
                directionalVisual?.SetWorldDirection(direction);

                if (distance <= travelDistance + hitRadius)
                {
                    transform.position = target.transform.position;
                    target.TakeDamage(new DamageInfo(damage, transform.position, gameObject));
                    Release();
                    return;
                }
            }

            transform.position += direction * (speed * Time.deltaTime);
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private void Release()
        {
            if (!isFlying)
            {
                return;
            }

            isFlying = false;
            releaseAction?.Invoke(this);
        }
    }
}
