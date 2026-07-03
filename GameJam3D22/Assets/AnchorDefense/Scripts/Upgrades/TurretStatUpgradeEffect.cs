using UnityEngine;

namespace AnchorDefense
{
    [CreateAssetMenu(menuName = "Anchor Defense/Upgrades/Turret Stat Effect", fileName = "TurretStatEffect")]
    public sealed class TurretStatUpgradeEffect : UpgradeEffect
    {
        [field: SerializeField] public TurretRuntimeStat Stat { get; private set; }
        [field: SerializeField, Min(0.01f)] public float Multiplier { get; private set; } = 1.1f;

        public override void Apply(UpgradeContext context)
        {
            context?.TurretStats?.Multiply(Stat, Multiplier);
        }

#if UNITY_EDITOR
        public void Configure(TurretRuntimeStat stat, float multiplier)
        {
            Stat = stat;
            Multiplier = multiplier;
        }
#endif
    }
}
