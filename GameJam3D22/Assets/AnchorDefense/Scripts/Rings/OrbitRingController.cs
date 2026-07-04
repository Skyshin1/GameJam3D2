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

        public OrbitRingId RingId => ringId;
        public int ActiveTurretCount { get; private set; }

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
            transform.Rotate(Vector3.up, -horizontalPixels * sensitivity, Space.Self);
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
