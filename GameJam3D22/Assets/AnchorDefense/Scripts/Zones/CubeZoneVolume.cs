using UnityEngine;

namespace AnchorDefense
{
    public sealed class CubeZoneVolume : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [Header("Identity")]
        [SerializeField] private int cubeId;
        [SerializeField] private Vector3Int gridPosition;

        [Header("Presentation")]
        [SerializeField] private Renderer zoneRenderer;
        [SerializeField] private BoxCollider zoneCollider;
        [SerializeField] private Transform localVfxAnchor;
        [SerializeField] private Color selectedColor = new Color(1f, 1f, 1f, 0.05f);
        [SerializeField] private Color swapTargetColor = new Color(0.3f, 1f, 0.9f, 0.2f);

        private MaterialPropertyBlock propertyBlock;
        private CubeZoneEffectDefinition currentEffect;
        private GameObject localVfxInstance;
        private GameObject currentZoneVfxPrefab;
        private bool isSelected;
        private bool isDropCandidate;

        public int CubeId => cubeId;
        public Vector3Int GridPosition => gridPosition;

        public void Configure(int id, Vector3Int position, Renderer visual, BoxCollider bounds, Transform vfxAnchor)
        {
            cubeId = id;
            gridPosition = position;
            zoneRenderer = visual;
            zoneCollider = bounds;
            localVfxAnchor = vfxAnchor;
        }

        public void SetGridPosition(Vector3Int position)
        {
            gridPosition = position;
        }

        public bool Contains(Vector3 worldPosition)
        {
            return zoneCollider != null && zoneCollider.bounds.Contains(worldPosition);
        }

        public void ApplySize(float cubeSize)
        {
            float size = Mathf.Max(1f, cubeSize);
            if (zoneCollider != null) zoneCollider.size = Vector3.one * size;
            if (zoneRenderer != null) zoneRenderer.transform.localScale = Vector3.one * size;
        }

        public void ApplyInteractionColors(Color selected, Color swapTarget)
        {
            selectedColor = selected;
            swapTargetColor = swapTarget;
            RefreshVisual();
        }

        public void SetEffect(CubeZoneEffectDefinition effect)
        {
            currentEffect = effect;
            SetZoneVfx(effect != null ? effect.ZoneVfxPrefab : null);
            RefreshVisual();
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            RefreshVisual();
        }

        public void SetDropCandidate(bool candidate)
        {
            isDropCandidate = candidate;
            RefreshVisual();
        }

        private void SetZoneVfx(GameObject prefab)
        {
            if (currentZoneVfxPrefab == prefab && (prefab == null || localVfxInstance != null))
            {
                return;
            }
            if (localVfxInstance != null)
            {
                Destroy(localVfxInstance);
                localVfxInstance = null;
            }
            currentZoneVfxPrefab = prefab;
            if (prefab == null || localVfxAnchor == null) return;
            localVfxInstance = Instantiate(prefab, localVfxAnchor);
            localVfxInstance.name = prefab.name + " (Zone Effect VFX)";
            localVfxInstance.transform.localPosition = Vector3.zero;
            localVfxInstance.transform.localRotation = Quaternion.identity;
            localVfxInstance.transform.localScale = Vector3.one;
        }

        private void RefreshVisual()
        {
            if (zoneRenderer == null)
            {
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            zoneRenderer.GetPropertyBlock(propertyBlock);
            Color color = currentEffect != null
                ? currentEffect.ZoneColor
                : new Color(0.2f, 0.03f, 0.3f, 0.035f);

            if (isSelected)
            {
                color = Color.Lerp(color, selectedColor, 0.38f);
                color.a = selectedColor.a;
            }
            else if (isDropCandidate)
            {
                color = Color.Lerp(color, swapTargetColor, 0.3f);
                color.a = swapTargetColor.a;
            }

            propertyBlock.SetColor(BaseColorId, color);
            propertyBlock.SetColor(ColorId, color);
            zoneRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
