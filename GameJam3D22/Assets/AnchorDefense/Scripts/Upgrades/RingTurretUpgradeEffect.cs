using UnityEngine;

namespace AnchorDefense
{
    [CreateAssetMenu(menuName = "Anchor Defense/Upgrades/Ring Turret Effect", fileName = "RingTurretEffect")]
    public sealed class RingTurretUpgradeEffect : UpgradeEffect
    {
        [field: SerializeField] public OrbitRingId RingId { get; private set; }
        [field: SerializeField, Min(1)] public int TurretCount { get; private set; } = 1;

        public override void Apply(UpgradeContext context)
        {
            context?.FindRing(RingId)?.UnlockTurrets(TurretCount);
        }

#if UNITY_EDITOR
        public void Configure(OrbitRingId ringId, int turretCount)
        {
            RingId = ringId;
            TurretCount = Mathf.Max(1, turretCount);
        }
#endif
    }
}
