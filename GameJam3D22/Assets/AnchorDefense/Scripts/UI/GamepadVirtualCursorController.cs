using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AnchorDefense
{
    public readonly struct ControllerCursorSnapTarget
    {
        public ControllerCursorSnapTarget(Object owner, Vector2 screenPosition, float radius)
        {
            Owner = owner;
            ScreenPosition = screenPosition;
            Radius = radius;
        }

        public Object Owner { get; }
        public Vector2 ScreenPosition { get; }
        public float Radius { get; }
    }

    public interface IControllerCursorContext
    {
        bool IsControllerCursorActive { get; }
        void CollectControllerCursorTargets(List<ControllerCursorSnapTarget> targets);
    }

    /// <summary>
    /// Console-style hybrid cursor.
    /// Right stick drives a virtual Mouse.
    /// Gamepad A / South Button becomes virtual left click.
    /// The virtual cursor is clamped to the full screen area.
    /// </summary>
    public sealed class GamepadVirtualCursorController : MonoBehaviour
    {
        private const float CursorSpeed = 1050f;
        private const float ReleaseSnapStickMagnitude = 0.52f;
        private const float ScreenEdgePadding = 4f;

        private static GamepadVirtualCursorController instance;

        private readonly List<ControllerCursorSnapTarget> snapTargets =
            new List<ControllerCursorSnapTarget>(64);

        private VirtualMouseInput virtualMouseInput;
        private ControllerCursorGraphic cursorGraphic;
        private RectTransform cursorTransform;
        private Canvas cursorCanvas;

        private InputAction stickAction;
        private InputAction submitAction;

        private IControllerCursorContext context;
        private Object snappedOwner;
        private float snapCooldown;

        private EventSystem navigationEventSystem;

        public static void SetContext(IControllerCursorContext nextContext)
        {
            EnsureExists().ApplyContext(nextContext);
        }

        public static void ClearContext(IControllerCursorContext owner)
        {
            if (instance != null && ReferenceEquals(instance.context, owner))
            {
                instance.ApplyContext(null);
            }
        }

        private static GamepadVirtualCursorController EnsureExists()
        {
            if (instance != null)
            {
                return instance;
            }

            GameObject root = new GameObject(
                "Gamepad Virtual Cursor",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            root.SetActive(false);

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            instance = root.AddComponent<GamepadVirtualCursorController>();

            DontDestroyOnLoad(root);
            root.SetActive(true);

            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            cursorCanvas = GetComponent<Canvas>();

            GameObject visual = new GameObject(
                "Cursor Visual",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(ControllerCursorGraphic));

            visual.transform.SetParent(transform, false);

            cursorTransform = visual.GetComponent<RectTransform>();
            cursorTransform.sizeDelta = new Vector2(42f, 42f);

            cursorGraphic = visual.GetComponent<ControllerCursorGraphic>();
            cursorGraphic.raycastTarget = false;

            stickAction = new InputAction(
                "Virtual Cursor Move",
                InputActionType.Value,
                "<Gamepad>/rightStick");

            submitAction = new InputAction(
                "Virtual Cursor Submit",
                InputActionType.Button,
                "<Gamepad>/buttonSouth");

            virtualMouseInput = gameObject.AddComponent<VirtualMouseInput>();
            virtualMouseInput.cursorMode = VirtualMouseInput.CursorMode.SoftwareCursor;
            virtualMouseInput.cursorTransform = cursorTransform;
            virtualMouseInput.cursorGraphic = cursorGraphic;
            virtualMouseInput.cursorSpeed = CursorSpeed;
            virtualMouseInput.stickAction = new InputActionProperty(stickAction);
            virtualMouseInput.leftButtonAction = new InputActionProperty(submitAction);

            stickAction.Enable();
            submitAction.Enable();

            ApplyContext(null);
        }

        private void OnEnable()
        {
            stickAction?.Enable();
            submitAction?.Enable();
        }

        private void OnDisable()
        {
            RestoreFocusNavigation();
            SetCursorEnabled(false);
        }

        private void OnDestroy()
        {
            RestoreFocusNavigation();

            stickAction?.Disable();
            submitAction?.Disable();

            stickAction?.Dispose();
            submitAction?.Dispose();

            if (instance == this)
            {
                instance = null;
            }
        }

        private void LateUpdate()
        {
            bool active =
                context != null &&
                context.IsControllerCursorActive &&
                Gamepad.current != null;

            if (!active)
            {
                SetCursorEnabled(false);
                RestoreFocusNavigation();
                return;
            }

            SetCursorEnabled(true);
            EnterCursorNavigationMode();
            EnsureUiInputModuleReady();

            snapCooldown = Mathf.Max(0f, snapCooldown - Time.unscaledDeltaTime);

            Mouse virtualMouse = virtualMouseInput != null
                ? virtualMouseInput.virtualMouse
                : null;

            if (virtualMouse == null)
            {
                return;
            }

            Vector2 pointer = virtualMouse.position.ReadValue();
            pointer = ClampPointerToScreen(pointer);

            InputState.Change(virtualMouse.position, pointer);
            cursorTransform.position = pointer;

            Vector2 stick = Gamepad.current.rightStick.ReadValue();

            if (snappedOwner != null && stick.magnitude >= ReleaseSnapStickMagnitude)
            {
                snappedOwner = null;
                snapCooldown = 0.16f;
            }

            snapTargets.Clear();
            context.CollectControllerCursorTargets(snapTargets);

            ControllerCursorSnapTarget? best = null;
            float bestDistance = float.PositiveInfinity;

            for (int i = 0; i < snapTargets.Count; i++)
            {
                ControllerCursorSnapTarget target = snapTargets[i];

                if (target.Owner == null)
                {
                    continue;
                }

                Vector2 targetScreenPosition = ClampPointerToScreen(target.ScreenPosition);
                float distance = Vector2.Distance(pointer, targetScreenPosition);

                bool retainingCurrent =
                    target.Owner == snappedOwner &&
                    distance <= target.Radius * 1.45f;

                bool canSnap =
                    retainingCurrent ||
                    (snapCooldown <= 0f && distance <= target.Radius);

                if (canSnap && distance < bestDistance)
                {
                    best = new ControllerCursorSnapTarget(
                        target.Owner,
                        targetScreenPosition,
                        target.Radius);

                    bestDistance = distance;
                }
            }

            if (best.HasValue)
            {
                snappedOwner = best.Value.Owner;

                pointer = ClampPointerToScreen(best.Value.ScreenPosition);

                InputState.Change(virtualMouse.position, pointer);
                cursorTransform.position = pointer;
            }

            cursorGraphic.SetPressed(Gamepad.current.buttonSouth.isPressed);
        }

        private void ApplyContext(IControllerCursorContext nextContext)
        {
            RestoreFocusNavigation();

            context = nextContext;
            snappedOwner = null;
            snapCooldown = 0f;

            bool active =
                context != null &&
                context.IsControllerCursorActive &&
                Gamepad.current != null;

            SetCursorEnabled(active);

            if (active)
            {
                EnterCursorNavigationMode();
                EnsureUiInputModuleReady();
                MoveCursorToScreenCenter();
            }
        }

        private void MoveCursorToScreenCenter()
        {
            if (virtualMouseInput == null || virtualMouseInput.virtualMouse == null)
            {
                return;
            }

            Vector2 center = new Vector2(
                Screen.width * 0.5f,
                Screen.height * 0.5f);

            center = ClampPointerToScreen(center);

            InputState.Change(virtualMouseInput.virtualMouse.position, center);
            cursorTransform.position = center;
        }

        private static Vector2 ClampPointerToScreen(Vector2 pointer)
        {
            if (float.IsNaN(pointer.x) ||
                float.IsNaN(pointer.y) ||
                float.IsInfinity(pointer.x) ||
                float.IsInfinity(pointer.y))
            {
                pointer = new Vector2(
                    Screen.width * 0.5f,
                    Screen.height * 0.5f);
            }

            float maxX = Mathf.Max(ScreenEdgePadding, Screen.width - ScreenEdgePadding);
            float maxY = Mathf.Max(ScreenEdgePadding, Screen.height - ScreenEdgePadding);

            pointer.x = Mathf.Clamp(pointer.x, ScreenEdgePadding, maxX);
            pointer.y = Mathf.Clamp(pointer.y, ScreenEdgePadding, maxY);

            return pointer;
        }

        private void RestoreFocusNavigation()
        {
            if (navigationEventSystem != null)
            {
                navigationEventSystem.sendNavigationEvents = true;
                navigationEventSystem = null;
            }
        }

        private void EnterCursorNavigationMode()
        {
            EventSystem current = EventSystem.current;

            if (navigationEventSystem == current && current != null)
            {
                return;
            }

            RestoreFocusNavigation();

            navigationEventSystem = current;

            if (navigationEventSystem != null)
            {
                // A 键在虚拟光标模式下作为鼠标左键使用。
                // 关闭 UI 焦点导航，避免同一个 A 键既点击光标下按钮，又 Submit 当前选中按钮。
                navigationEventSystem.sendNavigationEvents = false;
                navigationEventSystem.SetSelectedGameObject(null);
            }
        }

        private static void EnsureUiInputModuleReady()
        {
            EventSystem eventSystem = EventSystem.current;

            if (eventSystem == null)
            {
                return;
            }

            InputSystemUIInputModule inputModule =
                eventSystem.GetComponent<InputSystemUIInputModule>();

            if (inputModule == null)
            {
                inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            if (inputModule.actionsAsset == null)
            {
                inputModule.AssignDefaultActions();
            }
        }

        private void SetCursorEnabled(bool enabled)
        {
            if (virtualMouseInput != null)
            {
                if (virtualMouseInput.enabled != enabled)
                {
                    virtualMouseInput.enabled = enabled;
                }
            }

            if (stickAction != null)
            {
                if (enabled && !stickAction.enabled)
                {
                    stickAction.Enable();
                }
                else if (!enabled && stickAction.enabled)
                {
                    stickAction.Disable();
                }
            }

            if (submitAction != null)
            {
                if (enabled && !submitAction.enabled)
                {
                    submitAction.Enable();
                }
                else if (!enabled && submitAction.enabled)
                {
                    submitAction.Disable();
                }
            }

            if (cursorGraphic != null &&
                cursorGraphic.gameObject.activeSelf != enabled)
            {
                cursorGraphic.gameObject.SetActive(enabled);
            }
        }
    }

    public sealed class ControllerCursorGraphic : MaskableGraphic
    {
        private bool pressed;

        public void SetPressed(bool value)
        {
            if (pressed == value)
            {
                return;
            }

            pressed = value;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Color32 outer = pressed
                ? new Color32(255, 190, 55, 255)
                : new Color32(55, 235, 255, 255);

            Color32 inner = new Color32(20, 28, 45, 245);

            AddDiamond(vh, 20f, outer);
            AddDiamond(vh, 12f, inner);
            AddDiamond(vh, 5f, outer);
        }

        private static void AddDiamond(VertexHelper vh, float radius, Color32 tint)
        {
            int start = vh.currentVertCount;

            vh.AddVert(Vector3.zero, tint, Vector2.zero);
            vh.AddVert(new Vector3(0f, radius), tint, Vector2.zero);
            vh.AddVert(new Vector3(radius, 0f), tint, Vector2.zero);
            vh.AddVert(new Vector3(0f, -radius), tint, Vector2.zero);
            vh.AddVert(new Vector3(-radius, 0f), tint, Vector2.zero);

            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
            vh.AddTriangle(start, start + 3, start + 4);
            vh.AddTriangle(start, start + 4, start + 1);
        }
    }
}