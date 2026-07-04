using UnityEngine;

namespace AnchorDefense
{
    public enum TurretProjectileType
    {
        A,
        B,
        Fused
    }

    [CreateAssetMenu(menuName = "Anchor Defense/Turret Config", fileName = "TurretConfig")]
    public sealed class TurretConfig : ScriptableObject
    {
        [field: Header("Weapon")]
        [field: SerializeField, Min(0.1f)] public float Range { get; private set; } = 9f;
        [field: SerializeField, Min(0f)] public float Damage { get; private set; } = 10f;
        [field: SerializeField, Min(0.03f)] public float FireInterval { get; private set; } = 0.72f;
        [field: SerializeField, Min(1f)] public float MaxHealth { get; private set; } = 100f;
        [field: SerializeField, Min(0.1f)] public float DisableDuration { get; private set; } = 10f;
        [field: SerializeField, Min(0.1f)] public float ProjectileSpeed { get; private set; } = 12f;
        [field: SerializeField, Min(0.1f)] public float ProjectileLifetime { get; private set; } = 3f;
        [field: SerializeField, Min(0.01f)] public float ProjectileHitRadius { get; private set; } = 0.25f;

        [field: Header("Presentation")]
        [field: SerializeField, Min(0.02f)] public float ProjectileScale { get; private set; } = 0.16f;
        [field: SerializeField] public Color TurretColor { get; private set; } = new Color(0.2f, 0.95f, 1f);
        [field: SerializeField] public Color ProjectileColor { get; private set; } = new Color(0.35f, 1f, 1f);
        [field: SerializeField] public ProjectileController ProjectilePrefab { get; private set; }
        [field: SerializeField] public PooledParticleEffect MuzzleEffectPrefab { get; private set; }
        [field: SerializeField] public PooledParticleEffect HitEffectPrefab { get; private set; }
        [field: SerializeField] public Color HitEffectColor { get; private set; } = new Color(1f, 0.35f, 0.08f);

        [field: Header("Projectile Fusion: A + B")]
        [field: SerializeField] public ProjectileController ProjectileBPrefab { get; private set; }
        [field: SerializeField] public ProjectileController FusedProjectilePrefab { get; private set; }
        [field: SerializeField, Min(0.05f)] public float FusionRadius { get; private set; } = 0.75f;
        [field: SerializeField, Min(1f)] public float FusionDamageMultiplier { get; private set; } = 1.35f;
        [field: SerializeField, Min(0.1f)] public float FusedSpeedMultiplier { get; private set; } = 1.1f;
        [field: SerializeField, Min(0.1f)] public float FusedHitRadiusMultiplier { get; private set; } = 1.5f;
        [field: SerializeField] public Color ProjectileBColor { get; private set; } = new Color(1f, 0.4f, 0.9f);
        [field: SerializeField] public Color FusedProjectileColor { get; private set; } = new Color(1f, 0.78f, 0.18f);
        [field: SerializeField] public PooledParticleEffect FusionEffectPrefab { get; private set; }
    }
}
