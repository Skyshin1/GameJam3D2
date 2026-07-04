using System.Collections.Generic;
using UnityEngine;

namespace AnchorDefense
{
    public sealed class TurretRegistry
    {
        private readonly List<TurretHealth> turrets = new List<TurretHealth>(32);

        public IReadOnlyList<TurretHealth> Turrets => turrets;

        public void Register(TurretHealth turret)
        {
            if (turret != null && !turrets.Contains(turret))
            {
                turrets.Add(turret);
            }
        }

        public TurretHealth FindNearestOperational(Vector3 position)
        {
            TurretHealth nearest = null;
            float nearestDistance = float.PositiveInfinity;
            for (int i = 0; i < turrets.Count; i++)
            {
                TurretHealth turret = turrets[i];
                if (turret == null || !turret.gameObject.activeInHierarchy || !turret.IsAlive)
                {
                    continue;
                }

                float distance = (turret.transform.position - position).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = turret;
                }
            }
            return nearest;
        }
    }
}
