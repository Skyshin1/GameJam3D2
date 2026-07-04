using System;
using UnityEngine;

namespace AnchorDefense
{
    public sealed class CubeZoneGridController : MonoBehaviour
    {
        public const int ZoneRaycastLayer = 2;

        [SerializeField] private CubeZoneConfig config;
        [SerializeField] private CubeZoneVolume[] zoneVolumes;
        [Header("Drag Placement Hints")]
        [SerializeField] private Transform[] dropHintTransforms;
        [SerializeField, Min(30f)] private float dropSnapScreenRadius = 150f;

        private readonly CubeZoneEffectDefinition[] cubeEffects =
            new CubeZoneEffectDefinition[CubeZoneConfig.ZoneCount];
        private readonly CubeZoneVolume[] slotToCube =
            new CubeZoneVolume[CubeZoneConfig.ZoneCount];
        private readonly int[] adjacentSlots = new int[3];
        private EnemyRegistry enemyRegistry;
        private TurretRegistry turretRegistry;
        private CubeZoneVolume selectedCube;
        private bool initialized;
        private bool isDragging;
        private int hoveredDropSlot = -1;

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
            if (core != null)
            {
                transform.position = core.position;
            }

            Array.Clear(slotToCube, 0, slotToCube.Length);
            if (zoneVolumes != null)
            {
                for (int i = 0; i < zoneVolumes.Length; i++)
                {
                    CubeZoneVolume cube = zoneVolumes[i];
                    if (cube == null)
                    {
                        continue;
                    }

                    int slot = Mathf.Clamp(cube.SlotIndex, 0, CubeZoneConfig.ZoneCount - 1);
                    int id = Mathf.Clamp(cube.CubeId, 0, CubeZoneConfig.ZoneCount - 1);
                    slotToCube[slot] = cube;
                    cubeEffects[id] = config != null ? config.GetDefaultEffect(slot) : null;
                    cube.transform.localPosition = GetSlotLocalPosition(slot);
                    cube.SetEffect(cubeEffects[id]);
                    cube.EnsureLocalVfx();
                }
            }

            SetHintsVisible(false);
            initialized = config != null && enemyRegistry != null && turretRegistry != null;
            SelectCubeAtSlot(0);
        }

        public CubeZoneVolume GetCubeAtSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < slotToCube.Length ? slotToCube[slotIndex] : null;
        }

        public CubeZoneEffectDefinition GetAssignedEffect(int slotIndex)
        {
            CubeZoneVolume cube = GetCubeAtSlot(slotIndex);
            return cube != null ? cubeEffects[cube.CubeId] : null;
        }

        public void AssignEffect(int slotIndex, CubeZoneEffectDefinition effect)
        {
            CubeZoneVolume cube = GetCubeAtSlot(slotIndex);
            if (cube == null)
            {
                return;
            }

            cubeEffects[cube.CubeId] = effect;
            cube.SetEffect(effect);
            AssignmentChanged?.Invoke(slotIndex, effect);
        }

        public void SelectCubeAtSlot(int slotIndex)
        {
            SelectCube(GetCubeAtSlot(slotIndex));
        }

        public bool TryBeginPointerInteraction(Camera camera, Vector2 pointerPosition)
        {
            if (camera == null)
            {
                return false;
            }

            Ray ray = camera.ScreenPointToRay(pointerPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 200f, 1 << ZoneRaycastLayer,
                    QueryTriggerInteraction.Collide))
            {
                return false;
            }

            CubeZoneVolume cube = hit.collider.GetComponentInParent<CubeZoneVolume>();
            if (cube == null)
            {
                return false;
            }

            SelectCube(cube);
            isDragging = true;
            hoveredDropSlot = -1;
            ShowAdjacentHints(cube.SlotIndex);
            return true;
        }

        public void UpdatePointerInteraction(Camera camera, Vector2 pointerPosition)
        {
            if (!isDragging || selectedCube == null || camera == null)
            {
                return;
            }

            int previous = hoveredDropSlot;
            hoveredDropSlot = -1;
            float closest = dropSnapScreenRadius;
            BuildAdjacentSlots(selectedCube.SlotIndex);
            for (int i = 0; i < adjacentSlots.Length; i++)
            {
                int slot = adjacentSlots[i];
                Vector3 screen = camera.WorldToScreenPoint(transform.TransformPoint(GetSlotLocalPosition(slot)));
                if (screen.z <= 0f)
                {
                    continue;
                }

                float distance = Vector2.Distance(pointerPosition, new Vector2(screen.x, screen.y));
                if (distance < closest)
                {
                    closest = distance;
                    hoveredDropSlot = slot;
                }
            }

            if (previous != hoveredDropSlot)
            {
                RefreshCandidateHighlights();
            }
        }

        public void EndPointerInteraction()
        {
            if (!isDragging)
            {
                return;
            }

            if (selectedCube != null && hoveredDropSlot >= 0)
            {
                SwapSlots(selectedCube.SlotIndex, hoveredDropSlot);
            }

            isDragging = false;
            hoveredDropSlot = -1;
            ClearCandidateHighlights();
            SetHintsVisible(false);
        }

        private void SelectCube(CubeZoneVolume cube)
        {
            if (selectedCube == cube)
            {
                SelectionChanged?.Invoke(selectedCube);
                return;
            }

            selectedCube?.SetSelected(false);
            selectedCube = cube;
            selectedCube?.SetSelected(true);
            SelectionChanged?.Invoke(selectedCube);
        }

        private void SwapSlots(int firstSlot, int secondSlot)
        {
            if (firstSlot == secondSlot || firstSlot < 0 || secondSlot < 0)
            {
                return;
            }

            CubeZoneVolume first = slotToCube[firstSlot];
            CubeZoneVolume second = slotToCube[secondSlot];
            if (first == null || second == null)
            {
                return;
            }

            slotToCube[firstSlot] = second;
            slotToCube[secondSlot] = first;
            first.SetSlotIndex(secondSlot);
            second.SetSlotIndex(firstSlot);
            first.transform.localPosition = GetSlotLocalPosition(secondSlot);
            second.transform.localPosition = GetSlotLocalPosition(firstSlot);
            LayoutChanged?.Invoke();
            AssignmentChanged?.Invoke(firstSlot, GetAssignedEffect(firstSlot));
            AssignmentChanged?.Invoke(secondSlot, GetAssignedEffect(secondSlot));
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            ApplyTurretEffects();
            ApplyEnemyEffects();
        }

        private void ApplyTurretEffects()
        {
            var turrets = turretRegistry.Turrets;
            for (int i = 0; i < turrets.Count; i++)
            {
                TurretHealth health = turrets[i];
                if (health == null)
                {
                    continue;
                }

                CubeZoneEffectDefinition effect = GetEffectAtPosition(health.transform.position);
                float intervalMultiplier = effect != null && effect.EffectType == CubeZoneEffectType.TurretFireRateBoost
                    ? effect.TurretFireIntervalMultiplier : 1f;
                float damageMultiplier = effect != null && effect.EffectType == CubeZoneEffectType.TurretDamageBoost
                    ? effect.TurretDamageMultiplier : 1f;
                TurretController turret = health.GetComponent<TurretController>();
                turret?.SetZoneFireIntervalMultiplier(intervalMultiplier);
                turret?.SetZoneDamageMultiplier(damageMultiplier);
            }
        }

        private void ApplyEnemyEffects()
        {
            var enemies = enemyRegistry.ActiveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyController enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                CubeZoneEffectDefinition effect = GetEffectAtPosition(enemy.transform.position);
                if (effect != null && effect.EffectType == CubeZoneEffectType.EnemySlowAndDamage)
                {
                    enemy.SetZoneEffect(effect.EnemySpeedMultiplier, effect.EnemyDamagePerSecond);
                }
                else
                {
                    enemy.SetZoneEffect(1f, 0f);
                }
            }
        }

        private CubeZoneEffectDefinition GetEffectAtPosition(Vector3 worldPosition)
        {
            int index = GetZoneIndex(worldPosition);
            return index >= 0 ? GetAssignedEffect(index) : null;
        }

        public int GetZoneIndex(Vector3 worldPosition)
        {
            if (config == null)
            {
                return -1;
            }

            Vector3 local = transform.InverseTransformPoint(worldPosition);
            float extent = config.GridHalfExtent;
            if (Mathf.Abs(local.x) > extent || Mathf.Abs(local.y) > extent || Mathf.Abs(local.z) > extent)
            {
                return -1;
            }

            int index = local.x >= 0f ? 1 : 0;
            if (local.y >= 0f) index |= 2;
            if (local.z >= 0f) index |= 4;
            return index;
        }

        private Vector3 GetSlotLocalPosition(int slot)
        {
            float offset = config != null ? config.GridHalfExtent * 0.5f : 5.25f;
            return new Vector3((slot & 1) != 0 ? offset : -offset,
                (slot & 2) != 0 ? offset : -offset,
                (slot & 4) != 0 ? offset : -offset);
        }

        private void BuildAdjacentSlots(int slot)
        {
            adjacentSlots[0] = slot ^ 1;
            adjacentSlots[1] = slot ^ 2;
            adjacentSlots[2] = slot ^ 4;
        }

        private void ShowAdjacentHints(int slot)
        {
            BuildAdjacentSlots(slot);
            if (dropHintTransforms == null)
            {
                return;
            }

            for (int i = 0; i < dropHintTransforms.Length; i++)
            {
                Transform hint = dropHintTransforms[i];
                if (hint == null)
                {
                    continue;
                }

                bool active = i < adjacentSlots.Length;
                hint.gameObject.SetActive(active);
                if (active)
                {
                    hint.localPosition = GetSlotLocalPosition(adjacentSlots[i]);
                }
            }

            RefreshCandidateHighlights();
        }

        private void RefreshCandidateHighlights()
        {
            ClearCandidateHighlights();
            if (hoveredDropSlot >= 0)
            {
                GetCubeAtSlot(hoveredDropSlot)?.SetDropCandidate(true);
            }
        }

        private void ClearCandidateHighlights()
        {
            for (int i = 0; i < slotToCube.Length; i++)
            {
                slotToCube[i]?.SetDropCandidate(false);
            }
        }

        private void SetHintsVisible(bool visible)
        {
            if (dropHintTransforms == null)
            {
                return;
            }

            for (int i = 0; i < dropHintTransforms.Length; i++)
            {
                if (dropHintTransforms[i] != null)
                {
                    dropHintTransforms[i].gameObject.SetActive(visible);
                }
            }
        }
    }
}
