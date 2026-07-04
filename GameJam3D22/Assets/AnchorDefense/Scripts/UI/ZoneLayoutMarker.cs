using UnityEngine;
using UnityEngine.UI;

namespace AnchorDefense
{
    [RequireComponent(typeof(Button))]
    public sealed class ZoneLayoutMarker : MonoBehaviour
    {
        [SerializeField] private int slotIndex;
        [SerializeField] private Image background;
        [SerializeField] private Text label;
        [SerializeField] private Outline outline;
        private CubeZoneAssignmentController controller;
        private bool selected;

        public int SlotIndex => slotIndex;

        public void Configure(int slot, Image backgroundView, Text labelView, Outline outlineView)
        {
            slotIndex = slot;
            background = backgroundView;
            label = labelView;
            outline = outlineView;
        }

        public void Bind(CubeZoneAssignmentController owner)
        {
            controller = owner;
            GetComponent<Button>().onClick.RemoveListener(HandleClick);
            GetComponent<Button>().onClick.AddListener(HandleClick);
        }

        public void Refresh(CubeZoneVolume cube, CubeZoneEffectDefinition effect, bool isSelected)
        {
            if (background != null)
            {
                Color color = effect != null ? effect.ZoneColor : new Color(0.1f, 0.12f, 0.16f, 1f);
                color.a = 0.86f;
                background.color = color;
            }
            if (label != null)
            {
                label.text = cube != null ? $"C{cube.CubeId + 1:00}" : "--";
            }
            SetSelected(isSelected);
        }

        public void SetSelected(bool value)
        {
            selected = value;
            if (outline != null)
            {
                outline.effectColor = selected ? new Color(0.25f, 1f, 0.9f, 1f) : new Color(0.1f, 0.4f, 0.55f, 0.7f);
                outline.effectDistance = selected ? new Vector2(4f, -4f) : new Vector2(2f, -2f);
            }
        }

        private void OnDestroy()
        {
            Button button = GetComponent<Button>();
            if (button != null) button.onClick.RemoveListener(HandleClick);
        }

        private void HandleClick()
        {
            controller?.SelectSlot(slotIndex);
        }
    }
}
