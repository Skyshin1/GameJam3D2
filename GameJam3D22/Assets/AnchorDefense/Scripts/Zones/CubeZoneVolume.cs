using UnityEngine;

namespace AnchorDefense
{
    public sealed class CubeZoneVolume : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private int zoneIndex;
        [SerializeField] private Renderer zoneRenderer;
        [SerializeField] private BoxCollider zoneCollider;
        private MaterialPropertyBlock propertyBlock;

        public int ZoneIndex => zoneIndex;

        public void Configure(int index, Renderer visual, BoxCollider bounds)
        {
            zoneIndex = index;
            zoneRenderer = visual;
            zoneCollider = bounds;
        }

        public void SetEffect(CubeZoneEffectDefinition effect)
        {
            if (zoneRenderer == null)
            {
                return;
            }
            propertyBlock = propertyBlock ?? new MaterialPropertyBlock();
            zoneRenderer.GetPropertyBlock(propertyBlock);
            Color color = effect != null ? effect.ZoneColor : new Color(0.2f, 0.25f, 0.3f, 0.035f);
            propertyBlock.SetColor(BaseColorId, color);
            propertyBlock.SetColor(ColorId, color);
            zoneRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
