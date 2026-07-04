using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AnchorDefense
{
    public sealed class ZoneEffectDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private CubeZoneEffectDefinition definition;
        [SerializeField] private Image icon;
        [SerializeField] private Text label;
        private RectTransform dragGhost;

        public CubeZoneEffectDefinition Definition => definition;

        public void Configure(CubeZoneEffectDefinition effect, Image iconView, Text labelView)
        {
            definition = effect;
            icon = iconView;
            label = labelView;
            Refresh();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (definition == null)
            {
                return;
            }
            Canvas canvas = GetComponentInParent<Canvas>();
            GameObject ghost = new GameObject("Zone Effect Drag Ghost", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ghost.transform.SetParent(canvas.transform, false);
            dragGhost = ghost.GetComponent<RectTransform>();
            dragGhost.sizeDelta = new Vector2(72f, 72f);
            Image ghostImage = ghost.GetComponent<Image>();
            ghostImage.sprite = definition.Icon;
            ghostImage.color = definition.Icon != null ? Color.white : definition.ZoneColor;
            ghostImage.raycastTarget = false;
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragGhost != null)
            {
                dragGhost.position = eventData.position;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (dragGhost != null)
            {
                Destroy(dragGhost.gameObject);
                dragGhost = null;
            }
        }

        private void Refresh()
        {
            if (definition == null)
            {
                return;
            }
            if (icon != null)
            {
                icon.sprite = definition.Icon;
                icon.color = definition.Icon != null ? Color.white : definition.ZoneColor;
            }
            if (label != null)
            {
                label.text = definition.DisplayName;
            }
        }
    }
}
