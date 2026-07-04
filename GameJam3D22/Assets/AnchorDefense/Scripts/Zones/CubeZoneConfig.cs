using UnityEngine;

namespace AnchorDefense
{
    [CreateAssetMenu(menuName = "Anchor Defense/Zones/Cube Zone Config", fileName = "CubeZoneConfig")]
    public sealed class CubeZoneConfig : ScriptableObject
    {
        public const int ZoneCount = 8;

        [field: SerializeField, Min(1f)] public float GridHalfExtent { get; private set; } = 10.5f;
        [field: SerializeField] public CubeZoneEffectDefinition[] AvailableEffects { get; private set; }
        [field: SerializeField] public CubeZoneEffectDefinition[] DefaultZoneEffects { get; private set; }

        public CubeZoneEffectDefinition GetDefaultEffect(int zoneIndex)
        {
            return DefaultZoneEffects != null && zoneIndex >= 0 && zoneIndex < DefaultZoneEffects.Length
                ? DefaultZoneEffects[zoneIndex]
                : null;
        }

#if UNITY_EDITOR
        public void Configure(float halfExtent, CubeZoneEffectDefinition[] effects,
            CubeZoneEffectDefinition[] defaults)
        {
            GridHalfExtent = Mathf.Max(1f, halfExtent);
            AvailableEffects = effects ?? new CubeZoneEffectDefinition[0];
            DefaultZoneEffects = new CubeZoneEffectDefinition[ZoneCount];
            if (defaults != null)
            {
                for (int i = 0; i < Mathf.Min(ZoneCount, defaults.Length); i++)
                {
                    DefaultZoneEffects[i] = defaults[i];
                }
            }
        }
#endif
    }
}
