using System;
using UnityEngine;

namespace AnchorDefense
{
    public sealed class TurretHealth : MonoBehaviour, IDamageable
    {
        private TurretRuntimeStats runtimeStats;
        private Action<Vector3> hitFeedback;
        private float disabledRemaining;
        private float zoneMaxHealthMultiplier = 1f;

        public float CurrentHealth { get; private set; }
        public float MaxHealth { get; private set; }
        public bool IsAlive { get; private set; }
        public float DisabledRemaining => disabledRemaining;

        public event Action<float, float> HealthChanged;
        public event Action Died;
        public event Action Recovered;

        public void Initialize(TurretRuntimeStats stats, Action<Vector3> onHitFeedback = null)
        {
            if (runtimeStats != null)
            {
                runtimeStats.Changed -= HandleStatsChanged;
            }

            runtimeStats = stats;
            hitFeedback = onHitFeedback;
            zoneMaxHealthMultiplier = 1f;
            MaxHealth = GetEffectiveMaxHealth();
            CurrentHealth = MaxHealth;
            IsAlive = true;
            disabledRemaining = 0f;
            if (runtimeStats != null)
            {
                runtimeStats.Changed += HandleStatsChanged;
            }
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (!IsAlive || damage.Amount <= 0f)
            {
                return;
            }

            float damageMultiplier = runtimeStats != null ? runtimeStats.DamageTakenMultiplier : 1f;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage.Amount * damageMultiplier);
            hitFeedback?.Invoke(damage.HitPoint);
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
            if (CurrentHealth > 0f)
            {
                return;
            }

            IsAlive = false;
            disabledRemaining = runtimeStats != null ? runtimeStats.DisableDuration : 10f;
            Died?.Invoke();
        }

        private void Update()
        {
            if (IsAlive || disabledRemaining <= 0f)
            {
                return;
            }

            disabledRemaining -= Time.deltaTime;
            if (disabledRemaining > 0f)
            {
                return;
            }

            disabledRemaining = 0f;
            CurrentHealth = MaxHealth;
            IsAlive = true;
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
            Recovered?.Invoke();
        }

        public void SetZoneMaxHealthMultiplier(float multiplier)
        {
            float clamped = Mathf.Clamp(multiplier, 1f, 10f);
            if (Mathf.Approximately(zoneMaxHealthMultiplier, clamped)) return;

            float healthRatio = MaxHealth > 0f ? CurrentHealth / MaxHealth : 1f;
            zoneMaxHealthMultiplier = clamped;
            MaxHealth = GetEffectiveMaxHealth();
            if (IsAlive) CurrentHealth = Mathf.Clamp(MaxHealth * healthRatio, 0f, MaxHealth);
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f || CurrentHealth >= MaxHealth) return;
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        private void OnDestroy()
        {
            if (runtimeStats != null)
            {
                runtimeStats.Changed -= HandleStatsChanged;
            }
        }

        private void HandleStatsChanged()
        {
            float healthRatio = MaxHealth > 0f ? CurrentHealth / MaxHealth : 1f;
            MaxHealth = GetEffectiveMaxHealth();
            if (IsAlive)
            {
                CurrentHealth = Mathf.Clamp(MaxHealth * healthRatio, 0f, MaxHealth);
            }
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        private float GetEffectiveMaxHealth()
        {
            float baseMaximum = runtimeStats != null ? runtimeStats.MaxHealth : 1f;
            return Mathf.Max(1f, baseMaximum * zoneMaxHealthMultiplier);
        }
    }
}
