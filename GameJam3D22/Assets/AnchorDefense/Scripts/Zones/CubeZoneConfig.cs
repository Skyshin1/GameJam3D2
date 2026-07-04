using UnityEngine;

namespace AnchorDefense
{
    [CreateAssetMenu(menuName = "Anchor Defense/Zones/Cube Zone Config", fileName = "CubeZoneConfig")]
    public sealed class CubeZoneConfig : ScriptableObject
    {
        public const int ZoneCount = 8;

        [Tooltip("所有区域方块的统一边长。位置步长、碰撞体和吸附虚影都会使用这个值，因此不会产生间隙。")]
        [field: SerializeField, Min(1f)] public float CubeSize { get; private set; } = 10.5f;
        [field: SerializeField] public CubeZoneEffectDefinition[] AvailableEffects { get; private set; }
        [field: SerializeField] public CubeZoneEffectDefinition[] DefaultZoneEffects { get; private set; }

        public CubeZoneEffectDefinition GetDefaultEffect(int zoneIndex)
        {
            return DefaultZoneEffects != null && zoneIndex >= 0 && zoneIndex < DefaultZoneEffects.Length
                ? DefaultZoneEffects[zoneIndex]
                : null;
        }

#if UNITY_EDITOR
        public void Configure(float cubeSize, CubeZoneEffectDefinition[] effects,
            CubeZoneEffectDefinition[] defaults)
        {
            CubeSize = Mathf.Max(1f, cubeSize);
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
