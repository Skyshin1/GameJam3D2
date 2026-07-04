using UnityEngine;

namespace AnchorDefense
{
    public enum EnemyAttackMode
    {
        ContactCore,
        RangedTurret
    }

    [CreateAssetMenu(menuName = "Anchor Defense/Enemy Config", fileName = "EnemyConfig")]
    public sealed class EnemyConfig : ScriptableObject
    {
        [field: Header("Prefab")]
        [field: SerializeField] public EnemyController Prefab { get; private set; }

        [field: Header("Stats")]
        [field: SerializeField, Min(0.1f)] public float MaxHealth { get; private set; } = 24f;
        [field: SerializeField, Min(0.1f)] public float MoveSpeed { get; private set; } = 1.8f;
        [field: SerializeField, Min(0f)] public float CoreDamage { get; private set; } = 10f;

        [field: Header("Combat Behaviour")]
        [field: SerializeField] public EnemyAttackMode AttackMode { get; private set; } = EnemyAttackMode.ContactCore;
        [field: SerializeField, Min(1f)] public float RangedStopRadius { get; private set; } = 10.5f;
        [field: SerializeField, Min(0.05f)] public float FireInterval { get; private set; } = 2.2f;
        [field: SerializeField, Min(0f)] public float ProjectileDamage { get; private set; } = 18f;
        [field: SerializeField, Min(0.1f)] public float ProjectileSpeed { get; private set; } = 8f;
        [field: SerializeField, Min(0.1f)] public float ProjectileLifetime { get; private set; } = 4f;
        [field: SerializeField, Min(0.01f)] public float ProjectileHitRadius { get; private set; } = 0.18f;
        [field: SerializeField] public EnemyProjectileController ProjectilePrefab { get; private set; }
        [field: SerializeField] public Color ProjectileColor { get; private set; } = new Color(1f, 0.22f, 0.08f);

        [field: Header("Presentation")]
        [field: SerializeField, Min(0.05f)] public float Size { get; private set; } = 0.55f;
        [field: SerializeField] public Color BaseColor { get; private set; } = new Color(0.92f, 0.16f, 0.25f);
        [field: SerializeField] public Color HitColor { get; private set; } = Color.white;
        [field: SerializeField, Min(0.01f)] public float HitFlashDuration { get; private set; } = 0.1f;
        [field: SerializeField] public PooledParticleEffect HitEffectPrefab { get; private set; }
        [field: SerializeField] public PooledParticleEffect DeathEffectPrefab { get; private set; }
    }
}
