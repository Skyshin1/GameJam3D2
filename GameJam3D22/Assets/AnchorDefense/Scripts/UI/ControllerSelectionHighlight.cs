using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AnchorDefense
{
    public sealed class ControllerSelectionHighlight : MonoBehaviour,
        ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly List<RaycastResult> RaycastResults = new List<RaycastResult>(32);

        [SerializeField, Min(1f)] private float selectedScale = 1.035f;
        [SerializeField] private Color outlineColor = new Color(0.12f, 1f, 0.92f, 1f);
        [SerializeField] private Vector2 outlineBaseDistance = new Vector2(5f, -5f);
        [SerializeField, Min(0f)] private float outlinePulseAmount = 1.5f;
        [SerializeField, Min(0f)] private float outlinePulseSpeed = 6f;

        private Selectable selectable;
        private Outline focusOutline;
        private Vector3 baseScale = Vector3.one;

        private bool selected;
        private bool hovered;
        private bool lastVisible;

        public static void EnsureInHierarchy(Transform root)
        {
            if (root == null)
            {
                return;
            }

            Selectable[] selectables = root.GetComponentsInChildren<Selectable>(true);

            for (int i = 0; i < selectables.Length; i++)
            {
                Selectable item = selectables[i];

                if (item != null &&
                    item.GetComponent<ControllerSelectionHighlight>() == null)
                {
                    item.gameObject.AddComponent<ControllerSelectionHighlight>();
                }
            }
        }

        public static void RefreshAllInHierarchy(Transform root)
        {
            if (root == null)
            {
                return;
            }

            ControllerSelectionHighlight[] highlights =
                root.GetComponentsInChildren<ControllerSelectionHighlight>(true);

            for (int i = 0; i < highlights.Length; i++)
            {
                if (highlights[i] != null)
                {
                    highlights[i].ForceRefreshPublic();
                }
            }
        }

        public void ForceRefreshPublic()
        {
            ForceRefresh();
        }

        private void Awake()
        {
            selectable = GetComponent<Selectable>();

            baseScale = transform.localScale;
            if (baseScale == Vector3.zero)
            {
                baseScale = Vector3.one;
            }

            Graphic target = selectable != null
                ? selectable.targetGraphic
                : GetComponent<Graphic>();

            if (target != null)
            {
                focusOutline = target.GetComponent<Outline>();

                if (focusOutline == null)
                {
                    focusOutline = target.gameObject.AddComponent<Outline>();
                }

                focusOutline.effectColor = outlineColor;
                focusOutline.effectDistance = outlineBaseDistance;
                focusOutline.useGraphicAlpha = false;
                focusOutline.enabled = false;
            }

            ForceRefresh();
        }

        private void OnEnable()
        {
            ForceRefresh();
        }

        private void Update()
        {
            SyncSelectedFromEventSystem();
            SyncHoveredFromPointerRaycast();

            bool visible = ShouldShow();

            if (visible && focusOutline != null)
            {
                float pulse = outlineBaseDistance.x +
                              Mathf.Sin(Time.unscaledTime * outlinePulseSpeed) *
                              outlinePulseAmount;

                focusOutline.effectDistance = new Vector2(pulse, -pulse);
            }

            if (visible != lastVisible)
            {
                Refresh(true);
            }
        }

        public void OnSelect(BaseEventData eventData)
        {
            selected = true;
            Refresh(true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            selected = false;
            Refresh(true);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            Refresh(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            Refresh(true);
        }

        private void OnDisable()
        {
            selected = false;
            hovered = false;
            lastVisible = false;

            transform.localScale = baseScale;

            if (focusOutline != null)
            {
                focusOutline.enabled = false;
            }
        }

        private void ForceRefresh()
        {
            SyncSelectedFromEventSystem();
            SyncHoveredFromPointerRaycast();
            Refresh(true);
        }

        private void SyncSelectedFromEventSystem()
        {
            EventSystem eventSystem = EventSystem.current;

            selected =
                eventSystem != null &&
                eventSystem.currentSelectedGameObject == gameObject;
        }

        private void SyncHoveredFromPointerRaycast()
        {
            EventSystem eventSystem = EventSystem.current;
            Mouse mouse = Mouse.current;

            if (eventSystem == null || mouse == null || selectable == null)
            {
                return;
            }

            PointerEventData pointerData = new PointerEventData(eventSystem)
            {
                position = mouse.position.ReadValue()
            };

            RaycastResults.Clear();
            eventSystem.RaycastAll(pointerData, RaycastResults);

            hovered = false;

            for (int i = 0; i < RaycastResults.Count; i++)
            {
                GameObject hitObject = RaycastResults[i].gameObject;
                if (hitObject == null)
                {
                    continue;
                }

                Selectable hitSelectable = hitObject.GetComponentInParent<Selectable>();

                if (hitSelectable == null)
                {
                    continue;
                }

                hovered = hitSelectable == selectable;
                break;
            }
        }

        private bool ShouldShow()
        {
            if (selectable != null &&
                (!selectable.interactable || !selectable.gameObject.activeInHierarchy))
            {
                return false;
            }

            return selected || hovered;
        }

        private void Refresh(bool force)
        {
            bool visible = ShouldShow();

            if (!force && visible == lastVisible)
            {
                return;
            }

            lastVisible = visible;

            if (focusOutline != null)
            {
                focusOutline.enabled = visible;
                focusOutline.effectColor = outlineColor;
            }

            transform.localScale = visible
                ? baseScale * selectedScale
                : baseScale;
        }
    }
}