using System;
using UnityEngine;

namespace AnchorDefense
{
    public enum TurretRuntimeStat
    {
        Damage,
        FireInterval,
        MaxHealth
    }

    public sealed class TurretRuntimeStats
    {
        private readonly TurretConfig baseConfig;
        private float damageMultiplier = 1f;
        private float fireIntervalMultiplier = 1f;
        private float maxHealthMultiplier = 1f;

        public TurretRuntimeStats(TurretConfig config)
        {
            baseConfig = config != null ? config : throw new ArgumentNullException(nameof(config));
        }

        public float Range => baseConfig.Range;
        public float Damage => baseConfig.Damage * damageMultiplier;
        public float FireInterval => Mathf.Max(0.03f, baseConfig.FireInterval * fireIntervalMultiplier);
        public float MaxHealth => Mathf.Max(1f, baseConfig.MaxHealth * maxHealthMultiplier);
        public float DisableDuration => Mathf.Max(0.1f, baseConfig.DisableDuration);

        public event Action Changed;

        public void Multiply(TurretRuntimeStat stat, float multiplier)
        {
            if (multiplier <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(multiplier), "Upgrade multipliers must be greater than zero.");
            }

            switch (stat)
            {
                case TurretRuntimeStat.Damage:
                    damageMultiplier *= multiplier;
                    break;
                case TurretRuntimeStat.FireInterval:
                    fireIntervalMultiplier *= multiplier;
                    break;
                case TurretRuntimeStat.MaxHealth:
                    maxHealthMultiplier *= multiplier;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stat), stat, null);
            }

            Changed?.Invoke();
        }
    }
}
