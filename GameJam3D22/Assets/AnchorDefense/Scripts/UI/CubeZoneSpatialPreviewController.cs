using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AnchorDefense
{
    public sealed class CubeZoneSpatialPreviewController : MonoBehaviour, IDragHandler, IPointerClickHandler
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private RawImage display;
        [SerializeField] private Camera previewCamera;
        [SerializeField] private Transform modelRoot;
        [SerializeField] private Transform[] cubeTransforms;
        [SerializeField] private Renderer[] cubeRenderers;
        [SerializeField] private TextMesh[] cubeLabels;
        [SerializeField] private float rotationSensitivity = 0.35f;

        private MaterialPropertyBlock propertyBlock;
        private CubeZoneGridController grid;
        private CubeZoneAssignmentController owner;
        private int selectedCubeId;
        private float yaw = -35f;
        private float pitch = 24f;

        public void Configure(RawImage image, Camera camera, Transform model,
            Transform[] cubes, Renderer[] renderers, TextMesh[] labels)
        {
            display = image;
            previewCamera = camera;
            modelRoot = model;
            cubeTransforms = cubes;
            cubeRenderers = renderers;
            cubeLabels = labels;
        }

        public void Initialize(CubeZoneGridController zoneGrid, CubeZoneAssignmentController controller)
        {
            if (grid != null)
            {
                grid.LayoutChanged -= Refresh;
                grid.AssignmentChanged -= HandleAssignmentChanged;
            }
            grid = zoneGrid;
            owner = controller;
            if (grid != null)
            {
                grid.LayoutChanged += Refresh;
                grid.AssignmentChanged += HandleAssignmentChanged;
            }
            ApplyRotation();
            Refresh();
        }

        public void SetSelectedCube(int cubeId)
        {
            selectedCubeId = cubeId;
            RefreshVisuals();
        }

        public void OnDrag(PointerEventData eventData)
        {
            yaw -= eventData.delta.x * rotationSensitivity;
            pitch = Mathf.Clamp(pitch + eventData.delta.y * rotationSensitivity, -80f, 80f);
            ApplyRotation();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (previewCamera == null || display == null) return;
            RectTransform rect = display.rectTransform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rect, eventData.position, eventData.pressEventCamera, out Vector2 local)) return;
            Rect bounds = rect.rect;
            Vector2 uv = new Vector2((local.x - bounds.xMin) / bounds.width,
                (local.y - bounds.yMin) / bounds.height);
            Ray ray = previewCamera.ViewportPointToRay(uv);
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f, 1 << 31,
                    QueryTriggerInteraction.Collide)) return;
            for (int i = 0; i < cubeTransforms.Length; i++)
            {
                if (cubeTransforms[i] != null &&
                    (hit.transform == cubeTransforms[i] || hit.transform.IsChildOf(cubeTransforms[i])))
                {
                    owner?.SelectSlot(i);
                    return;
                }
            }
        }

        public void Refresh()
        {
            if (grid == null) return;
            for (int i = 0; i < cubeTransforms.Length; i++)
            {
                CubeZoneVolume cube = grid.GetCubeById(i);
                if (cubeTransforms[i] != null && cube != null)
                    cubeTransforms[i].localPosition = (Vector3)cube.GridPosition * 1.05f;
            }
            RefreshVisuals();
        }

        private void LateUpdate()
        {
            if (previewCamera == null || cubeLabels == null) return;
            for (int i = 0; i < cubeLabels.Length; i++)
            {
                TextMesh label = cubeLabels[i];
                if (label == null || cubeTransforms[i] == null) continue;
                Vector3 towardCamera = (previewCamera.transform.position - cubeTransforms[i].position).normalized;
                label.transform.position = cubeTransforms[i].position + towardCamera * 0.52f;
                label.transform.rotation = previewCamera.transform.rotation;
            }
        }

        private void RefreshVisuals()
        {
            if (grid == null) return;
            propertyBlock ??= new MaterialPropertyBlock();
            for (int i = 0; i < cubeRenderers.Length; i++)
            {
                Renderer renderer = cubeRenderers[i];
                if (renderer == null) continue;
                CubeZoneEffectDefinition effect = grid.GetAssignedEffect(i);
                Color color = effect != null ? effect.ZoneColor : new Color(0.15f, 0.3f, 0.45f, 1f);
                color.a = 1f;
                if (i == selectedCubeId) color = Color.Lerp(color, Color.white, 0.42f);
                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, color);
                propertyBlock.SetColor(ColorId, color);
                renderer.SetPropertyBlock(propertyBlock);
                cubeTransforms[i].localScale = Vector3.one * (i == selectedCubeId ? 0.98f : 0.9f);
            }
        }

        private void ApplyRotation()
        {
            if (modelRoot != null) modelRoot.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private void HandleAssignmentChanged(int cubeId, CubeZoneEffectDefinition effect) => RefreshVisuals();

        private void OnDestroy()
        {
            if (grid == null) return;
            grid.LayoutChanged -= Refresh;
            grid.AssignmentChanged -= HandleAssignmentChanged;
        }
    }
}
