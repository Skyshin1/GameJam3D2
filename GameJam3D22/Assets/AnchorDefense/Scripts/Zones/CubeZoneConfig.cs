using UnityEngine;

namespace AnchorDefense
{
    [CreateAssetMenu(menuName = "Anchor Defense/Zones/Cube Zone Config", fileName = "CubeZoneConfig")]
    public sealed class CubeZoneConfig : ScriptableObject
    {
        public const int ZoneCount = 8;

        [Tooltip("所有区域方块的统一边长。位置步长、碰撞体和吸附虚影都会使用这个值，因此不会产生间隙。")]
        [field: SerializeField, Min(1f)] public float CubeSize { get; private set; } = 10.5f;

        [field: Header("Zone Interaction Presentation")]
        [field: Tooltip("当前选中方块的边框颜色。A 值就是透明度。")]
        [field: SerializeField] public Color SelectedCubeColor { get; private set; } = new Color(1f, 1f, 1f, 0.05f);
        [field: Tooltip("拖动时可交换目标方块的边框颜色。A 值就是透明度。")]
        [field: SerializeField] public Color SwapTargetColor { get; private set; } = new Color(0.3f, 1f, 0.9f, 0.2f);
        [field: Tooltip("尚未指向的可移动空位虚影颜色。A 值就是透明度。")]
        [field: SerializeField] public Color AvailableHintColor { get; private set; } = new Color(0.2f, 0.78f, 1f, 0.15f);
        [field: Tooltip("鼠标当前指向的可移动空位虚影颜色。A 值就是透明度。")]
        [field: SerializeField] public Color HoveredHintColor { get; private set; } = new Color(0.25f, 1f, 0.65f, 0.34f);

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
