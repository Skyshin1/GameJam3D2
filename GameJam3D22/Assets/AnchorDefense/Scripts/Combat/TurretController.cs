using UnityEngine;

namespace AnchorDefense
{
    public sealed class TurretController : MonoBehaviour
    {
        [SerializeField] private Transform firePoint;
        [SerializeField] private DirectionalSpriteRenderer directionalVisual;
        [SerializeField] private TurretHealth health;
        [SerializeField] private TurretProjectileType projectileType = TurretProjectileType.A;
        [SerializeField] private AudioSource fireAudioSource;
        [SerializeField] private AudioClip fireClip;
        [SerializeField, Range(0f, 1f)] private float fireVolume = 0.4f;
        [SerializeField, Min(0.01f)] private float fireSoundWindowSeconds = 0.2f;
        [SerializeField, Min(1)] private int maxFireSoundsPerWindow = 3;
        [SerializeField, Range(0f, 1f)] private float fireSoundRepeatVolumeMultiplier = 0.6f;
[SerializeField, Min(1)] private int maxFireSoundsPerWindow = 3;

        private static float fireSoundWindowStart = float.NegativeInfinity;
        private static int fireSoundsInWindow;
        private TurretRuntimeStats runtimeStats;
        private EnemyRegistry registry;
        private ProjectileService projectileService;
        private EnemyController currentTarget;
        private float cooldown;
        private float zoneFireIntervalMultiplier = 1f;
        private float zoneDamageMultiplier = 1f;

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
        public TurretProjectileType ProjectileType => projectileType;
        public float ZoneFireIntervalMultiplier => zoneFireIntervalMultiplier;
        public float ZoneDamageMultiplier => zoneDamageMultiplier;

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

        public void ConfigureProjectileType(TurretProjectileType type)
        {
            projectileType = type == TurretProjectileType.Fused ? TurretProjectileType.A : type;
        }

public void ConfigureFireAudio(AudioSource source, AudioClip clip, float volume = 0.4f, float windowSeconds = 0.2f, int maxSoundsPerWindow = 3, float repeatVolumeMultiplier = 0.6f)
        {
            fireAudioSource = source;
            fireClip = clip;
            fireVolume = Mathf.Clamp01(volume);
            fireSoundWindowSeconds = Mathf.Max(0.01f, windowSeconds);
            maxFireSoundsPerWindow = Mathf.Max(1, maxSoundsPerWindow);
            fireSoundRepeatVolumeMultiplier = Mathf.Clamp01(repeatVolumeMultiplier);
        }


        public void SetZoneFireIntervalMultiplier(float multiplier)
        {
            float clamped = Mathf.Clamp(multiplier, 0.05f, 4f);
            if (Mathf.Approximately(zoneFireIntervalMultiplier, clamped))
            {
                return;
            }
            zoneFireIntervalMultiplier = clamped;
            if (runtimeStats != null)
            {
                cooldown = Mathf.Min(cooldown, runtimeStats.FireInterval * zoneFireIntervalMultiplier);
            }
        }

        public void SetZoneDamageMultiplier(float multiplier)
        {
            zoneDamageMultiplier = Mathf.Clamp(multiplier, 1f, 10f);
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
                ApplyPlanarVisualAim(direction);
            }

            if (cooldown <= 0f)
            {
                projectileService.Fire(firePoint.position, currentTarget,
                    runtimeStats.Damage * zoneDamageMultiplier, projectileType);
                PlayFireSound();
                cooldown = runtimeStats.FireInterval * zoneFireIntervalMultiplier;
            }
        }

        public void ApplyPlanarVisualAim(Vector3 worldDirection)
        {
            Vector3 ringNormal = transform.parent != null ? transform.parent.up : Vector3.up;
            Vector3 planarDirection = Vector3.ProjectOnPlane(worldDirection, ringNormal);
            if (planarDirection.sqrMagnitude <= 0.001f)
            {
                return;
            }

            planarDirection.Normalize();
            directionalVisual?.SetWorldDirection(planarDirection);
            transform.rotation = Quaternion.LookRotation(planarDirection, ringNormal);
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
    

public static void ResetFireSoundLimiter()
        {
            fireSoundWindowStart = float.NegativeInfinity;
            fireSoundsInWindow = 0;
        }


public static bool ShouldPlayFireSound(float realtime, float windowSeconds, int maxSoundsPerWindow)
        {
            return TryGetFireSoundVolumeScale(realtime, windowSeconds, maxSoundsPerWindow, 1f, out _);
        }

public static bool TryGetFireSoundVolumeScale(float realtime, float windowSeconds, int maxSoundsPerWindow, float repeatVolumeMultiplier, out float volumeScale)
        {
            float safeWindow = Mathf.Max(0.01f, windowSeconds);
            int safeMax = Mathf.Max(1, maxSoundsPerWindow);
            float safeMultiplier = Mathf.Clamp01(repeatVolumeMultiplier);
            if (realtime - fireSoundWindowStart >= safeWindow)
            {
                fireSoundWindowStart = realtime;
                fireSoundsInWindow = 0;
            }
            if (fireSoundsInWindow >= safeMax)
            {
                volumeScale = 0f;
                return false;
            }

            volumeScale = Mathf.Pow(safeMultiplier, fireSoundsInWindow);
            fireSoundsInWindow++;
            return true;
        }



private void PlayFireSound()
        {
            if (fireClip == null)
            {
                return;
            }
            if (fireAudioSource == null)
            {
                fireAudioSource = GetComponent<AudioSource>();
            }
            if (fireAudioSource == null || !TryGetFireSoundVolumeScale(Time.unscaledTime, fireSoundWindowSeconds, maxFireSoundsPerWindow, fireSoundRepeatVolumeMultiplier, out float volumeScale))
            {
                return;
            }

            fireAudioSource.pitch = Random.Range(0.96f, 1.04f);
            fireAudioSource.PlayOneShot(fireClip, fireVolume * volumeScale);
        }
}
}
