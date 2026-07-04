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
        [Tooltip("放在这里的预制体只属于这个区域立方体，并会随该立方体一起移动。")]
        [SerializeField] private GameObject localVfxPrefab;

        private MaterialPropertyBlock propertyBlock;
        private CubeZoneEffectDefinition currentEffect;
        private GameObject localVfxInstance;
        private bool isSelected;
        private bool isDropCandidate;

        public int CubeId => cubeId;
        public Vector3Int GridPosition => gridPosition;
        public GameObject LocalVfxPrefab => localVfxPrefab;

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

        public void SetEffect(CubeZoneEffectDefinition effect)
        {
            currentEffect = effect;
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

        public void EnsureLocalVfx()
        {
            if (localVfxPrefab == null || localVfxInstance != null || localVfxAnchor == null)
            {
                return;
            }

            localVfxInstance = Instantiate(localVfxPrefab, localVfxAnchor);
            localVfxInstance.name = localVfxPrefab.name + " (Zone Local VFX)";
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
                : new Color(0.2f, 0.25f, 0.3f, 0.035f);

            if (isSelected)
            {
                color = Color.Lerp(color, Color.white, 0.38f);
                color.a = Mathf.Max(color.a, 0.26f);
            }
            else if (isDropCandidate)
            {
                color = Color.Lerp(color, new Color(0.3f, 1f, 0.9f), 0.3f);
                color.a = Mathf.Max(color.a, 0.2f);
            }

            propertyBlock.SetColor(BaseColorId, color);
            propertyBlock.SetColor(ColorId, color);
            zoneRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
