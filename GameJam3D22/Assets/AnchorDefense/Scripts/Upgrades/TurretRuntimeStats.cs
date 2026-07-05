using System;
using UnityEngine;

namespace AnchorDefense
{
    public enum TurretRuntimeStat
    {
        Damage,
        FireInterval,
        MaxHealth,
        Range,
        ProjectileSpeed,
        ProjectileHitRadius,
        DisableDuration,
        DamageTaken
    }

    public sealed class TurretRuntimeStats
    {
        private readonly TurretConfig baseConfig;
        private float damageMultiplier = 1f;
        private float fireIntervalMultiplier = 1f;
        private float maxHealthMultiplier = 1f;
        private float rangeMultiplier = 1f;
        private float projectileSpeedMultiplier = 1f;
        private float projectileHitRadiusMultiplier = 1f;
        private float disableDurationMultiplier = 1f;
        private float damageTakenMultiplier = 1f;

        public TurretRuntimeStats(TurretConfig config)
        {
            baseConfig = config != null ? config : throw new ArgumentNullException(nameof(config));
        }

        public float Range => baseConfig.Range * rangeMultiplier;
        public float Damage => baseConfig.Damage * damageMultiplier;
        public float FireInterval => Mathf.Max(0.03f, baseConfig.FireInterval * fireIntervalMultiplier);
        public float MaxHealth => Mathf.Max(1f, baseConfig.MaxHealth * maxHealthMultiplier);
        public float DisableDuration => Mathf.Max(0.1f, baseConfig.DisableDuration * disableDurationMultiplier);
        public float ProjectileSpeedMultiplier => projectileSpeedMultiplier;
        public float ProjectileHitRadiusMultiplier => projectileHitRadiusMultiplier;
        public float DamageTakenMultiplier => damageTakenMultiplier;

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
                case TurretRuntimeStat.Range:
                    rangeMultiplier *= multiplier;
                    break;
                case TurretRuntimeStat.ProjectileSpeed:
                    projectileSpeedMultiplier *= multiplier;
                    break;
                case TurretRuntimeStat.ProjectileHitRadius:
                    projectileHitRadiusMultiplier *= multiplier;
                    break;
                case TurretRuntimeStat.DisableDuration:
                    disableDurationMultiplier *= multiplier;
                    break;
                case TurretRuntimeStat.DamageTaken:
                    damageTakenMultiplier *= multiplier;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stat), stat, null);
            }

            Changed?.Invoke();
        }
    }
}
