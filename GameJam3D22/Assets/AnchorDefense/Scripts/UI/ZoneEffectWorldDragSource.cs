using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AnchorDefense
{
    public sealed class ZoneEffectWorldDragSource : MonoBehaviour, IPointerEnterHandler,
        IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private CubeZoneEffectDefinition definition;
        [SerializeField] private Image icon;
        [SerializeField] private Text label;
        [SerializeField] private CanvasGroup canvasGroup;
        private CubeZoneEditModeController owner;
        private RectTransform dragGhost;
        private float hoverTime;
        private bool hovering;
        private bool tooltipShown;
        private bool unlocked;

        public CubeZoneEffectDefinition Definition => definition;

        public void Configure(CubeZoneEffectDefinition effect, Image iconView, Text labelView,
            CanvasGroup group)
        {
            definition = effect;
            icon = iconView;
            label = labelView;
            canvasGroup = group;
        }

        public void Bind(CubeZoneEditModeController controller) => owner = controller;

        public void SetUnlocked(bool value)
        {
            unlocked = value;
            if (canvasGroup != null) canvasGroup.alpha = unlocked ? 1f : 0.42f;
            if (label != null) label.text = definition != null
                ? definition.DisplayName + (unlocked ? string.Empty : "  [LOCKED]") : "--";
        }

        private void Update()
        {
            if (!hovering || tooltipShown) return;
            hoverTime += Time.unscaledDeltaTime;
            if (hoverTime >= 1f)
            {
                tooltipShown = true;
                owner?.ShowFragmentTooltip(definition, unlocked);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovering = true;
            hoverTime = 0f;
            tooltipShown = false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovering = false;
            tooltipShown = false;
            owner?.HideFragmentTooltip();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!unlocked || definition == null) return;
            owner?.HideFragmentTooltip();
            Canvas canvas = GetComponentInParent<Canvas>();
            GameObject ghost = new GameObject("World Fragment Drag Ghost",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ghost.transform.SetParent(canvas.transform, false);
            dragGhost = ghost.GetComponent<RectTransform>();
            dragGhost.sizeDelta = new Vector2(76f, 76f);
            Image image = ghost.GetComponent<Image>();
            image.sprite = definition.Icon;
            image.color = definition.Icon != null ? Color.white : definition.ZoneColor;
            image.raycastTarget = false;
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragGhost != null) dragGhost.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (dragGhost != null) Destroy(dragGhost.gameObject);
            dragGhost = null;
            if (unlocked) owner?.AssignFragmentAtPointer(definition, eventData.position);
        }
    }
}
