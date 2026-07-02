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
        private float currentHealth;
        private float moveSpeed;
        private float flashRemaining;
        private bool isAlive;

        public bool IsAlive => isAlive;
        public int SpawnVersion { get; private set; }

        public void Initialize(
            EnemyConfig enemyConfig,
            CoreHealth targetCore,
            float healthMultiplier,
            float speedMultiplier,
            Action<EnemyController> onRelease,
            Action<Vector3, Color> onHitEffect,
            Action<Vector3, Color> onDeathEffect)
        {
            config = enemyConfig;
            core = targetCore;
            releaseAction = onRelease;
            hitEffectAction = onHitEffect;
            deathEffectAction = onDeathEffect;
            cachedRenderer = cachedRenderer != null ? cachedRenderer : GetComponentInChildren<Renderer>();
            propertyBlock = propertyBlock ?? new MaterialPropertyBlock();
            currentHealth = config.MaxHealth * healthMultiplier;
            moveSpeed = config.MoveSpeed * speedMultiplier;
            SpawnVersion++;
            isAlive = true;
            flashRemaining = 0f;
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

            if (distance > 0.001f)
            {
                Vector3 direction = toCore / distance;
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
