using System.Collections;
using UnityEngine;

namespace AnchorDefense
{
    public enum OrbitRingId
    {
        Inner,
        Middle,
        Outer
    }

    public sealed class OrbitRingController : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Renderer[] selectionRenderers;
        [SerializeField] private Color normalColor = new Color(0.2f, 0.75f, 1f);
        [SerializeField] private Color selectedColor = Color.white;
        [Header("Turret Slots")]
        [SerializeField] private OrbitRingId ringId;
        [SerializeField] private TurretController[] initialTurrets;
        [SerializeField] private TurretController[] upgradeTurrets;
        [SerializeField] private TurretSlot[] initialTurretSlots;
        [SerializeField] private TurretSlot[] upgradeTurretSlots;

        private MaterialPropertyBlock propertyBlock;
        private Coroutine commandRotation;
        private AIParsedCommand commandOwner;

        public OrbitRingId RingId => ringId;
        public int ActiveTurretCount { get; private set; }
        public bool IsCommandRotating => commandRotation != null;

        public void Configure(Renderer[] renderers, Color idleColor, Color highlightColor)
        {
            selectionRenderers = renderers;
            normalColor = idleColor;
            selectedColor = highlightColor;
        }

        public void ConfigureTurretSlots(
            OrbitRingId id,
            TurretController[] startingTurrets,
            TurretController[] unlockableTurrets)
        {
            ringId = id;
            initialTurrets = startingTurrets;
            upgradeTurrets = unlockableTurrets;
        }

        public void ConfigureTurretSlotAssets(
            OrbitRingId id,
            TurretSlot[] startingSlots,
            TurretSlot[] unlockableSlots)
        {
            ringId = id;
            initialTurretSlots = startingSlots;
            upgradeTurretSlots = unlockableSlots;
            initialTurrets = null;
            upgradeTurrets = null;
        }

        public void InitializeTurretSlots()
        {
            ActiveTurretCount = 0;
            if (HasAssetSlots())
            {
                SetSlotsActive(initialTurretSlots, true);
                SetSlotsActive(upgradeTurretSlots, false);
                return;
            }
            SetTurretsActive(initialTurrets, true);
            SetTurretsActive(upgradeTurrets, false);
        }

        public int UnlockTurrets(int count)
        {
            if (HasAssetSlots())
            {
                return UnlockSlots(count);
            }
            if (upgradeTurrets == null || count <= 0)
            {
                return 0;
            }

            int unlocked = 0;
            for (int i = 0; i < upgradeTurrets.Length && unlocked < count; i++)
            {
                TurretController turret = upgradeTurrets[i];
                if (turret == null || turret.gameObject.activeSelf)
                {
                    continue;
                }

                turret.gameObject.SetActive(true);
                ActiveTurretCount++;
                unlocked++;
            }

            return unlocked;
        }

        private bool HasAssetSlots()
        {
            return (initialTurretSlots != null && initialTurretSlots.Length > 0) ||
                   (upgradeTurretSlots != null && upgradeTurretSlots.Length > 0);
        }

        private void SetSlotsActive(TurretSlot[] slots, bool active)
        {
            if (slots == null)
            {
                return;
            }
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                {
                    continue;
                }
                slots[i].SetUnlocked(active);
                if (active && slots[i].Instance != null)
                {
                    ActiveTurretCount++;
                }
            }
        }

        private int UnlockSlots(int count)
        {
            if (upgradeTurretSlots == null || count <= 0)
            {
                return 0;
            }
            int unlocked = 0;
            for (int i = 0; i < upgradeTurretSlots.Length && unlocked < count; i++)
            {
                TurretSlot slot = upgradeTurretSlots[i];
                if (slot == null)
                {
                    continue;
                }
                TurretController turret = slot.EnsureInstance();
                if (turret == null || turret.gameObject.activeSelf)
                {
                    continue;
                }
                turret.gameObject.SetActive(true);
                ActiveTurretCount++;
                unlocked++;
            }
            return unlocked;
        }

        public void RotateByDrag(float horizontalPixels, float sensitivity)
        {
            StopCommandRotation();
            transform.Rotate(Vector3.up, -horizontalPixels * sensitivity, Space.Self);
        }

        public void RotateByCommand(float degrees, float duration)
        {
            StopCommandRotation();
            commandRotation = StartCoroutine(AnimateCommandRotation(degrees, duration));
        }

        public void RotateContinuouslyByCommand(float degreesPerSecond, float duration)
        {
            StopCommandRotation();
            commandRotation = StartCoroutine(AnimateContinuousRotation(degreesPerSecond, duration));
        }

        public void RotateRepeatedlyByCommand(float degrees, float duration, int count)
        {
            if (count < 1 || count > 20) throw new System.ArgumentOutOfRangeException(nameof(count));
            StopCommandRotation();
            commandRotation = StartCoroutine(AnimateRepeatedRotation(degrees, duration, count));
        }

        private IEnumerator AnimateRepeatedRotation(float degrees, float duration, int count)
        {
            duration = Mathf.Max(0.05f, duration);
            for (int iteration = 0; iteration < count; iteration++)
            {
                float elapsed = 0f;
                float previous = 0f;
                while (elapsed < duration)
                {
                    elapsed = Mathf.Min(duration, elapsed + Time.deltaTime);
                    float progress = elapsed / duration;
                    float current = degrees * progress * progress * (3f - 2f * progress);
                    transform.Rotate(Vector3.up, current - previous, Space.Self);
                    previous = current;
                    yield return null;
                }
            }
            commandRotation = null;
        }

        public void DefendByCommand(float maximumDegreesPerSecond, float duration)
        {
            StopCommandRotation();
            commandRotation = StartCoroutine(AnimateTacticalRotation(
                Mathf.Abs(maximumDegreesPerSecond), duration));
        }

        public void StopCommandRotation()
        {
            commandOwner = null;
            if (commandRotation == null) return;
            StopCoroutine(commandRotation);
            commandRotation = null;
        }

        public void ClaimCommandOwnership(AIParsedCommand command) => commandOwner = command;

        public void StopCommandRotationIfOwned(AIParsedCommand command)
        {
            if (ReferenceEquals(commandOwner, command)) StopCommandRotation();
        }

        private void OnDisable() => StopCommandRotation();

        private IEnumerator AnimateContinuousRotation(float degreesPerSecond, float duration)
        {
            float elapsed = 0f;
            while (duration <= 0f || elapsed < duration)
            {
                float step = duration <= 0f ? Time.deltaTime :
                    Mathf.Min(Time.deltaTime, duration - elapsed);
                transform.Rotate(Vector3.up, degreesPerSecond * step, Space.Self);
                elapsed += step;
                yield return null;
            }
            commandRotation = null;
        }

        private IEnumerator AnimateTacticalRotation(float maximumDegreesPerSecond, float duration)
        {
            float elapsed = 0f;
            float reassessIn = 0f;
            float remainingAngle = 0f;
            bool scanning = false;
            while (duration <= 0f || elapsed < duration)
            {
                float step = duration <= 0f ? Time.deltaTime :
                    Mathf.Min(Time.deltaTime, duration - elapsed);
                elapsed += step;
                reassessIn -= step;
                if (reassessIn <= 0f)
                {
                    reassessIn = 0.2f;
                    scanning = !TryFindThreatAlignment(out remainingAngle);
                }

                float turn;
                if (scanning)
                {
                    turn = -maximumDegreesPerSecond * 0.5f * step;
                }
                else
                {
                    turn = Mathf.Clamp(remainingAngle,
                        -maximumDegreesPerSecond * step, maximumDegreesPerSecond * step);
                    remainingAngle -= turn;
                }
                transform.Rotate(Vector3.up, turn, Space.Self);
                yield return null;
            }
            commandRotation = null;
        }

        private bool TryFindThreatAlignment(out float angle)
        {
            angle = 0f;
            EnemyController[] enemies = FindObjectsOfType<EnemyController>(false);
            TurretController[] turrets = GetComponentsInChildren<TurretController>(false);
            if (enemies.Length == 0 || turrets.Length == 0) return false;

            Vector3 origin = transform.position;
            Vector3 axis = transform.up;
            EnemyController nearestEnemy = null;
            float nearestDistance = float.PositiveInfinity;
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyController enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive) continue;
                float distance = (enemy.transform.position - origin).sqrMagnitude;
                if (distance >= nearestDistance) continue;
                nearestDistance = distance;
                nearestEnemy = enemy;
            }
            if (nearestEnemy == null) return false;

            Vector3 enemyDirection = Vector3.ProjectOnPlane(
                nearestEnemy.transform.position - origin, axis);
            if (enemyDirection.sqrMagnitude < 0.01f) return false;
            float smallestAngle = float.PositiveInfinity;
            for (int i = 0; i < turrets.Length; i++)
            {
                TurretController turret = turrets[i];
                if (turret == null || !turret.gameObject.activeInHierarchy) continue;
                Vector3 turretDirection = Vector3.ProjectOnPlane(
                    turret.transform.position - origin, axis);
                if (turretDirection.sqrMagnitude < 0.01f) continue;
                float candidate = Vector3.SignedAngle(turretDirection, enemyDirection, axis);
                if (Mathf.Abs(candidate) >= smallestAngle) continue;
                smallestAngle = Mathf.Abs(candidate);
                angle = candidate;
            }
            return !float.IsPositiveInfinity(smallestAngle);
        }

        private IEnumerator AnimateCommandRotation(float degrees, float duration)
        {
            float elapsed = 0f;
            float previous = 0f;
            duration = Mathf.Max(0.05f, duration);
            while (elapsed < duration)
            {
                elapsed = Mathf.Min(duration, elapsed + Time.deltaTime);
                float progress = elapsed / duration;
                progress = progress * progress * (3f - 2f * progress);
                float current = degrees * progress;
                transform.Rotate(Vector3.up, current - previous, Space.Self);
                previous = current;
                yield return null;
            }
            commandRotation = null;
        }

        public void SetSelected(bool selected)
        {
            if (selectionRenderers == null)
            {
                return;
            }

            propertyBlock = propertyBlock ?? new MaterialPropertyBlock();
            Color color = selected ? selectedColor : normalColor;
            for (int i = 0; i < selectionRenderers.Length; i++)
            {
                Renderer targetRenderer = selectionRenderers[i];
                if (targetRenderer == null)
                {
                    continue;
                }

                targetRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, color);
                propertyBlock.SetColor(ColorId, color);
                propertyBlock.SetColor(EmissionColorId, color * (selected ? 2f : 0.7f));
                targetRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void Awake()
        {
            SetSelected(false);
        }

        private void SetTurretsActive(TurretController[] turrets, bool active)
        {
            if (turrets == null)
            {
                return;
            }

            for (int i = 0; i < turrets.Length; i++)
            {
                if (turrets[i] == null)
                {
                    continue;
                }

                turrets[i].gameObject.SetActive(active);
                if (active)
                {
                    ActiveTurretCount++;
                }
            }
        }
    }
}
