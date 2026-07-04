using System;
using UnityEngine;

namespace AnchorDefense
{
    public sealed class TurretHealth : MonoBehaviour, IDamageable
    {
        private TurretRuntimeStats runtimeStats;
        private Action<Vector3> hitFeedback;
        private float disabledRemaining;

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
            MaxHealth = runtimeStats != null ? runtimeStats.MaxHealth : 1f;
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

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage.Amount);
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

        private void OnDestroy()
        {
            if (runtimeStats != null)
            {
                runtimeStats.Changed -= HandleStatsChanged;
            }
        }

        private void HandleStatsChanged()
        {
            float previousMaximum = MaxHealth;
            MaxHealth = runtimeStats.MaxHealth;
            if (IsAlive)
            {
                CurrentHealth = Mathf.Clamp(CurrentHealth + MaxHealth - previousMaximum, 0f, MaxHealth);
            }
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }
    }
}
