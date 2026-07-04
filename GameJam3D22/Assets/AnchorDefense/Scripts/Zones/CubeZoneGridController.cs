using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnchorDefense
{
    public sealed class CubeZoneGridController : MonoBehaviour
    {
        public const int ZoneRaycastLayer = 2;

        private static readonly Vector3Int[] NeighborDirections =
        {
            Vector3Int.right, Vector3Int.left, Vector3Int.up,
            Vector3Int.down, new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)
        };
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private CubeZoneConfig config;
        [SerializeField] private CubeZoneVolume[] zoneVolumes;
        [Header("Drag Placement Hints")]
        [SerializeField] private Transform[] dropHintTransforms;
        [SerializeField, Min(1f)] private float dropSnapScreenPadding = 1.25f;

        private readonly CubeZoneEffectDefinition[] cubeEffects =
            new CubeZoneEffectDefinition[CubeZoneConfig.ZoneCount];
        private readonly CubeZoneVolume[] cubeById =
            new CubeZoneVolume[CubeZoneConfig.ZoneCount];
        private readonly List<Vector3Int> candidatePositions = new List<Vector3Int>(48);
        private readonly HashSet<Vector3Int> occupiedPositions = new HashSet<Vector3Int>();
        private readonly HashSet<Vector3Int> uniqueCandidates = new HashSet<Vector3Int>();
        private MaterialPropertyBlock hintPropertyBlock;
        private EnemyRegistry enemyRegistry;
        private TurretRegistry turretRegistry;
        private CubeZoneVolume selectedCube;
        private bool initialized;
        private bool isDragging;
        private int hoveredHintIndex = -1;
        private CubeZoneVolume hoveredSwapCube;

        public CubeZoneConfig Config => config;
        public CubeZoneVolume SelectedCube => selectedCube;
        public bool IsDragging => isDragging;
        public event Action<int, CubeZoneEffectDefinition> AssignmentChanged;
        public event Action<CubeZoneVolume> SelectionChanged;
        public event Action LayoutChanged;

        public void Configure(CubeZoneConfig zoneConfig, CubeZoneVolume[] volumes, Transform[] hints = null)
        {
            config = zoneConfig;
            zoneVolumes = volumes;
            dropHintTransforms = hints;
        }

        public void Initialize(EnemyRegistry enemies, TurretRegistry turrets, Transform core)
        {
            enemyRegistry = enemies;
            turretRegistry = turrets;
            if (core != null) transform.position = core.position;

            Array.Clear(cubeById, 0, cubeById.Length);
            if (zoneVolumes != null)
            {
                for (int i = 0; i < zoneVolumes.Length; i++)
                {
                    CubeZoneVolume cube = zoneVolumes[i];
                    if (cube == null) continue;
                    int id = Mathf.Clamp(cube.CubeId, 0, CubeZoneConfig.ZoneCount - 1);
                    cubeById[id] = cube;
                    cubeEffects[id] = config != null ? config.GetDefaultEffect(id) : null;
                    cube.transform.localPosition = GetGridLocalPosition(cube.GridPosition);
                    cube.SetEffect(cubeEffects[id]);
                    cube.EnsureLocalVfx();
                }
            }

            SetHintsVisible(false);
            initialized = config != null && enemyRegistry != null && turretRegistry != null;
            SelectCubeById(0);
        }

        public CubeZoneVolume GetCubeById(int cubeId)
        {
            return cubeId >= 0 && cubeId < cubeById.Length ? cubeById[cubeId] : null;
        }

        // 保留旧接口，避免已有 UI / 测试和后续外部脚本立即失效；参数现在代表稳定的 CubeId。
        public CubeZoneVolume GetCubeAtSlot(int cubeId) => GetCubeById(cubeId);

        public CubeZoneEffectDefinition GetAssignedEffect(int cubeId)
        {
            return cubeId >= 0 && cubeId < cubeEffects.Length ? cubeEffects[cubeId] : null;
        }

        public void AssignEffect(int cubeId, CubeZoneEffectDefinition effect)
        {
            CubeZoneVolume cube = GetCubeById(cubeId);
            if (cube == null) return;
            cubeEffects[cubeId] = effect;
            cube.SetEffect(effect);
            AssignmentChanged?.Invoke(cubeId, effect);
        }

        public void SelectCubeById(int cubeId) => SelectCube(GetCubeById(cubeId));
        public void SelectCubeAtSlot(int cubeId) => SelectCubeById(cubeId);

        public bool TryBeginPointerInteraction(Camera camera, Vector2 pointerPosition)
        {
            if (camera == null) return false;
            Ray ray = camera.ScreenPointToRay(pointerPosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 300f, 1 << ZoneRaycastLayer,
                QueryTriggerInteraction.Collide);
            CubeZoneVolume cube = null;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < hits.Length; i++)
            {
                CubeZoneVolume hitCube = hits[i].collider.GetComponentInParent<CubeZoneVolume>();
                if (hitCube != null && hits[i].distance < nearest)
                {
                    cube = hitCube;
                    nearest = hits[i].distance;
                }
            }
            if (cube == null) return false;

            SelectCube(cube);
            isDragging = true;
            hoveredHintIndex = -1;
            hoveredSwapCube = null;
            BuildEmptyNeighborCandidates();
            ShowCandidateHints();
            return true;
        }

        public void UpdatePointerInteraction(Camera camera, Vector2 pointerPosition)
        {
            if (!isDragging || selectedCube == null || camera == null) return;

            int previousHint = hoveredHintIndex;
            CubeZoneVolume previousSwap = hoveredSwapCube;
            FindDropTarget(camera, pointerPosition, out hoveredHintIndex, out hoveredSwapCube);
            if (previousHint != hoveredHintIndex || previousSwap != hoveredSwapCube)
            {
                previousSwap?.SetDropCandidate(false);
                hoveredSwapCube?.SetDropCandidate(true);
                RefreshHintVisuals();
            }
        }

        public void EndPointerInteraction()
        {
            if (!isDragging) return;
            if (selectedCube != null && hoveredSwapCube != null)
            {
                SwapCubePositions(selectedCube, hoveredSwapCube);
            }
            else if (selectedCube != null && hoveredHintIndex >= 0 && hoveredHintIndex < candidatePositions.Count)
            {
                MoveSelectedCube(candidatePositions[hoveredHintIndex]);
            }
            hoveredSwapCube?.SetDropCandidate(false);
            isDragging = false;
            hoveredHintIndex = -1;
            hoveredSwapCube = null;
            SetHintsVisible(false);
        }

        private void FindDropTarget(Camera camera, Vector2 pointerPosition,
            out int hintResult, out CubeZoneVolume swapResult)
        {
            hintResult = -1;
            swapResult = null;
            Ray ray = camera.ScreenPointToRay(pointerPosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 500f, 1 << ZoneRaycastLayer,
                QueryTriggerInteraction.Collide);
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < hits.Length; i++)
            {
                int index = FindHintIndex(hits[i].collider.transform);
                if (index >= 0 && hits[i].distance < nearest)
                {
                    nearest = hits[i].distance;
                    hintResult = index;
                    swapResult = null;
                    continue;
                }
                CubeZoneVolume hitCube = hits[i].collider.GetComponentInParent<CubeZoneVolume>();
                if (hitCube != null && hitCube != selectedCube && hits[i].distance < nearest)
                {
                    nearest = hits[i].distance;
                    hintResult = -1;
                    swapResult = hitCube;
                }
            }
            if (hintResult >= 0 || swapResult != null) return;

            // 屏幕空间兜底：按虚影投影后的实际半径判断，而不是只认固定的中心小圆。
            float bestNormalizedDistance = 1f;
            float cubeSize = config != null ? config.GridHalfExtent : 10.5f;
            for (int i = 0; i < candidatePositions.Count && i < dropHintTransforms.Length; i++)
            {
                Transform hint = dropHintTransforms[i];
                Vector3 center = camera.WorldToScreenPoint(hint.position);
                if (center.z <= 0f) continue;
                Vector3 rightEdge = camera.WorldToScreenPoint(hint.position + camera.transform.right * cubeSize * 0.5f);
                Vector3 upEdge = camera.WorldToScreenPoint(hint.position + camera.transform.up * cubeSize * 0.5f);
                float radius = Mathf.Max(Vector2.Distance(center, rightEdge), Vector2.Distance(center, upEdge));
                radius = Mathf.Max(80f, radius * dropSnapScreenPadding);
                float normalized = Vector2.Distance(pointerPosition, center) / radius;
                if (normalized < bestNormalizedDistance)
                {
                    bestNormalizedDistance = normalized;
                    hintResult = i;
                }
            }
        }

        private int FindHintIndex(Transform hitTransform)
        {
            if (dropHintTransforms == null) return -1;
            for (int i = 0; i < candidatePositions.Count && i < dropHintTransforms.Length; i++)
            {
                Transform hint = dropHintTransforms[i];
                if (hint != null && hint.gameObject.activeInHierarchy &&
                    (hitTransform == hint || hitTransform.IsChildOf(hint))) return i;
            }
            return -1;
        }

        private void BuildEmptyNeighborCandidates()
        {
            occupiedPositions.Clear();
            uniqueCandidates.Clear();
            candidatePositions.Clear();
            for (int i = 0; i < cubeById.Length; i++)
            {
                CubeZoneVolume cube = cubeById[i];
                if (cube != null && cube != selectedCube) occupiedPositions.Add(cube.GridPosition);
            }
            foreach (Vector3Int occupied in occupiedPositions)
            {
                for (int i = 0; i < NeighborDirections.Length; i++)
                {
                    Vector3Int candidate = occupied + NeighborDirections[i];
                    if (!occupiedPositions.Contains(candidate)) uniqueCandidates.Add(candidate);
                }
            }
            candidatePositions.AddRange(uniqueCandidates);
            candidatePositions.Sort(CompareGridPositions);
        }

        private static int CompareGridPositions(Vector3Int a, Vector3Int b)
        {
            int y = a.y.CompareTo(b.y);
            if (y != 0) return y;
            int z = a.z.CompareTo(b.z);
            return z != 0 ? z : a.x.CompareTo(b.x);
        }

        private void ShowCandidateHints()
        {
            if (dropHintTransforms == null) return;
            for (int i = 0; i < dropHintTransforms.Length; i++)
            {
                Transform hint = dropHintTransforms[i];
                if (hint == null) continue;
                bool active = i < candidatePositions.Count;
                hint.gameObject.SetActive(active);
                if (active) hint.localPosition = GetGridLocalPosition(candidatePositions[i]);
            }
            RefreshHintVisuals();
        }

        private void RefreshHintVisuals()
        {
            if (dropHintTransforms == null) return;
            hintPropertyBlock ??= new MaterialPropertyBlock();
            for (int i = 0; i < candidatePositions.Count && i < dropHintTransforms.Length; i++)
            {
                Renderer renderer = dropHintTransforms[i] != null
                    ? dropHintTransforms[i].GetComponent<Renderer>() : null;
                if (renderer == null) continue;
                Color color = i == hoveredHintIndex
                    ? new Color(0.25f, 1f, 0.65f, 0.34f)
                    : new Color(0.2f, 0.78f, 1f, 0.15f);
                renderer.GetPropertyBlock(hintPropertyBlock);
                hintPropertyBlock.SetColor(BaseColorId, color);
                hintPropertyBlock.SetColor(ColorId, color);
                renderer.SetPropertyBlock(hintPropertyBlock);
            }
        }

        private void MoveSelectedCube(Vector3Int targetPosition)
        {
            if (selectedCube == null || IsOccupied(targetPosition, selectedCube) ||
                !IsAdjacentToAnotherCube(targetPosition, selectedCube)) return;
            selectedCube.SetGridPosition(targetPosition);
            selectedCube.transform.localPosition = GetGridLocalPosition(targetPosition);
            LayoutChanged?.Invoke();
        }

        private void SwapCubePositions(CubeZoneVolume first, CubeZoneVolume second)
        {
            if (first == null || second == null || first == second) return;
            Vector3Int firstPosition = first.GridPosition;
            Vector3Int secondPosition = second.GridPosition;
            first.SetGridPosition(secondPosition);
            second.SetGridPosition(firstPosition);
            first.transform.localPosition = GetGridLocalPosition(secondPosition);
            second.transform.localPosition = GetGridLocalPosition(firstPosition);
            LayoutChanged?.Invoke();
        }

        private bool IsOccupied(Vector3Int position, CubeZoneVolume ignored)
        {
            for (int i = 0; i < cubeById.Length; i++)
            {
                CubeZoneVolume cube = cubeById[i];
                if (cube != null && cube != ignored && cube.GridPosition == position) return true;
            }
            return false;
        }

        private bool IsAdjacentToAnotherCube(Vector3Int position, CubeZoneVolume ignored)
        {
            for (int i = 0; i < NeighborDirections.Length; i++)
            {
                if (IsOccupied(position + NeighborDirections[i], ignored)) return true;
            }
            return false;
        }

        private void SelectCube(CubeZoneVolume cube)
        {
            if (selectedCube != cube)
            {
                selectedCube?.SetSelected(false);
                selectedCube = cube;
                selectedCube?.SetSelected(true);
            }
            SelectionChanged?.Invoke(selectedCube);
        }

        private void Update()
        {
            if (!initialized) return;
            ApplyTurretEffects();
            ApplyEnemyEffects();
        }

        private void ApplyTurretEffects()
        {
            var turrets = turretRegistry.Turrets;
            for (int i = 0; i < turrets.Count; i++)
            {
                TurretHealth health = turrets[i];
                if (health == null) continue;
                CubeZoneEffectDefinition effect = GetEffectAtPosition(health.transform.position);
                TurretController turret = health.GetComponent<TurretController>();
                turret?.SetZoneFireIntervalMultiplier(effect != null && effect.EffectType == CubeZoneEffectType.TurretFireRateBoost
                    ? effect.TurretFireIntervalMultiplier : 1f);
                turret?.SetZoneDamageMultiplier(effect != null && effect.EffectType == CubeZoneEffectType.TurretDamageBoost
                    ? effect.TurretDamageMultiplier : 1f);
            }
        }

        private void ApplyEnemyEffects()
        {
            var enemies = enemyRegistry.ActiveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyController enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive) continue;
                CubeZoneEffectDefinition effect = GetEffectAtPosition(enemy.transform.position);
                if (effect != null && effect.EffectType == CubeZoneEffectType.EnemySlowAndDamage)
                    enemy.SetZoneEffect(effect.EnemySpeedMultiplier, effect.EnemyDamagePerSecond);
                else enemy.SetZoneEffect(1f, 0f);
            }
        }

        private CubeZoneEffectDefinition GetEffectAtPosition(Vector3 worldPosition)
        {
            int cubeId = GetZoneIndex(worldPosition);
            return cubeId >= 0 ? GetAssignedEffect(cubeId) : null;
        }

        public int GetZoneIndex(Vector3 worldPosition)
        {
            for (int i = 0; i < cubeById.Length; i++)
            {
                if (cubeById[i] != null && cubeById[i].Contains(worldPosition)) return cubeById[i].CubeId;
            }
            return -1;
        }

        private Vector3 GetGridLocalPosition(Vector3Int gridPosition)
        {
            float cubeSize = config != null ? config.GridHalfExtent : 10.5f;
            return new Vector3((gridPosition.x - 0.5f) * cubeSize,
                (gridPosition.y - 0.5f) * cubeSize,
                (gridPosition.z - 0.5f) * cubeSize);
        }

        private void SetHintsVisible(bool visible)
        {
            if (dropHintTransforms == null) return;
            for (int i = 0; i < dropHintTransforms.Length; i++)
            {
                if (dropHintTransforms[i] != null) dropHintTransforms[i].gameObject.SetActive(visible);
            }
        }
    }
}
