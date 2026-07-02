using System;
using UnityEngine;

namespace AnchorDefense
{
    public sealed class CoreHealth : MonoBehaviour
    {
        public float CurrentHealth { get; private set; }
        public float MaxHealth { get; private set; }
        public float Radius { get; private set; }
        public bool IsDead => CurrentHealth <= 0f;

        public event Action<float, float> HealthChanged;
        public event Action Died;

        public void Initialize(CoreConfig config)
        {
            MaxHealth = config.MaxHealth;
            CurrentHealth = MaxHealth;
            Radius = config.Radius;
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void ApplyDamage(float amount)
        {
            if (IsDead || amount <= 0f)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            HealthChanged?.Invoke(CurrentHealth, MaxHealth);

            if (IsDead)
            {
                Died?.Invoke();
            }
        }
    }
}
