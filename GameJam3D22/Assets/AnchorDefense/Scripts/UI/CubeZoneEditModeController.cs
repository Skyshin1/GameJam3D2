using UnityEngine;
using UnityEngine.UI;

namespace AnchorDefense
{
    public sealed class CubeZoneEditModeController : MonoBehaviour
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

        public bool IsEditing => editBanner != null && editBanner.activeSelf;

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
            UpgradeTreeController tree, UpgradeSystem upgrades)
        {
            grid = zoneGrid;
            gameFlow = flow;
            upgradeTree = tree;
            upgradeSystem = upgrades;
            openButton.gameObject.SetActive(grid != null);
            if (gameFlow != null) gameFlow.StateChanged += HandleGameStateChanged;
            if (upgradeSystem != null) upgradeSystem.Changed += RefreshFragmentSources;
            for (int i = 0; i < fragmentSources.Length; i++) fragmentSources[i]?.Bind(this);
            RefreshFragmentSources();
            grid?.SetEditMode(false);
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
