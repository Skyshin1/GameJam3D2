using UnityEngine;
using UnityEngine.InputSystem;

namespace AnchorDefense
{
    [DefaultExecutionOrder(-1100)]
    public sealed class GameInputController : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;

        private InputActionMap gameplayMap;
        private InputActionMap commandConsoleMap;

        public InputActionAsset InputActions => inputActions;
        public bool IsTextEntryActive { get; set; }

        public bool ShouldSuppressShortcut(InputAction action)
        {
            return IsTextEntryActive && action != null &&
                action.activeControl != null && action.activeControl.device is Keyboard;
        }
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
        public InputAction ToggleAICommand { get; private set; }
        public InputAction SubmitAICommand { get; private set; }
        public InputAction CloseAICommand { get; private set; }

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
            commandConsoleMap?.Enable();
        }

        private void OnDisable()
        {
            gameplayMap?.Disable();
            commandConsoleMap?.Disable();
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
            commandConsoleMap = inputActions.FindActionMap("CommandConsole", true);
            ToggleAICommand = commandConsoleMap.FindAction("Toggle", true);
            SubmitAICommand = commandConsoleMap.FindAction("Submit", true);
            CloseAICommand = commandConsoleMap.FindAction("Close", true);
        }

        public void SetGameplayInputEnabled(bool enabled)
        {
            if (enabled) gameplayMap?.Enable();
            else gameplayMap?.Disable();
        }
    }
}
