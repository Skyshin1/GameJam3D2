using System;
using UnityEngine;

namespace AnchorDefense
{
    public sealed class EnemyController : MonoBehaviour, IDamageable, IPoolable
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public const float DefaultAttackSoundVolume = 0.25f;

        private MaterialPropertyBlock propertyBlock;
        private EnemyConfig config;
        private CoreHealth core;
        private Renderer cachedRenderer;
        private Action<EnemyController> releaseAction;
        private Action<Vector3, Color> hitEffectAction;
        private Action<Vector3, Color> deathEffectAction;
        private TurretRegistry turretRegistry;
        private EnemyProjectileService enemyProjectiles;

        private float currentHealth;
        private float moveSpeed;
        private float flashRemaining;
        private bool isAlive;
        private float fireCooldown;
        private float zoneSpeedMultiplier = 1f;
        private float zoneDamagePerSecond;
        private float zoneDamageTakenMultiplier = 1f;

        private float orbitAngle;
        private float orbitBobPhase;
        private Vector3 orbitAxis = Vector3.up;
        private Vector3 orbitReferenceDirection = Vector3.forward;

        [SerializeField] private DirectionalSpriteRenderer directionalVisual;
        [SerializeField] private SingleSpriteBillboardVisual singleSpriteVisual;
        [SerializeField] private AudioSource attackAudioSource;
        [SerializeField] private AudioClip attackClip;
        [SerializeField, Range(0f, 1f)] private float attackVolume = DefaultAttackSoundVolume;
        [SerializeField, Min(0.01f)] private float attackSoundWindowSeconds = 0.18f;
        [SerializeField, Min(1)] private int maxAttackSoundsPerWindow = 2;

        private static float attackSoundWindowStart = float.NegativeInfinity;
        private static int attackSoundsInWindow;

        public bool IsAlive => isAlive;
        public int SpawnVersion { get; private set; }
        public float ZoneSpeedMultiplier => zoneSpeedMultiplier;
        public float ZoneDamagePerSecond => zoneDamagePerSecond;
        public float ZoneDamageTakenMultiplier => zoneDamageTakenMultiplier;

        public event Action<EnemyController> Killed;

        public void SetZoneEffect(float speedMultiplier, float damagePerSecond)
        {
            zoneSpeedMultiplier = Mathf.Clamp(speedMultiplier, 0.05f, 4f);
            zoneDamagePerSecond = Mathf.Max(0f, damagePerSecond);
        }

        public void SetZoneDamageTakenMultiplier(float multiplier)
        {
            zoneDamageTakenMultiplier = Mathf.Clamp(multiplier, 1f, 10f);
        }

        public void ConfigureDirectionalVisual(DirectionalSpriteRenderer visual)
        {
            directionalVisual = visual;
        }

        public void ConfigureSingleSpriteVisual(SingleSpriteBillboardVisual visual)
        {
            singleSpriteVisual = visual;
        }

        public void ConfigureAttackAudio(
            AudioSource source,
            AudioClip clip,
            float volume = DefaultAttackSoundVolume,
            float windowSeconds = 0.18f,
            int maxSoundsPerWindow = 2)
        {
            attackAudioSource = source;
            attackClip = clip;
            attackVolume = Mathf.Clamp01(volume);
            attackSoundWindowSeconds = Mathf.Max(0.01f, windowSeconds);
            maxAttackSoundsPerWindow = Mathf.Max(1, maxSoundsPerWindow);
        }

        public void Initialize(
            EnemyConfig enemyConfig,
            CoreHealth targetCore,
            float healthMultiplier,
            float speedMultiplier,
            Action<EnemyController> onRelease,
            Action<Vector3, Color> onHitEffect,
            Action<Vector3, Color> onDeathEffect,
            TurretRegistry turretTargets = null,
            EnemyProjectileService projectileService = null)
        {
            config = enemyConfig;
            core = targetCore;
            releaseAction = onRelease;
            hitEffectAction = onHitEffect;
            deathEffectAction = onDeathEffect;
            turretRegistry = turretTargets;
            enemyProjectiles = projectileService;

            cachedRenderer = cachedRenderer != null ? cachedRenderer : GetComponentInChildren<Renderer>();
            propertyBlock = propertyBlock ?? new MaterialPropertyBlock();

            currentHealth = config.MaxHealth * healthMultiplier;
            moveSpeed = config.MoveSpeed * speedMultiplier;
            SpawnVersion++;
            isAlive = true;
            flashRemaining = 0f;

            fireCooldown = UnityEngine.Random.Range(
                0.15f,
                Mathf.Max(0.15f, config.FireInterval));

            zoneSpeedMultiplier = 1f;
            zoneDamagePerSecond = 0f;
            zoneDamageTakenMultiplier = 1f;

            transform.localScale = Vector3.one * config.Size;
            SetColor(config.BaseColor);

            InitializeOrbitBossState();
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (!isAlive || damage.Amount <= 0f)
            {
                return;
            }

            currentHealth -= damage.Amount * zoneDamageTakenMultiplier;
            hitEffectAction?.Invoke(damage.HitPoint, config.HitColor);

            if (currentHealth <= 0f)
            {
                Die();
                return;
            }

            flashRemaining = config.HitFlashDuration;
            SetColor(config.HitColor);
        }

        public void OnTakenFromPool()
        {
            isAlive = true;
        }

        public void OnReturnedToPool()
        {
            isAlive = false;
            releaseAction = null;
            hitEffectAction = null;
            deathEffectAction = null;
            turretRegistry = null;
            enemyProjectiles = null;
            zoneSpeedMultiplier = 1f;
            zoneDamagePerSecond = 0f;
            zoneDamageTakenMultiplier = 1f;
        }

        private void Update()
        {
            if (!isAlive || core == null || config == null)
            {
                return;
            }

            if (zoneDamagePerSecond > 0f)
            {
                currentHealth -= zoneDamagePerSecond * Time.deltaTime;
                if (currentHealth <= 0f)
                {
                    Die();
                    return;
                }
            }

            if (config.AttackMode == EnemyAttackMode.OrbitTurretBoss)
            {
                UpdateOrbitBossCombat();
                UpdateHitFlash();
                return;
            }

            Vector3 toCore = core.transform.position - transform.position;
            float distance = toCore.magnitude;

            if (distance <= core.Radius)
            {
                ReachCore();
                return;
            }

            if (config.AttackMode == EnemyAttackMode.RangedTurret)
            {
                UpdateRangedCombat(toCore, distance);
            }
            else if (distance > 0.001f)
            {
                Vector3 direction = toCore / distance;

                directionalVisual?.SetWorldDirection(direction);
                singleSpriteVisual?.SetWorldDirection(direction);

                transform.position += direction * (moveSpeed * zoneSpeedMultiplier * Time.deltaTime);
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }

            UpdateHitFlash();
        }

        private void InitializeOrbitBossState()
        {
            if (config == null || config.AttackMode != EnemyAttackMode.OrbitTurretBoss || core == null)
            {
                return;
            }

            orbitAxis = config.OrbitAxis.sqrMagnitude > 0.001f
                ? config.OrbitAxis.normalized
                : Vector3.up;

            orbitReferenceDirection = Vector3.ProjectOnPlane(Vector3.forward, orbitAxis);

            if (orbitReferenceDirection.sqrMagnitude <= 0.001f)
            {
                orbitReferenceDirection = Vector3.ProjectOnPlane(Vector3.right, orbitAxis);
            }

            orbitReferenceDirection.Normalize();

            Vector3 fromCore = transform.position - core.transform.position;
            Vector3 planar = Vector3.ProjectOnPlane(fromCore, orbitAxis);

            if (planar.sqrMagnitude <= 0.001f)
            {
                orbitAngle = UnityEngine.Random.Range(0f, 360f);
            }
            else
            {
                orbitAngle = Vector3.SignedAngle(
                    orbitReferenceDirection,
                    planar.normalized,
                    orbitAxis);
            }

            orbitBobPhase = config.RandomizeOrbitBobPhase
                ? UnityEngine.Random.Range(0f, Mathf.PI * 2f)
                : 0f;
        }

        private void UpdateOrbitBossCombat()
        {
            orbitAxis = orbitAxis.sqrMagnitude > 0.001f
                ? orbitAxis.normalized
                : Vector3.up;

            orbitAngle += config.OrbitAngularSpeed * zoneSpeedMultiplier * Time.deltaTime;

            orbitBobPhase +=
                config.OrbitBobFrequency *
                Mathf.PI *
                2f *
                zoneSpeedMultiplier *
                Time.deltaTime;

            Quaternion orbitRotation = Quaternion.AngleAxis(orbitAngle, orbitAxis);
            Vector3 radialDirection = orbitRotation * orbitReferenceDirection;

            float bobOffset = Mathf.Sin(orbitBobPhase) * config.OrbitBobAmplitude;

            Vector3 targetOrbitPosition =
                core.transform.position +
                radialDirection * config.OrbitRadius +
                orbitAxis * (config.OrbitHeight + bobOffset);

            float smoothness = Mathf.Max(0.1f, config.OrbitPositionSmoothness);
            float lerpFactor = 1f - Mathf.Exp(-smoothness * Time.deltaTime);

            transform.position = Vector3.Lerp(
                transform.position,
                targetOrbitPosition,
                lerpFactor);

            TurretHealth target = turretRegistry?.FindNearestOperational(transform.position);

            Vector3 fireDirection;

            if (target != null)
            {
                Vector3 toTurret = target.transform.position - transform.position;
                fireDirection = toTurret.sqrMagnitude > 0.001f
                    ? toTurret.normalized
                    : transform.forward;
            }
            else
            {
                fireDirection = Vector3.Cross(orbitAxis, radialDirection);

                if (fireDirection.sqrMagnitude <= 0.001f)
                {
                    fireDirection = transform.forward;
                }
                else
                {
                    fireDirection.Normalize();
                }
            }

            directionalVisual?.SetWorldDirection(fireDirection);
            singleSpriteVisual?.SetWorldDirection(fireDirection);

            if (fireDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(fireDirection, orbitAxis);
            }

            if (target == null)
            {
                return;
            }

            fireCooldown -= Time.deltaTime;
            if (fireCooldown <= 0f)
            {
                float fireOffset = Mathf.Max(0f, config.OrbitProjectileFireOffset);
                enemyProjectiles?.Fire(transform.position + fireDirection * fireOffset, fireDirection);
                PlayAttackSound();
                fireCooldown = config.FireInterval;
            }
        }

        private void UpdateRangedCombat(Vector3 toCore, float distance)
        {
            Vector3 coreDirection = distance > 0.001f ? toCore / distance : transform.forward;

            if (distance > config.RangedStopRadius)
            {
                transform.position += coreDirection * (moveSpeed * zoneSpeedMultiplier * Time.deltaTime);
            }

            TurretHealth target = turretRegistry?.FindNearestOperational(transform.position);

            Vector3 fireDirection = target != null
                ? (target.transform.position - transform.position).normalized
                : coreDirection;

            directionalVisual?.SetWorldDirection(fireDirection);
            singleSpriteVisual?.SetWorldDirection(fireDirection);

            if (fireDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(fireDirection, Vector3.up);
            }

            fireCooldown -= Time.deltaTime;
            if (fireCooldown <= 0f)
            {
                enemyProjectiles?.Fire(transform.position + fireDirection * 0.65f, fireDirection);
                PlayAttackSound();
                fireCooldown = config.FireInterval;
            }
        }

        private void UpdateHitFlash()
        {
            if (flashRemaining <= 0f)
            {
                return;
            }

            flashRemaining -= Time.deltaTime;

            if (flashRemaining <= 0f)
            {
                SetColor(config.BaseColor);
            }
        }

        private void ReachCore()
        {
            if (!isAlive)
            {
                return;
            }

            isAlive = false;
            core.ApplyDamage(config.CoreDamage);
            PlayAttackSound();
            releaseAction?.Invoke(this);
        }

        private void Die()
        {
            if (!isAlive)
            {
                return;
            }

            isAlive = false;
            deathEffectAction?.Invoke(transform.position, config.BaseColor);
            Killed?.Invoke(this);
            releaseAction?.Invoke(this);
        }

        private void SetColor(Color color)
        {
            if (cachedRenderer == null)
            {
                return;
            }

            cachedRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, color);
            propertyBlock.SetColor(ColorId, color);
            cachedRenderer.SetPropertyBlock(propertyBlock);
        }

        public static void ResetAttackSoundLimiter()
        {
            attackSoundWindowStart = float.NegativeInfinity;
            attackSoundsInWindow = 0;
        }

        public static bool ShouldPlayAttackSound(float realtime, float windowSeconds, int maxSoundsPerWindow)
        {
            float safeWindow = Mathf.Max(0.01f, windowSeconds);
            int safeMax = Mathf.Max(1, maxSoundsPerWindow);

            if (realtime - attackSoundWindowStart >= safeWindow)
            {
                attackSoundWindowStart = realtime;
                attackSoundsInWindow = 0;
            }

            if (attackSoundsInWindow >= safeMax)
            {
                return false;
            }

            attackSoundsInWindow++;
            return true;
        }

        private void PlayAttackSound()
        {
            if (attackClip == null)
            {
                return;
            }

            if (attackAudioSource == null)
            {
                attackAudioSource = GetComponent<AudioSource>();
            }

            if (attackAudioSource == null ||
                !ShouldPlayAttackSound(Time.unscaledTime, attackSoundWindowSeconds, maxAttackSoundsPerWindow))
            {
                return;
            }

            attackAudioSource.pitch = UnityEngine.Random.Range(0.94f, 1.02f);
            attackAudioSource.PlayOneShot(attackClip, attackVolume);
        }
    }
}
