using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AnchorDefense
{
    public sealed class UpgradeTreeController : MonoBehaviour, IControllerCursorContext
    {
        [Header("Navigation")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text compactKillText;
        [SerializeField] private Text totalKillText;
        [SerializeField] private Text availableKillText;

        [Header("Node Details")]
        [SerializeField] private Text selectedTitle;
        [SerializeField] private Text selectedDescription;
        [SerializeField] private Text selectedCost;
        [SerializeField] private Text selectedStatus;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private Text purchaseButtonText;
        [SerializeField] private Image selectedAccent;
        [SerializeField] private UpgradeNodeView[] nodeViews;

        [Header("Audio")]
        [SerializeField] private AudioSource uiAudioSource;
        [SerializeField] private AudioClip upgradePurchasedClip;
        [SerializeField] private AudioClip purchaseFailedClip;
        [SerializeField, Range(0f, 1f)] private float upgradePurchasedVolume = 1f;


        private UpgradeSystem system;
        private GameFlowController gameFlow;
        private GameInputController input;
        private UpgradeNodeView selectedView;
        private const string ChineseUiFontPath = "Assets/AnchorDefense/Art/UI/PF频凡胡涂体 PFANHUTUTI.ttf";

        private Font chineseUiFont;
        private CubeZoneEditModeController zoneEditor;
        private ScrollRect protocolScroll;

        public bool IsControllerCursorActive => IsOpen;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        public void CloseFromExternal()
        {
            SetPanelOpen(false, true);
        }

        public void ConfigureZoneEditor(CubeZoneEditModeController editor)
        {
            zoneEditor = editor;
        }

        public void Initialize(UpgradeSystem upgradeSystem, GameFlowController flow, GameInputController inputController)
        {
            system = upgradeSystem;
            gameFlow = flow;
            ApplyDetailFonts();
            input = inputController;

            if (uiAudioSource == null)
            {
                uiAudioSource = GetComponent<AudioSource>();
            }
            if (uiAudioSource != null)
            {
                uiAudioSource.playOnAwake = false;
            }
            system.Changed += Refresh;
            gameFlow.StateChanged += HandleGameStateChanged;
            openButton.onClick.AddListener(TogglePanel);
            closeButton.onClick.AddListener(ClosePanel);
            purchaseButton.onClick.AddListener(PurchaseSelected);

            for (int i = 0; i < nodeViews.Length; i++)
            {
                nodeViews[i]?.Initialize(SelectNode);
            }

            protocolScroll = nodeViews != null && nodeViews.Length > 0 && nodeViews[0] != null
                ? nodeViews[0].GetComponentInParent<ScrollRect>()
                : null;
            ControllerSelectionHighlight.EnsureInHierarchy(panelRoot.transform);
            Text[] panelTexts = panelRoot.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < panelTexts.Length; i++)
            {
                if (panelTexts[i] != null && panelTexts[i].name == "Footer Hint")
                {
                    panelTexts[i].text = "左摇杆移动画布 · 右摇杆磁吸光标 · A 选择 · X 激活 · B 返回";
                    break;
                }
            }

            panelRoot.SetActive(false);
            selectedView = FindFirstDefinedView();
            Refresh();
        }

#if UNITY_EDITOR
        public void ConfigureView(
            GameObject treePanel,
            Button treeOpenButton,
            Button treeCloseButton,
            Text compactKills,
            Text totalKills,
            Text availableKills,
            Text detailTitle,
            Text detailDescription,
            Text detailCost,
            Text detailStatus,
            Button buyButton,
            Text buyButtonText,
            Image detailAccent,
            UpgradeNodeView[] views)
        {
            panelRoot = treePanel;
            openButton = treeOpenButton;
            closeButton = treeCloseButton;
            compactKillText = compactKills;
            totalKillText = totalKills;
            availableKillText = availableKills;
            selectedTitle = detailTitle;
            selectedDescription = detailDescription;
            selectedCost = detailCost;
            selectedStatus = detailStatus;
            purchaseButton = buyButton;
            purchaseButtonText = buyButtonText;
            selectedAccent = detailAccent;
            nodeViews = views;
        }
#endif

        private void Update()
        {
            if (input != null && input.ToggleUpgrade.WasPressedThisFrame())
            {
                TogglePanel();
            }

            if (IsOpen)
            {
                HandleControllerInput();
            }
        }

        private void OnDestroy()
        {
            if (system != null)
            {
                system.Changed -= Refresh;
            }
            if (gameFlow != null)
            {
                gameFlow.StateChanged -= HandleGameStateChanged;
            }
            if (openButton != null)
            {
                openButton.onClick.RemoveListener(TogglePanel);
            }
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(ClosePanel);
            }
            if (purchaseButton != null)
            {
                purchaseButton.onClick.RemoveListener(PurchaseSelected);
            }
            GamepadVirtualCursorController.ClearContext(this);
        }

        private void TogglePanel()
        {
            if (gameFlow == null || !gameFlow.IsPlaying)
            {
                return;
            }

            bool opening = !IsOpen;
            if (opening && zoneEditor != null && zoneEditor.IsEditing)
            {
                zoneEditor.ExitFromExternal();
            }
            SetPanelOpen(opening, true);
        }

        private void ClosePanel()
        {
            SetPanelOpen(false, true);
        }

        private void SetPanelOpen(bool open, bool updateTimeScale)
        {
            panelRoot.SetActive(open);
            if (updateTimeScale && gameFlow != null && gameFlow.IsPlaying)
            {
                Time.timeScale = open ? 0f : 1f;
            }
            if (open)
            {
                Refresh();
                GamepadVirtualCursorController.SetContext(this);
                if (selectedView != null)
                {
                    EventSystem.current?.SetSelectedGameObject(selectedView.gameObject);
                }
            }
            else
            {
                GamepadVirtualCursorController.ClearContext(this);
            }
        }

        private void HandleControllerInput()
        {
            Gamepad gamepad = Gamepad.current;
            if (gamepad == null)
            {
                return;
            }

            Vector2 pan = gamepad.leftStick.ReadValue();
            if (protocolScroll != null && pan.sqrMagnitude > 0.01f)
            {
                Vector2 normalized = protocolScroll.normalizedPosition;
                normalized.x += pan.x * 0.72f * Time.unscaledDeltaTime;
                normalized.y += pan.y * 0.72f * Time.unscaledDeltaTime;
                protocolScroll.normalizedPosition = new Vector2(
                    Mathf.Clamp01(normalized.x), Mathf.Clamp01(normalized.y));
            }

            if (gamepad.buttonWest.wasPressedThisFrame)
            {
                PurchaseSelected();
            }
            if (gamepad.buttonEast.wasPressedThisFrame)
            {
                ClosePanel();
            }
        }

        public void CollectControllerCursorTargets(List<ControllerCursorSnapTarget> targets)
        {
            if (!IsOpen || targets == null)
            {
                return;
            }

            for (int i = 0; i < nodeViews.Length; i++)
            {
                UpgradeNodeView view = nodeViews[i];
                if (view == null || !view.gameObject.activeInHierarchy || view.Definition == null) continue;
                targets.Add(new ControllerCursorSnapTarget(
                    view,
                    GetScreenPosition(view.transform as RectTransform),
                    76f));
            }

            AddButtonTarget(targets, purchaseButton, 90f);
            AddButtonTarget(targets, closeButton, 70f);
        }

        private static void AddButtonTarget(
            List<ControllerCursorSnapTarget> targets, Button button, float radius)
        {
            if (button == null || !button.gameObject.activeInHierarchy) return;
            targets.Add(new ControllerCursorSnapTarget(
                button,
                GetScreenPosition(button.transform as RectTransform),
                radius));
        }

        private static Vector2 GetScreenPosition(RectTransform rect)
        {
            if (rect == null) return Vector2.zero;
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            return RectTransformUtility.WorldToScreenPoint(camera, rect.position);
        }

        private void SelectNode(UpgradeNodeView view)
        {
            selectedView = view;
            Refresh();
        }

        private void PurchaseSelected()
        {
            if (selectedView == null)
            {
                return;
            }

            UpgradeNodeState state = system.GetState(selectedView.Definition);
            if (system.TryPurchase(selectedView.Definition))
            {
                PlayUpgradePurchasedSound();
                Refresh();
                return;
            }

            if (state == UpgradeNodeState.InsufficientKills)
            {
                PlayPurchaseFailedSound();
            }

            Refresh();
        }

        private void PlayUpgradePurchasedSound()
        {
            PlayUiClip(upgradePurchasedClip);
        }

        private void PlayPurchaseFailedSound()
        {
            PlayUiClip(purchaseFailedClip);
        }

        private void PlayUiClip(AudioClip clip)
        {
            if (uiAudioSource == null)
            {
                uiAudioSource = GetComponent<AudioSource>();
            }
            if (uiAudioSource == null)
            {
                return;
            }

            AudioClip resolvedClip = clip != null ? clip : uiAudioSource.clip;
            if (resolvedClip == null)
            {
                return;
            }

            uiAudioSource.PlayOneShot(resolvedClip, upgradePurchasedVolume);
        }

        private void Refresh()
        {
            if (system == null)
            {
                return;
            }

            compactKillText.text = $"升级协议  {system.Wallet.AvailableKills}";
            totalKillText.text = $"总击杀  {system.Wallet.TotalKills}";
            availableKillText.text = $"可用击杀点  {system.Wallet.AvailableKills}";

            for (int i = 0; i < nodeViews.Length; i++)
            {
                UpgradeNodeView view = nodeViews[i];
                if (view == null)
                {
                    continue;
                }

                view.SetSelected(view == selectedView);
                view.Refresh(system);
            }

            RefreshDetails();
        }

        private void RefreshDetails()
        {
            UpgradeNodeDefinition node = selectedView != null ? selectedView.Definition : null;
            if (node == null)
            {
                selectedTitle.text = "待接入协议";
                selectedDescription.text = "该 Anchor 分支暂未开放，可在后续直接绑定新的升级节点资产。";
                selectedCost.text = "消耗  --";
                selectedStatus.text = "尚未开放";
                purchaseButton.interactable = false;
                purchaseButtonText.text = "锁定";
                selectedAccent.color = new Color(0.22f, 0.28f, 0.38f);
                return;
            }

            UpgradeNodeState state = system.GetState(node);
            selectedTitle.text = node.DisplayName;
            selectedDescription.text = node.Description;
            selectedCost.text = $"消耗击杀点  {node.KillCost}";
            selectedAccent.color = selectedView.GetBranchColor();
            purchaseButton.interactable = state == UpgradeNodeState.Available || state == UpgradeNodeState.InsufficientKills;

            switch (state)
            {
                case UpgradeNodeState.Purchased:
                    selectedStatus.text = "协议已激活";
                    purchaseButtonText.text = "已完成";
                    break;
                case UpgradeNodeState.Available:
                    selectedStatus.text = "可以激活";
                    purchaseButtonText.text = "激活升级";
                    break;
                case UpgradeNodeState.InsufficientKills:
                    selectedStatus.text = "击杀点不足";
                    purchaseButtonText.text = "资源不足";
                    break;
                case UpgradeNodeState.Locked:
                    selectedStatus.text = "需要先激活前置协议";
                    purchaseButtonText.text = "前置锁定";
                    break;
                default:
                    selectedStatus.text = "尚未开放";
                    purchaseButtonText.text = "锁定";
                    break;
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.GameOver)
            {
                SetPanelOpen(false, false);
            }
        }

        private UpgradeNodeView FindFirstDefinedView()
        {
            for (int i = 0; i < nodeViews.Length; i++)
            {
                if (nodeViews[i] != null && nodeViews[i].Definition != null)
                {
                    return nodeViews[i];
                }
            }

            return null;
        }
    

private void ApplyDetailFonts()
        {
            if (chineseUiFont == null)
            {
                if (selectedTitle != null && selectedTitle.font != null)
                {
                    chineseUiFont = selectedTitle.font;
                }
                else if (selectedDescription != null && selectedDescription.font != null)
                {
                    chineseUiFont = selectedDescription.font;
                }
                else if (selectedCost != null && selectedCost.font != null)
                {
                    chineseUiFont = selectedCost.font;
                }
                else if (selectedStatus != null && selectedStatus.font != null)
                {
                    chineseUiFont = selectedStatus.font;
                }

#if UNITY_EDITOR
                Font editorFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(ChineseUiFontPath);
                if (editorFont != null)
                {
                    chineseUiFont = editorFont;
                }
#endif
            }

            if (chineseUiFont == null)
            {
                return;
            }

            if (selectedTitle != null)
            {
                selectedTitle.font = chineseUiFont;
            }

            if (selectedDescription != null)
            {
                selectedDescription.font = chineseUiFont;
            }

            if (selectedCost != null)
            {
                selectedCost.font = chineseUiFont;
            }

            if (selectedStatus != null)
            {
                selectedStatus.font = chineseUiFont;
            }
        }
}
}
