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
        private int selectedCubeId;

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
            selectedCubeId = grid.SelectedCube != null ? grid.SelectedCube.CubeId : 0;
            RefreshUnlocks();
            RefreshAll();
        }

        public void SelectSlot(int cubeId)
        {
            selectedCubeId = Mathf.Clamp(cubeId, 0, CubeZoneConfig.ZoneCount - 1);
            grid?.SelectCubeById(selectedCubeId);
            RefreshSelection();
        }

        public void Assign(int cubeId, CubeZoneEffectDefinition effect)
        {
            if (IsEffectUnlocked(effect))
            {
                grid?.AssignEffect(cubeId, effect);
            }
        }

        public void AssignSelected(CubeZoneEffectDefinition effect)
        {
            Assign(selectedCubeId, effect);
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
            selectedCubeId = cube.CubeId;
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
                layoutMarkers[i]?.Refresh(grid.GetCubeById(i), grid.GetAssignedEffect(i), i == selectedCubeId);
            }
            RefreshTopologyPositions();
            RefreshSelection();
        }

        private void RefreshTopologyPositions()
        {
            Vector2[] projected = new Vector2[CubeZoneConfig.ZoneCount];
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            for (int i = 0; i < projected.Length; i++)
            {
                CubeZoneVolume cube = grid.GetCubeById(i);
                Vector3Int p = cube != null ? cube.GridPosition : Vector3Int.zero;
                projected[i] = new Vector2((p.x - p.z) * 112f, p.y * 118f + (p.x + p.z) * 46f);
                min = Vector2.Min(min, projected[i]);
                max = Vector2.Max(max, projected[i]);
            }
            Vector2 size = max - min;
            float scale = Mathf.Min(1f, Mathf.Min(500f / Mathf.Max(126f, size.x + 126f),
                530f / Mathf.Max(126f, size.y + 126f)));
            Vector2 center = (min + max) * 0.5f;
            for (int i = 0; i < layoutMarkers.Length; i++)
            {
                layoutMarkers[i]?.SetTopologyPosition((projected[i] - center) * scale);
            }
        }

        private void RefreshSelection()
        {
            if (grid == null)
            {
                return;
            }
            CubeZoneVolume cube = grid.GetCubeById(selectedCubeId);
            CubeZoneEffectDefinition effect = grid.GetAssignedEffect(selectedCubeId);
            selectedTarget?.SetZoneIndex(selectedCubeId);
            selectedTarget?.Refresh(effect);
            if (selectedDetails != null)
            {
                selectedDetails.text = cube == null
                    ? "未选中区域立方体"
                    : $"立方体 C{cube.CubeId + 1:00}  /  坐标 {cube.GridPosition}\n" +
                      (effect != null ? effect.Description : "尚未配置区域效果");
            }
            for (int i = 0; i < layoutMarkers.Length; i++)
            {
                if (layoutMarkers[i] != null) layoutMarkers[i].SetSelected(i == selectedCubeId);
            }
        }
    }
}
