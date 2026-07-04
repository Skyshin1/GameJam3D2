using System;
using UnityEngine;

namespace AnchorDefense
{
    public sealed class EnemyController : MonoBehaviour, IDamageable, IPoolable
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

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
        [SerializeField] private DirectionalSpriteRenderer directionalVisual;
        [SerializeField] private SingleSpriteBillboardVisual singleSpriteVisual;

        public bool IsAlive => isAlive;
        public int SpawnVersion { get; private set; }
        public event Action<EnemyController> Killed;

        public void ConfigureDirectionalVisual(DirectionalSpriteRenderer visual)
        {
            directionalVisual = visual;
        }

        public void ConfigureSingleSpriteVisual(SingleSpriteBillboardVisual visual)
        {
            singleSpriteVisual = visual;
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
            fireCooldown = UnityEngine.Random.Range(0.15f, Mathf.Max(0.15f, config.FireInterval));
            transform.localScale = Vector3.one * config.Size;
            SetColor(config.BaseColor);
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (!isAlive || damage.Amount <= 0f)
            {
                return;
            }

            currentHealth -= damage.Amount;
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
        }

        private void Update()
        {
            if (!isAlive || core == null)
            {
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
                transform.position += direction * (moveSpeed * Time.deltaTime);
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }

            if (flashRemaining > 0f)
            {
                flashRemaining -= Time.deltaTime;
                if (flashRemaining <= 0f)
                {
                    SetColor(config.BaseColor);
                }
            }
        }

        private void UpdateRangedCombat(Vector3 toCore, float distance)
        {
            Vector3 coreDirection = distance > 0.001f ? toCore / distance : transform.forward;
            if (distance > config.RangedStopRadius)
            {
                transform.position += coreDirection * (moveSpeed * Time.deltaTime);
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
                fireCooldown = config.FireInterval;
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
    }
}
