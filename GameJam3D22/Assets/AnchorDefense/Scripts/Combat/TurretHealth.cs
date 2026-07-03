using System;
using UnityEngine;

namespace AnchorDefense
{
    public sealed class TurretHealth : MonoBehaviour, IDamageable
    {
        private TurretRuntimeStats runtimeStats;

        public float CurrentHealth { get; private set; }
        public float MaxHealth { get; private set; }
        public bool IsAlive { get; private set; }

        public event Action<float, float> HealthChanged;
        public event Action Died;

        public void Initialize(TurretRuntimeStats stats)
        {
            if (runtimeStats != null)
            {
                runtimeStats.Changed -= HandleStatsChanged;
            }

            runtimeStats = stats;
            MaxHealth = runtimeStats != null ? runtimeStats.MaxHealth : 1f;
            CurrentHealth = MaxHealth;
            IsAlive = true;
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
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
            if (CurrentHealth > 0f)
            {
                return;
            }

            IsAlive = false;
            Died?.Invoke();
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
