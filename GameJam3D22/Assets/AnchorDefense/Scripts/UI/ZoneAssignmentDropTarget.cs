using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AnchorDefense
{
    public sealed class ZoneAssignmentDropTarget : MonoBehaviour, IDropHandler
    {
        [SerializeField] private int zoneIndex;
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private Text label;
        private CubeZoneAssignmentController controller;

        public int ZoneIndex => zoneIndex;

        public void Configure(int index, Image backgroundView, Image iconView, Text labelView)
        {
            zoneIndex = index;
            background = backgroundView;
            icon = iconView;
            label = labelView;
        }

        public void Bind(CubeZoneAssignmentController owner)
        {
            controller = owner;
        }

        public void SetZoneIndex(int index)
        {
            zoneIndex = index;
        }

        public void Refresh(CubeZoneEffectDefinition effect)
        {
            if (background != null)
            {
                background.color = effect != null ? effect.ZoneColor * new Color(1f, 1f, 1f, 4f) : new Color(0.08f, 0.1f, 0.15f, 1f);
            }
            if (icon != null)
            {
                icon.gameObject.SetActive(effect != null && effect.Icon != null);
                icon.sprite = effect != null ? effect.Icon : null;
            }
            if (label != null)
            {
                label.text = $"当前立方体位置 {zoneIndex + 1}\n" + (effect != null ? effect.DisplayName : "未配置");
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            ZoneEffectDragSource source = eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponent<ZoneEffectDragSource>() : null;
            if (source != null && source.Definition != null)
            {
                controller?.Assign(zoneIndex, source.Definition);
            }
        }
    }
}
