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
        [SerializeField] private string bindingGroup = "Keyboard&Mouse";
        [SerializeField] private Button rebindButton;
        [SerializeField] private Text bindingText;

        private InputActionAsset inputActions;
        private InputAction action;
        private InputActionRebindingExtensions.RebindingOperation operation;
        private bool wasEnabled;

        public string ActionName => actionName;
        public Button RebindButton => rebindButton;
        public Text BindingText => bindingText;
        public bool IsRebinding => operation != null;

        public void Configure(string mapName, string inputActionName, int index, Button button, Text valueText)
        {
            actionMapName = mapName;
            actionName = inputActionName;
            bindingIndex = index;
            rebindButton = button;
            bindingText = valueText;
        }

        public void Configure(string mapName, string inputActionName, string group, Button button, Text valueText)
        {
            actionMapName = mapName;
            actionName = inputActionName;
            bindingGroup = group;
            rebindButton = button;
            bindingText = valueText;
        }

        public void Initialize(InputActionAsset asset)
        {
            inputActions = asset;
            action = inputActions?.FindActionMap(actionMapName, false)?.FindAction(actionName, false);
            ResolveBindingIndex();
            rebindButton.onClick.RemoveListener(BeginRebind);
            rebindButton.onClick.AddListener(BeginRebind);
            Refresh();
        }

        public void Refresh()
        {
            ResolveBindingIndex();
            if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            {
                bindingText.text = "未绑定";
                rebindButton.interactable = false;
                return;
            }

            rebindButton.interactable = operation == null;
            string devicePrefix = bindingGroup == "Gamepad" ? "PAD  " : "KBM  ";
            bindingText.text = devicePrefix + action.GetBindingDisplayString(bindingIndex);
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
            if (bindingGroup == "Gamepad")
            {
                operation.WithControlsHavingToMatchPath("<Gamepad>");
            }
            else
            {
                operation.WithControlsExcluding("<Gamepad>");
            }
            operation.Start();
        }

        private void ResolveBindingIndex()
        {
            if (action == null || string.IsNullOrEmpty(bindingGroup)) return;
            bindingIndex = -1;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                string groups = action.bindings[i].groups;
                if (!string.IsNullOrEmpty(groups) &&
                    (groups == bindingGroup || groups.Contains(bindingGroup)))
                {
                    bindingIndex = i;
                    return;
                }
            }
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
