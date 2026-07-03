using System.Collections.Generic;
using UnityEngine;

namespace AnchorDefense
{
    public sealed class UpgradeContext
    {
        private readonly IReadOnlyList<OrbitRingController> rings;

        public UpgradeContext(TurretRuntimeStats turretStats, IReadOnlyList<OrbitRingController> orbitRings)
        {
            TurretStats = turretStats;
            rings = orbitRings;
        }

        public TurretRuntimeStats TurretStats { get; }

        public OrbitRingController FindRing(OrbitRingId ringId)
        {
            if (rings == null)
            {
                return null;
            }

            for (int i = 0; i < rings.Count; i++)
            {
                OrbitRingController ring = rings[i];
                if (ring != null && ring.RingId == ringId)
                {
                    return ring;
                }
            }

            return null;
        }
    }

    public abstract class UpgradeEffect : ScriptableObject
    {
        public abstract void Apply(UpgradeContext context);
    }
}
