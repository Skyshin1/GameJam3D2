using UnityEngine;

namespace AnchorDefense
{
    /// <summary>
    /// Keeps exactly one zone-driven VFX attached to a combat actor. Reused enemies keep
    /// this lightweight component, while the instantiated effect is cleared on pool disable.
    /// </summary>
    public sealed class ZoneEffectVfxBinding : MonoBehaviour
    {
        private GameObject currentPrefab;
        private GameObject instance;

        public void SetPrefab(GameObject prefab)
        {
            if (currentPrefab == prefab && (prefab == null || instance != null)) return;
            ClearInstance();
            currentPrefab = prefab;
            if (prefab == null) return;

            instance = Instantiate(prefab, transform);
            instance.name = prefab.name + " (Zone Actor VFX)";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
        }

        private void OnDisable()
        {
            ClearInstance();
            currentPrefab = null;
        }

        private void ClearInstance()
        {
            if (instance == null) return;
            Destroy(instance);
            instance = null;
        }
    }
}
