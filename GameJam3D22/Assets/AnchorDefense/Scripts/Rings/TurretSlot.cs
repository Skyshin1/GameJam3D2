using UnityEngine;

namespace AnchorDefense
{
    public sealed class TurretSlot : MonoBehaviour
    {
        [SerializeField] private TurretController turretPrefab;
        [SerializeField] private bool startsUnlocked = true;

        private TurretController instance;

        public TurretController TurretPrefab => turretPrefab;
        public TurretController Instance => instance;
        public bool StartsUnlocked => startsUnlocked;

        public void Configure(TurretController prefab, bool unlocked)
        {
            turretPrefab = prefab;
            startsUnlocked = unlocked;
        }

        public TurretController EnsureInstance()
        {
            if (instance == null && turretPrefab != null)
            {
                instance = Instantiate(turretPrefab, transform);
                instance.name = turretPrefab.name;
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
            }
            return instance;
        }

        public void SetUnlocked(bool unlocked)
        {
            TurretController turret = EnsureInstance();
            if (turret != null)
            {
                turret.gameObject.SetActive(unlocked);
            }
        }
    }
}
