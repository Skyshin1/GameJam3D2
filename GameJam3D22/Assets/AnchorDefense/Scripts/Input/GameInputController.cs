using UnityEngine;
using UnityEngine.InputSystem;

namespace AnchorDefense
{
    [DefaultExecutionOrder(-1100)]
    public sealed class GameInputController : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;

        private InputActionMap gameplayMap;

        public InputActionAsset InputActions => inputActions;
        public InputAction Point { get; private set; }
        public InputAction PrimaryPress { get; private set; }
        public InputAction SecondaryPress { get; private set; }
        public InputAction CycleCamera { get; private set; }
        public InputAction ToggleUpgrade { get; private set; }
        public InputAction Pause { get; private set; }
        public InputAction RingAxis { get; private set; }
        public InputAction CameraOrbit { get; private set; }
        public InputAction CycleRing { get; private set; }
        public InputAction CameraOrbitPress { get; private set; }
        public InputAction ToggleZoneEdit { get; private set; }

        public void Configure(InputActionAsset actions)
        {
            inputActions = actions;
            ResolveActions();
        }

        private void Awake()
        {
            ResolveActions();
            InputBindingPersistence.Load(inputActions);
        }

        private void OnEnable()
        {
            ResolveActions();
            gameplayMap?.Enable();
        }

        private void OnDisable()
        {
            gameplayMap?.Disable();
        }

        private void ResolveActions()
        {
            if (inputActions == null)
            {
                return;
            }

            gameplayMap = inputActions.FindActionMap("Gameplay", true);
            Point = gameplayMap.FindAction("Point", true);
            PrimaryPress = gameplayMap.FindAction("PrimaryPress", true);
            SecondaryPress = gameplayMap.FindAction("SecondaryPress", true);
            CycleCamera = gameplayMap.FindAction("CycleCamera", true);
            ToggleUpgrade = gameplayMap.FindAction("ToggleUpgrade", true);
            Pause = gameplayMap.FindAction("Pause", true);
            RingAxis = gameplayMap.FindAction("RingAxis", true);
            CameraOrbit = gameplayMap.FindAction("CameraOrbit", true);
            CycleRing = gameplayMap.FindAction("CycleRing", true);
            CameraOrbitPress = gameplayMap.FindAction("CameraOrbitPress", true);
            ToggleZoneEdit = gameplayMap.FindAction("ToggleZoneEdit", true);
        }
    }
}
