using UnityEngine;
using UnityEngine.UI;

namespace AnchorDefense
{
    public sealed class CubeZoneAssignmentController : MonoBehaviour
    {
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private ZoneEffectDragSource[] effectSources;
        [SerializeField] private ZoneLayoutMarker[] layoutMarkers;
        [SerializeField] private ZoneAssignmentDropTarget selectedTarget;
        [SerializeField] private Text selectedDetails;

        private CubeZoneGridController grid;
        private UpgradeSystem upgradeSystem;
        private int selectedSlot;

        public void Configure(Button open, Button close, GameObject panel,
            ZoneEffectDragSource[] sources, ZoneLayoutMarker[] markers,
            ZoneAssignmentDropTarget target, Text details)
        {
            openButton = open;
            closeButton = close;
            panelRoot = panel;
            effectSources = sources;
            layoutMarkers = markers;
            selectedTarget = target;
            selectedDetails = details;
        }

        public void Initialize(CubeZoneGridController zoneGrid, UpgradeSystem upgrades)
        {
            grid = zoneGrid;
            upgradeSystem = upgrades;
            bool available = grid != null;
            openButton.gameObject.SetActive(available);
            panelRoot.SetActive(false);
            if (!available)
            {
                return;
            }

            grid.AssignmentChanged += HandleAssignmentChanged;
            grid.SelectionChanged += HandleWorldSelectionChanged;
            grid.LayoutChanged += RefreshAll;
            if (upgradeSystem != null) upgradeSystem.Changed += RefreshUnlocks;

            for (int i = 0; i < effectSources.Length; i++)
            {
                effectSources[i]?.Bind(this);
            }
            for (int i = 0; i < layoutMarkers.Length; i++)
            {
                layoutMarkers[i]?.Bind(this);
            }
            selectedTarget?.Bind(this);
            selectedSlot = grid.SelectedCube != null ? grid.SelectedCube.SlotIndex : 0;
            RefreshUnlocks();
            RefreshAll();
        }

        public void SelectSlot(int slotIndex)
        {
            selectedSlot = Mathf.Clamp(slotIndex, 0, CubeZoneConfig.ZoneCount - 1);
            grid?.SelectCubeAtSlot(selectedSlot);
            RefreshSelection();
        }

        public void Assign(int slotIndex, CubeZoneEffectDefinition effect)
        {
            if (IsEffectUnlocked(effect))
            {
                grid?.AssignEffect(slotIndex, effect);
            }
        }

        public void AssignSelected(CubeZoneEffectDefinition effect)
        {
            Assign(selectedSlot, effect);
        }

        private void Awake()
        {
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(Close);
            panelRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (openButton != null) openButton.onClick.RemoveListener(Open);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (grid != null)
            {
                grid.AssignmentChanged -= HandleAssignmentChanged;
                grid.SelectionChanged -= HandleWorldSelectionChanged;
                grid.LayoutChanged -= RefreshAll;
            }
            if (upgradeSystem != null) upgradeSystem.Changed -= RefreshUnlocks;
        }

        private void Open()
        {
            panelRoot.SetActive(true);
            RefreshAll();
        }

        private void Close() => panelRoot.SetActive(false);

        private void HandleAssignmentChanged(int slotIndex, CubeZoneEffectDefinition effect)
        {
            RefreshAll();
        }

        private void HandleWorldSelectionChanged(CubeZoneVolume cube)
        {
            if (cube == null)
            {
                return;
            }
            selectedSlot = cube.SlotIndex;
            RefreshSelection();
        }

        private void RefreshUnlocks()
        {
            for (int i = 0; i < effectSources.Length; i++)
            {
                ZoneEffectDragSource source = effectSources[i];
                if (source != null) source.SetUnlocked(IsEffectUnlocked(source.Definition));
            }
        }

        private bool IsEffectUnlocked(CubeZoneEffectDefinition effect)
        {
            if (effect == null || effect.UnlockRequirement == null)
            {
                return true;
            }
            return upgradeSystem != null &&
                   upgradeSystem.GetState(effect.UnlockRequirement) == UpgradeNodeState.Purchased;
        }

        private void RefreshAll()
        {
            if (grid == null)
            {
                return;
            }
            for (int i = 0; i < layoutMarkers.Length; i++)
            {
                layoutMarkers[i]?.Refresh(grid.GetCubeAtSlot(i), grid.GetAssignedEffect(i), i == selectedSlot);
            }
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            if (grid == null)
            {
                return;
            }
            CubeZoneVolume cube = grid.GetCubeAtSlot(selectedSlot);
            CubeZoneEffectDefinition effect = grid.GetAssignedEffect(selectedSlot);
            selectedTarget?.SetZoneIndex(selectedSlot);
            selectedTarget?.Refresh(effect);
            if (selectedDetails != null)
            {
                selectedDetails.text = cube == null
                    ? "未选中区域立方体"
                    : $"立方体 C{cube.CubeId + 1:00}  /  当前格位 {selectedSlot + 1}\n" +
                      (effect != null ? effect.Description : "尚未配置区域效果");
            }
            for (int i = 0; i < layoutMarkers.Length; i++)
            {
                if (layoutMarkers[i] != null) layoutMarkers[i].SetSelected(i == selectedSlot);
            }
        }
    }
}
