using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AnchorDefense
{
    public sealed class ControllerSelectionHighlight : MonoBehaviour,
        ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private Selectable selectable;
        private Outline focusOutline;
        private Vector3 baseScale;
        private bool selected;
        private bool hovered;

        public static void EnsureInHierarchy(Transform root)
        {
            if (root == null) return;
            Selectable[] selectables = root.GetComponentsInChildren<Selectable>(true);
            for (int i = 0; i < selectables.Length; i++)
            {
                if (selectables[i] != null &&
                    selectables[i].GetComponent<ControllerSelectionHighlight>() == null)
                {
                    selectables[i].gameObject.AddComponent<ControllerSelectionHighlight>();
                }
            }
        }

        private void Awake()
        {
            selectable = GetComponent<Selectable>();
            baseScale = transform.localScale;
            Graphic target = selectable != null ? selectable.targetGraphic : GetComponent<Graphic>();
            if (target != null)
            {
                focusOutline = target.gameObject.AddComponent<Outline>();
                focusOutline.effectColor = new Color(0.12f, 1f, 0.92f, 1f);
                focusOutline.effectDistance = new Vector2(5f, -5f);
                focusOutline.useGraphicAlpha = false;
                focusOutline.enabled = false;
            }
        }

        private void Update()
        {
            if (!selected || focusOutline == null) return;
            float pulse = 4f + Mathf.Sin(Time.unscaledTime * 6f) * 1.5f;
            focusOutline.effectDistance = new Vector2(pulse, -pulse);
        }

        public void OnSelect(BaseEventData eventData)
        {
            selected = true;
            Refresh();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            selected = false;
            Refresh();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            Refresh();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            Refresh();
        }

        private void OnDisable()
        {
            selected = false;
            hovered = false;
            transform.localScale = baseScale;
            if (focusOutline != null) focusOutline.enabled = false;
        }

        private void Refresh()
        {
            bool visible = selected || hovered;
            if (focusOutline != null) focusOutline.enabled = visible;
            transform.localScale = visible ? baseScale * 1.035f : baseScale;
        }
    }
}
