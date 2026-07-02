using UnityEngine;

namespace AnchorDefense
{
    public sealed class OrbitRingController : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Renderer[] selectionRenderers;
        [SerializeField] private Color normalColor = new Color(0.2f, 0.75f, 1f);
        [SerializeField] private Color selectedColor = Color.white;

        private MaterialPropertyBlock propertyBlock;

        public void Configure(Renderer[] renderers, Color idleColor, Color highlightColor)
        {
            selectionRenderers = renderers;
            normalColor = idleColor;
            selectedColor = highlightColor;
        }

        public void RotateByDrag(float horizontalPixels, float sensitivity)
        {
            transform.Rotate(Vector3.up, -horizontalPixels * sensitivity, Space.Self);
        }

        public void SetSelected(bool selected)
        {
            if (selectionRenderers == null)
            {
                return;
            }

            propertyBlock = propertyBlock ?? new MaterialPropertyBlock();
            Color color = selected ? selectedColor : normalColor;
            for (int i = 0; i < selectionRenderers.Length; i++)
            {
                Renderer targetRenderer = selectionRenderers[i];
                if (targetRenderer == null)
                {
                    continue;
                }

                targetRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, color);
                propertyBlock.SetColor(ColorId, color);
                propertyBlock.SetColor(EmissionColorId, color * (selected ? 2f : 0.7f));
                targetRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void Awake()
        {
            SetSelected(false);
        }
    }
}
