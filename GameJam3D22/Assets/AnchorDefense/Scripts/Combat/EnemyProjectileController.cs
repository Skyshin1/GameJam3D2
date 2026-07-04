using System;
using UnityEngine;

namespace AnchorDefense
{
    public sealed class EnemyProjectileController : MonoBehaviour, IPoolable
    {
        [SerializeField] private TrailRenderer trail;

        private TrailRenderer[] trails;
        private ParticleSystem[] particleSystems;

        private TurretRegistry turretRegistry;
        private Action<EnemyProjectileController> releaseAction;
        private Vector3 direction;
        private float damage;
        private float speed;
        private float hitRadius;
        private float lifetime;
        private bool isFlying;

        private void Awake()
        {
            trails = GetComponentsInChildren<TrailRenderer>(true);
            particleSystems = GetComponentsInChildren<ParticleSystem>(true);

            if (trail == null && trails != null && trails.Length > 0)
            {
                trail = trails[0];
            }
        }

        public void Configure(TrailRenderer projectileTrail)
        {
            trail = projectileTrail;
            trails = GetComponentsInChildren<TrailRenderer>(true);
        }

        public void Launch(
            Vector3 position,
            Vector3 flightDirection,
            float projectileDamage,
            float projectileSpeed,
            float projectileHitRadius,
            float projectileLifetime,
            TurretRegistry targets,
            Action<EnemyProjectileController> onRelease)
        {
            isFlying = false;

            StopAndClearVfx();

            transform.position = position;

            direction = flightDirection.sqrMagnitude > 0.001f
                ? flightDirection.normalized
                : transform.forward;

            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            damage = projectileDamage;
            speed = projectileSpeed;
            hitRadius = projectileHitRadius;
            lifetime = projectileLifetime;
            turretRegistry = targets;
            releaseAction = onRelease;

            ResetAndPlayVfx();

            isFlying = true;
        }

        public void OnTakenFromPool()
        {
            isFlying = false;
            StopAndClearVfx();
        }

        public void OnReturnedToPool()
        {
            isFlying = false;
            turretRegistry = null;
            releaseAction = null;
            StopAndClearVfx();
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

            Vector3 nextPosition = transform.position + direction * (speed * Time.deltaTime);
            TurretHealth hit = FindHitTurret(transform.position, nextPosition);

            if (hit != null)
            {
                Vector3 hitPoint = hit.transform.position;
                hit.TakeDamage(new DamageInfo(damage, hitPoint, gameObject));
                transform.position = hitPoint;
                Release();
                return;
            }

            transform.position = nextPosition;
        }

        private TurretHealth FindHitTurret(Vector3 start, Vector3 end)
        {
            if (turretRegistry == null)
            {
                return null;
            }

            TurretHealth closestHit = null;
            float closestAlongPath = float.PositiveInfinity;

            Vector3 segment = end - start;
            float segmentLengthSqr = segment.sqrMagnitude;

            var turrets = turretRegistry.Turrets;
            for (int i = 0; i < turrets.Count; i++)
            {
                TurretHealth turret = turrets[i];
                if (turret == null || !turret.gameObject.activeInHierarchy || !turret.IsAlive)
                {
                    continue;
                }

                Collider targetCollider = turret.GetComponent<Collider>();

                float targetRadius = targetCollider != null
                    ? Mathf.Max(
                        targetCollider.bounds.extents.x,
                        targetCollider.bounds.extents.y,
                        targetCollider.bounds.extents.z)
                    : 0.5f;

                float along = segmentLengthSqr > 0.0001f
                    ? Mathf.Clamp01(Vector3.Dot(turret.transform.position - start, segment) / segmentLengthSqr)
                    : 0f;

                Vector3 closestPoint = start + segment * along;
                float combinedRadius = hitRadius + targetRadius;

                if ((turret.transform.position - closestPoint).sqrMagnitude <= combinedRadius * combinedRadius &&
                    along < closestAlongPath)
                {
                    closestAlongPath = along;
                    closestHit = turret;
                }
            }

            return closestHit;
        }

        private void Release()
        {
            if (!isFlying)
            {
                return;
            }

            isFlying = false;
            StopAndClearVfx();
            releaseAction?.Invoke(this);
        }

        private void StopAndClearVfx()
        {
            if (trails != null)
            {
                foreach (TrailRenderer t in trails)
                {
                    if (t == null)
                    {
                        continue;
                    }

                    t.emitting = false;
                    t.Clear();
                }
            }

            if (particleSystems != null)
            {
                foreach (ParticleSystem ps in particleSystems)
                {
                    if (ps == null)
                    {
                        continue;
                    }

                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.Clear(true);
                }
            }
        }

        private void ResetAndPlayVfx()
        {
            if (trails != null)
            {
                foreach (TrailRenderer t in trails)
                {
                    if (t == null)
                    {
                        continue;
                    }

                    t.Clear();
                    t.emitting = true;
                }
            }

            if (particleSystems != null)
            {
                foreach (ParticleSystem ps in particleSystems)
                {
                    if (ps == null)
                    {
                        continue;
                    }

                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.Clear(true);
                    ps.Play(true);
                }
            }
        }
    }
}