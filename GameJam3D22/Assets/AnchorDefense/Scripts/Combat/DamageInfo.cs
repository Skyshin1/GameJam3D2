using UnityEngine;

namespace AnchorDefense
{
    public readonly struct DamageInfo
    {
        public DamageInfo(float amount, Vector3 hitPoint, GameObject source)
        {
            Amount = amount;
            HitPoint = hitPoint;
            Source = source;
        }

        public float Amount { get; }
        public Vector3 HitPoint { get; }
        public GameObject Source { get; }
    }

    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(DamageInfo damage);
    }
}
