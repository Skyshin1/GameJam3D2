using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AnchorDefense
{
    public sealed class InputRebindRow : MonoBehaviour
    {
        [SerializeField] private string actionMapName = "Gameplay";
        [SerializeField] private string actionName;
        [SerializeField, Min(0)] private int bindingIndex;
        [SerializeField] private Button rebindButton;
        [SerializeField] private Text bindingText;

        private InputActionAsset inputActions;
        private InputAction action;
        private InputActionRebindingExtensions.RebindingOperation operation;
        private bool wasEnabled;

        public void Configure(string mapName, string inputActionName, int index, Button button, Text valueText)
        {
            actionMapName = mapName;
            actionName = inputActionName;
            bindingIndex = index;
            rebindButton = button;
            bindingText = valueText;
        }

        public void Initialize(InputActionAsset asset)
        {
            inputActions = asset;
            action = inputActions?.FindActionMap(actionMapName, false)?.FindAction(actionName, false);
            rebindButton.onClick.RemoveListener(BeginRebind);
            rebindButton.onClick.AddListener(BeginRebind);
            Refresh();
        }

        public void Refresh()
        {
            if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            {
                bindingText.text = "未绑定";
                rebindButton.interactable = false;
                return;
            }

            rebindButton.interactable = operation == null;
            bindingText.text = action.GetBindingDisplayString(bindingIndex);
        }

        private void OnDestroy()
        {
            operation?.Dispose();
            if (rebindButton != null)
            {
                rebindButton.onClick.RemoveListener(BeginRebind);
            }
        }

        private void BeginRebind()
        {
            if (action == null || operation != null)
            {
                return;
            }

            wasEnabled = action.enabled;
            action.Disable();
            bindingText.text = "请按下新的按键…  Esc 取消";
            rebindButton.interactable = false;
            operation = action.PerformInteractiveRebinding(bindingIndex)
                .WithControlsExcluding("<Pointer>/position")
                .WithControlsExcluding("<Pointer>/delta")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnCancel(FinishRebind)
                .OnComplete(FinishRebind);
            operation.Start();
        }

        private void FinishRebind(InputActionRebindingExtensions.RebindingOperation completedOperation)
        {
            completedOperation.Dispose();
            operation = null;
            if (wasEnabled)
            {
                action.Enable();
            }
            InputBindingPersistence.Save(inputActions);
            Refresh();
        }
    }
}
