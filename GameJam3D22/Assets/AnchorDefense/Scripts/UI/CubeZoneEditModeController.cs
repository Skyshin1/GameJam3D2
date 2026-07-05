using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AnchorDefense
{
    public sealed class CubeZoneEditModeController : MonoBehaviour, IControllerCursorContext
    {
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject editBanner;
        [SerializeField] private GameObject sidebarRoot;
        [SerializeField] private RectTransform sidebarRect;
        [SerializeField] private ZoneEffectWorldDragSource[] fragmentSources;
        [SerializeField] private GameObject tooltipRoot;
        [SerializeField] private Text tooltipTitle;
        [SerializeField] private Text tooltipDescription;
        [SerializeField] private Text tooltipStatus;

        private CubeZoneGridController grid;
        private GameFlowController gameFlow;
        private UpgradeTreeController upgradeTree;
        private UpgradeSystem upgradeSystem;
        private GameInputController input;

        public bool IsEditing => editBanner != null && editBanner.activeSelf;
        public bool IsControllerCursorActive => IsEditing;

        public void Configure(Button open, Button close, GameObject banner,
            GameObject sidebar, ZoneEffectWorldDragSource[] sources,
            GameObject tooltip, Text tooltipName, Text tooltipBody, Text tooltipState)
        {
            openButton = open;
            closeButton = close;
            editBanner = banner;
            sidebarRoot = sidebar;
            sidebarRect = sidebar != null ? sidebar.transform as RectTransform : null;
            fragmentSources = sources;
            tooltipRoot = tooltip;
            tooltipTitle = tooltipName;
            tooltipDescription = tooltipBody;
            tooltipStatus = tooltipState;
        }

        public void Initialize(CubeZoneGridController zoneGrid, GameFlowController flow,
            UpgradeTreeController tree, UpgradeSystem upgrades, GameInputController inputController)
        {
            grid = zoneGrid;
            gameFlow = flow;
            upgradeTree = tree;
            upgradeSystem = upgrades;
            input = inputController;
            openButton.gameObject.SetActive(grid != null);
            if (gameFlow != null) gameFlow.StateChanged += HandleGameStateChanged;
            if (upgradeSystem != null) upgradeSystem.Changed += RefreshFragmentSources;
            for (int i = 0; i < fragmentSources.Length; i++) fragmentSources[i]?.Bind(this);
            RefreshFragmentSources();
            grid?.SetEditMode(false);
            Text[] bannerTexts = editBanner.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < bannerTexts.Length; i++)
            {
                if (bannerTexts[i] != null && bannerTexts[i].name == "Edit Mode Hint")
                {
                    bannerTexts[i].text = "游戏已暂停 · 右摇杆移动磁吸光标 · 按住 A 拖动碎片或方块 · B 完成";
                    break;
                }
            }
        }

        private void Update()
        {
            if (IsEditing && Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
            {
                ExitEditMode();
                return;
            }

            if (IsEditing)
            {
                HandleLayerRotationInput();
            }

            if (input == null || input.ToggleZoneEdit == null ||
                !input.ToggleZoneEdit.WasPressedThisFrame()) return;

            if (IsEditing) ExitEditMode();
            else EnterEditMode();
        }

        private void HandleLayerRotationInput()
        {
            if (grid == null || grid.IsDragging || Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.aKey.wasPressedThisFrame)
            {
                grid.TryRotateSelectedHorizontalLayer(-1);
            }
            else if (Keyboard.current.dKey.wasPressedThisFrame)
            {
                grid.TryRotateSelectedHorizontalLayer(1);
            }
            else if (Keyboard.current.zKey.wasPressedThisFrame)
            {
                grid.TryRotateSelectedDepthLayer(-1);
            }
            else if (Keyboard.current.cKey.wasPressedThisFrame)
            {
                grid.TryRotateSelectedDepthLayer(1);
            }
            else if (Keyboard.current.sKey.wasPressedThisFrame)
            {
                grid.TryRotateSelectedWidthLayer(1);
            }
            else if (Keyboard.current.xKey.wasPressedThisFrame)
            {
                grid.TryRotateSelectedWidthLayer(-1);
            }
        }

        public void ShowFragmentTooltip(CubeZoneEffectDefinition effect, bool unlocked)
        {
            if (effect == null || tooltipRoot == null) return;
            tooltipTitle.text = effect.DisplayName;
            tooltipDescription.text = effect.Description;
            tooltipStatus.text = unlocked ? "已解锁 · 可拖到区域方块" : "未解锁 · 请先激活对应技能树节点";
            tooltipStatus.color = unlocked ? new Color(0.35f, 1f, 0.68f) : new Color(1f, 0.48f, 0.25f);
            tooltipRoot.SetActive(true);
        }

        public void HideFragmentTooltip()
        {
            tooltipRoot?.SetActive(false);
        }

        public void AssignFragmentAtPointer(CubeZoneEffectDefinition effect, Vector2 pointerPosition)
        {
            if (!IsEditing || effect == null || !IsEffectUnlocked(effect) || grid == null) return;
            if (sidebarRect != null && RectTransformUtility.RectangleContainsScreenPoint(sidebarRect, pointerPosition)) return;
            CubeZoneVolume cube = grid.FindCubeUnderPointer(Camera.main, pointerPosition);
            if (cube == null) return;
            grid.AssignEffect(cube.CubeId, effect);
            grid.SelectCubeById(cube.CubeId);
        }

        public void ExitFromExternal()
        {
            SetEditing(false, true);
        }

        private void Awake()
        {
            openButton.onClick.AddListener(EnterEditMode);
            closeButton.onClick.AddListener(ExitEditMode);
            editBanner.SetActive(false);
            sidebarRoot.SetActive(false);
            tooltipRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            bool wasEditing = IsEditing;
            if (openButton != null) openButton.onClick.RemoveListener(EnterEditMode);
            if (closeButton != null) closeButton.onClick.RemoveListener(ExitEditMode);
            if (gameFlow != null) gameFlow.StateChanged -= HandleGameStateChanged;
            if (upgradeSystem != null) upgradeSystem.Changed -= RefreshFragmentSources;
            grid?.SetEditMode(false);
            if (wasEditing) Time.timeScale = 1f;
            GamepadVirtualCursorController.ClearContext(this);
        }

        private void EnterEditMode()
        {
            if (gameFlow == null || !gameFlow.IsPlaying || grid == null) return;
            upgradeTree?.CloseFromExternal();
            SetEditing(true, true);
        }

        private void ExitEditMode()
        {
            SetEditing(false, true);
        }

        private void SetEditing(bool editing, bool updateTimeScale)
        {
            editBanner.SetActive(editing);
            sidebarRoot.SetActive(editing);
            if (!editing) HideFragmentTooltip();
            openButton.gameObject.SetActive(!editing && grid != null);
            grid?.SetEditMode(editing);
            if (editing)
            {
                GamepadVirtualCursorController.SetContext(this);
            }
            else
            {
                GamepadVirtualCursorController.ClearContext(this);
            }
            if (updateTimeScale && gameFlow != null && gameFlow.IsPlaying)
            {
                Time.timeScale = editing ? 0f : 1f;
            }
        }

        private void RefreshFragmentSources()
        {
            for (int i = 0; i < fragmentSources.Length; i++)
            {
                ZoneEffectWorldDragSource source = fragmentSources[i];
                if (source != null) source.SetUnlocked(IsEffectUnlocked(source.Definition));
            }
        }

        private bool IsEffectUnlocked(CubeZoneEffectDefinition effect)
        {
            if (effect == null || effect.UnlockRequirement == null) return true;
            return upgradeSystem != null &&
                   upgradeSystem.GetState(effect.UnlockRequirement) == UpgradeNodeState.Purchased;
        }

        public void CollectControllerCursorTargets(List<ControllerCursorSnapTarget> targets)
        {
            if (!IsEditing || targets == null)
            {
                return;
            }

            for (int i = 0; i < fragmentSources.Length; i++)
            {
                ZoneEffectWorldDragSource source = fragmentSources[i];
                if (source == null || !source.gameObject.activeInHierarchy) continue;
                RectTransform rect = source.transform as RectTransform;
                targets.Add(new ControllerCursorSnapTarget(
                    source,
                    GetScreenPosition(rect),
                    95f));
            }

            Camera camera = Camera.main;
            if (grid == null || camera == null) return;
            for (int i = 0; i < CubeZoneConfig.ZoneCount; i++)
            {
                CubeZoneVolume cube = grid.GetCubeById(i);
                if (cube == null || !cube.gameObject.activeInHierarchy) continue;
                Vector3 screen = camera.WorldToScreenPoint(cube.transform.position);
                if (screen.z <= 0f) continue;
                targets.Add(new ControllerCursorSnapTarget(
                    cube,
                    new Vector2(screen.x, screen.y),
                    125f));
            }
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

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.GameOver)
            {
                SetEditing(false, false);
                openButton.gameObject.SetActive(false);
            }
        }
    }
}
