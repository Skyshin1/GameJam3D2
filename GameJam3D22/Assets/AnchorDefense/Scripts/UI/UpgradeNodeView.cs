using System;
using UnityEngine;
using UnityEngine.UI;

namespace AnchorDefense
{
    public sealed class UpgradeNodeView : MonoBehaviour
    {
        [SerializeField] private UpgradeNodeDefinition definition;
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private Text shortLabel;
        [SerializeField] private Text costLabel;
        [SerializeField] private Color branchColor = new Color(0.15f, 0.8f, 1f);

        private Action<UpgradeNodeView> selectedAction;
        private UpgradeNodeState state;
        private bool selected;

        public UpgradeNodeDefinition Definition => definition;
        public UpgradeNodeState State => state;

        public void Initialize(Action<UpgradeNodeView> onSelected)
        {
            selectedAction = onSelected;
            button.onClick.RemoveListener(HandleClicked);
            button.onClick.AddListener(HandleClicked);
        }

        public void Refresh(UpgradeSystem system)
        {
            state = system != null ? system.GetState(definition) : UpgradeNodeState.Placeholder;
            bool hasDefinition = definition != null;
            shortLabel.text = hasDefinition ? definition.ShortLabel : "?";
            costLabel.text = hasDefinition && !definition.Placeholder ? definition.KillCost.ToString() : string.Empty;
            icon.gameObject.SetActive(hasDefinition && definition.Icon != null);
            if (hasDefinition && definition.Icon != null)
            {
                icon.sprite = definition.Icon;
            }

            button.interactable = hasDefinition && !definition.Placeholder;
            background.color = GetStateColor();
            transform.localScale = selected ? Vector3.one * 1.1f : Vector3.one;
        }

        public void SetSelected(bool isSelected)
        {
            selected = isSelected;
        }

        public Color GetBranchColor()
        {
            return branchColor;
        }

#if UNITY_EDITOR
        public void Configure(
            UpgradeNodeDefinition nodeDefinition,
            Button nodeButton,
            Image nodeBackground,
            Image nodeIcon,
            Text nodeShortLabel,
            Text nodeCostLabel,
            Color color)
        {
            definition = nodeDefinition;
            button = nodeButton;
            background = nodeBackground;
            icon = nodeIcon;
            shortLabel = nodeShortLabel;
            costLabel = nodeCostLabel;
            branchColor = color;
        }
#endif

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
            }
        }

        private void HandleClicked()
        {
            selectedAction?.Invoke(this);
        }

        private Color GetStateColor()
        {
            Color color;
            switch (state)
            {
                case UpgradeNodeState.Purchased:
                    color = new Color(1f, 0.82f, 0.38f, 1f);
                    break;
                case UpgradeNodeState.Available:
                    color = Color.Lerp(branchColor, Color.white, 0.3f);
                    break;
                case UpgradeNodeState.InsufficientKills:
                    color = branchColor * new Color(0.58f, 0.58f, 0.58f, 1f);
                    color.a = 1f;
                    break;
                default:
                    color = new Color(0.11f, 0.14f, 0.22f, 0.96f);
                    break;
            }

            return selected ? Color.Lerp(color, Color.white, 0.18f) : color;
        }
    }
}
