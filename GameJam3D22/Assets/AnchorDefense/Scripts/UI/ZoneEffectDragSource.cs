using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AnchorDefense
{
    public sealed class ZoneEffectDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler,
        IEndDragHandler, IPointerClickHandler
    {
        [SerializeField] private CubeZoneEffectDefinition definition;
        [SerializeField] private Image icon;
        [SerializeField] private Text label;
        private RectTransform dragGhost;
        private CubeZoneAssignmentController controller;
        private bool isUnlocked = true;

        public CubeZoneEffectDefinition Definition => definition;
        public bool IsUnlocked => isUnlocked;

        public void Configure(CubeZoneEffectDefinition effect, Image iconView, Text labelView)
        {
            definition = effect;
            icon = iconView;
            label = labelView;
            Refresh();
        }

        public void Bind(CubeZoneAssignmentController owner)
        {
            controller = owner;
        }

        public void SetUnlocked(bool unlocked)
        {
            isUnlocked = unlocked;
            Refresh();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (definition == null || !isUnlocked)
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

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isUnlocked)
            {
                controller?.AssignSelected(definition);
            }
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
                label.text = isUnlocked ? definition.DisplayName : definition.DisplayName + "\n（技能树未解锁）";
                label.color = isUnlocked ? Color.white : new Color(0.48f, 0.56f, 0.62f, 1f);
            }
            CanvasGroup group = GetComponent<CanvasGroup>();
            if (group != null) group.alpha = isUnlocked ? 1f : 0.45f;
        }
    }
}
