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
        [SerializeField] private ZoneAssignmentDropTarget[] zoneTargets;
        private CubeZoneGridController grid;

        public void Configure(Button open, Button close, GameObject panel,
            ZoneEffectDragSource[] sources, ZoneAssignmentDropTarget[] targets)
        {
            openButton = open;
            closeButton = close;
            panelRoot = panel;
            effectSources = sources;
            zoneTargets = targets;
        }

        public void Initialize(CubeZoneGridController zoneGrid)
        {
            grid = zoneGrid;
            bool available = grid != null;
            openButton.gameObject.SetActive(available);
            panelRoot.SetActive(false);
            if (!available)
            {
                return;
            }
            grid.AssignmentChanged += HandleAssignmentChanged;
            for (int i = 0; i < zoneTargets.Length; i++)
            {
                zoneTargets[i]?.Bind(this);
                zoneTargets[i]?.Refresh(grid.GetAssignedEffect(zoneTargets[i].ZoneIndex));
            }
        }

        public void Assign(int zoneIndex, CubeZoneEffectDefinition effect)
        {
            grid?.AssignEffect(zoneIndex, effect);
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
            if (grid != null) grid.AssignmentChanged -= HandleAssignmentChanged;
        }

        private void Open() => panelRoot.SetActive(true);
        private void Close() => panelRoot.SetActive(false);

        private void HandleAssignmentChanged(int zoneIndex, CubeZoneEffectDefinition effect)
        {
            for (int i = 0; i < zoneTargets.Length; i++)
            {
                if (zoneTargets[i] != null && zoneTargets[i].ZoneIndex == zoneIndex)
                {
                    zoneTargets[i].Refresh(effect);
                    return;
                }
            }
        }
    }
}
