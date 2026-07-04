using UnityEngine;
using UnityEngine.EventSystems;

namespace AnchorDefense
{
    public enum CameraOrbitMode
    {
        Disabled,

        // 注意：这里名字虽然还叫 XYPlane，
        // 但实际逻辑已经改成 Unity 水平面 XZ 旋转。
        XYPlane,

        FreeOrbit360
    }

    public sealed class RingInputController : MonoBehaviour
    {
        [Header("Ring Input")]
        [SerializeField] private float ringRotationSensitivity = 0.32f;

        [Header("Camera Orbit")]
        [SerializeField] private CameraOrbitMode cameraOrbitMode = CameraOrbitMode.Disabled;
        [SerializeField] private float cameraOrbitSensitivity = 0.18f;
        [SerializeField] private bool resetCameraWhenDisabled = true;
        [SerializeField, Range(-89f, 0f)] private float minimumFreePitch = -80f;
        [SerializeField, Range(0f, 89f)] private float maximumFreePitch = 80f;

        private Camera gameplayCamera;
        private Transform orbitTarget;
        private OrbitRingController selectedRing;
        private CubeZoneGridController cubeZoneGrid;
        private GameInputController input;

        private Vector2 previousRingPointerPosition;
        private Vector2 previousCameraPointerPosition;

        private Vector3 fixedCameraPosition;
        private Quaternion fixedCameraRotation;

        private CameraOrbitMode initializedOrbitMode;

        private float planarAngle;
        private float planarRadius;
        private float planarHeight;

        private float freeYaw;
        private float freePitch;
        private float freeDistance;

        private bool isInitialized;
        private bool isRingDragging;
        private bool isCameraDragging;

        public CameraOrbitMode CameraOrbitMode => cameraOrbitMode;

        public void Initialize(Camera targetCamera, Transform target, GameInputController inputController)
        {
            gameplayCamera = targetCamera;
            orbitTarget = target;
            input = inputController;
            cubeZoneGrid = FindObjectOfType<CubeZoneGridController>(true);

            fixedCameraPosition = gameplayCamera.transform.position;
            fixedCameraRotation = gameplayCamera.transform.rotation;

            isInitialized = true;

            InitializeOrbitMode(cameraOrbitMode);
        }

        public void SetCameraOrbitMode(CameraOrbitMode mode)
        {
            cameraOrbitMode = mode;

            if (isInitialized)
            {
                InitializeOrbitMode(mode);
            }
        }

        public void CycleCameraOrbitMode()
        {
            int nextMode = ((int)cameraOrbitMode + 1) % 3;
            SetCameraOrbitMode((CameraOrbitMode)nextMode);
        }

        public void ApplyCameraOrbitDelta(Vector2 pointerDelta)
        {
            if (!isInitialized || cameraOrbitMode == CameraOrbitMode.Disabled)
            {
                return;
            }

            if (cameraOrbitMode == CameraOrbitMode.XYPlane)
            {
                // 只用鼠标横向拖动控制水平环绕
                planarAngle += pointerDelta.x * cameraOrbitSensitivity;
                ApplyPlanarCameraPosition();
                return;
            }

            freeYaw += pointerDelta.x * cameraOrbitSensitivity;

            freePitch = Mathf.Clamp(
                freePitch + pointerDelta.y * cameraOrbitSensitivity,
                minimumFreePitch,
                maximumFreePitch);

            ApplyFreeCameraPosition();
        }

        private void Update()
        {
            if (!isInitialized || gameplayCamera == null || orbitTarget == null || input == null)
            {
                return;
            }

            if (initializedOrbitMode != cameraOrbitMode)
            {
                InitializeOrbitMode(cameraOrbitMode);
            }

            if (input.CycleCamera.WasPressedThisFrame())
            {
                CycleCameraOrbitMode();
            }

            HandleRingInput();
            HandleCameraInput();
        }

        private void HandleRingInput()
        {
            Vector2 pointerPosition = input.Point.ReadValue<Vector2>();
            if (input.PrimaryPress.WasPressedThisFrame())
            {
                isRingDragging = false;

                if (IsPointerOverUi())
                {
                    return;
                }

                previousRingPointerPosition = pointerPosition;
                if (SelectRingUnderPointer(pointerPosition))
                {
                    isRingDragging = selectedRing != null;
                }
                else
                {
                    cubeZoneGrid?.TryBeginPointerInteraction(gameplayCamera, pointerPosition);
                }
            }

            if (input.PrimaryPress.WasReleasedThisFrame())
            {
                isRingDragging = false;
                cubeZoneGrid?.EndPointerInteraction();
            }

            if (input.PrimaryPress.IsPressed() && cubeZoneGrid != null && cubeZoneGrid.IsDragging)
            {
                cubeZoneGrid.UpdatePointerInteraction(gameplayCamera, pointerPosition);
                return;
            }

            if (input.PrimaryPress.IsPressed() && isRingDragging && selectedRing != null)
            {
                float delta = pointerPosition.x - previousRingPointerPosition.x;

                selectedRing.RotateByDrag(
                    delta,
                    ringRotationSensitivity * GameSettingsService.Current.ringDragSensitivity);

                previousRingPointerPosition = pointerPosition;
            }
        }

        private void HandleCameraInput()
        {
            if (cameraOrbitMode == CameraOrbitMode.Disabled)
            {
                isCameraDragging = false;
                return;
            }

            Vector2 pointerPosition = input.Point.ReadValue<Vector2>();
            if (input.SecondaryPress.WasPressedThisFrame())
            {
                isCameraDragging = false;

                if (IsPointerOverUi())
                {
                    return;
                }

                previousCameraPointerPosition = pointerPosition;
                isCameraDragging = true;
            }

            if (input.SecondaryPress.WasReleasedThisFrame())
            {
                isCameraDragging = false;
            }

            if (input.SecondaryPress.IsPressed() && isCameraDragging)
            {
                Vector2 delta = pointerPosition - previousCameraPointerPosition;

                ApplyCameraOrbitDelta(delta * GameSettingsService.Current.cameraOrbitSensitivity);

                previousCameraPointerPosition = pointerPosition;
            }
        }

        private void InitializeOrbitMode(CameraOrbitMode mode)
        {
            initializedOrbitMode = mode;

            if (gameplayCamera == null || orbitTarget == null)
            {
                return;
            }

            if (mode == CameraOrbitMode.Disabled)
            {
                if (resetCameraWhenDisabled)
                {
                    gameplayCamera.transform.SetPositionAndRotation(
                        fixedCameraPosition,
                        fixedCameraRotation);
                }

                return;
            }

            Vector3 offset = gameplayCamera.transform.position - orbitTarget.position;

            if (mode == CameraOrbitMode.XYPlane)
            {
                // Unity 的水平面是 XZ。
                // 所以这里半径用 XZ，而不是 XY。
                planarRadius = Mathf.Max(
                    0.01f,
                    new Vector2(offset.x, offset.z).magnitude);

                // 水平角度也从 XZ 平面计算。
                planarAngle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;

                // 保持当前相机相对于目标的高度不变。
                planarHeight = offset.y;

                ApplyPlanarCameraPosition();
                return;
            }

            freeDistance = Mathf.Max(0.01f, offset.magnitude);

            freeYaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;

            freePitch = Mathf.Asin(
                Mathf.Clamp(offset.y / freeDistance, -1f, 1f)) * Mathf.Rad2Deg;

            freePitch = Mathf.Clamp(
                freePitch,
                minimumFreePitch,
                maximumFreePitch);

            ApplyFreeCameraPosition();
        }

        private void ApplyPlanarCameraPosition()
        {
            float angleRadians = planarAngle * Mathf.Deg2Rad;

            // 关键：
            // XZ 绕目标旋转，Y 高度固定。
            Vector3 offset = new Vector3(
                Mathf.Cos(angleRadians) * planarRadius,
                planarHeight,
                Mathf.Sin(angleRadians) * planarRadius);

            SetCameraPosition(orbitTarget.position + offset);
        }

        private void ApplyFreeCameraPosition()
        {
            float yawRadians = freeYaw * Mathf.Deg2Rad;
            float pitchRadians = freePitch * Mathf.Deg2Rad;

            float horizontalDistance = Mathf.Cos(pitchRadians) * freeDistance;

            Vector3 offset = new Vector3(
                Mathf.Sin(yawRadians) * horizontalDistance,
                Mathf.Sin(pitchRadians) * freeDistance,
                Mathf.Cos(yawRadians) * horizontalDistance);

            SetCameraPosition(orbitTarget.position + offset);
        }

        private void SetCameraPosition(Vector3 position)
        {
            gameplayCamera.transform.position = position;

            // 保持完整 LookAt 中心点。
            // 不要把 lookDirection.y 设为 0，
            // 否则相机不会真正看向中心。
            Vector3 lookDirection = orbitTarget.position - position;

            if (lookDirection.sqrMagnitude > 0.0001f)
            {
                gameplayCamera.transform.rotation =
                    Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            }
        }

        private bool SelectRingUnderPointer(Vector2 pointerPosition)
        {
            Ray ray = gameplayCamera.ScreenPointToRay(pointerPosition);

            OrbitRingController nextSelection = null;
            float nearestDistance = float.PositiveInfinity;
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Collide);
            for (int i = 0; i < hits.Length; i++)
            {
                OrbitRingController ring = hits[i].collider.GetComponentInParent<OrbitRingController>();
                if (ring != null && hits[i].distance < nearestDistance)
                {
                    nearestDistance = hits[i].distance;
                    nextSelection = ring;
                }
            }

            if (selectedRing == nextSelection)
            {
                return nextSelection != null;
            }

            selectedRing?.SetSelected(false);

            selectedRing = nextSelection;

            selectedRing?.SetSelected(true);
            return nextSelection != null;
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null &&
                   EventSystem.current.IsPointerOverGameObject();
        }
    }
}
