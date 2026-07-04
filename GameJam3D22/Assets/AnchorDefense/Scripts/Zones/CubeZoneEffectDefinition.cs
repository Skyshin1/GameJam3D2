using UnityEngine;

namespace AnchorDefense
{
    public enum CubeZoneEffectType
    {
        None,
        TurretFireRateBoost,
        EnemySlowAndDamage,
        TurretDamageBoost
    }

    [CreateAssetMenu(menuName = "Anchor Defense/Zones/Zone Effect", fileName = "ZoneEffect")]
    public sealed class CubeZoneEffectDefinition : ScriptableObject
    {
        [field: SerializeField] public string Id { get; private set; }
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField, TextArea(2, 4)] public string Description { get; private set; }
        [field: SerializeField] public Sprite Icon { get; private set; }
        [field: SerializeField] public Color ZoneColor { get; private set; } = new Color(0.1f, 0.6f, 1f, 0.1f);
        [field: SerializeField] public CubeZoneEffectType EffectType { get; private set; }
        [field: SerializeField, Range(0.1f, 2f)] public float TurretFireIntervalMultiplier { get; private set; } = 1f;
        [field: SerializeField, Min(1f)] public float TurretDamageMultiplier { get; private set; } = 1f;
        [field: SerializeField, Range(0.1f, 2f)] public float EnemySpeedMultiplier { get; private set; } = 1f;
        [field: SerializeField, Min(0f)] public float EnemyDamagePerSecond { get; private set; }
        [field: SerializeField] public UpgradeNodeDefinition UnlockRequirement { get; private set; }

        [field: Header("Effect Presentation")]
        [field: Tooltip("显示在区域方块内部，并随绑定该效果的方块一起移动。")]
        [field: SerializeField] public GameObject ZoneVfxPrefab { get; private set; }
        [field: Tooltip("显示在该效果区域内每座炮塔身上的 Buff 特效。")]
        [field: SerializeField] public GameObject TurretVfxPrefab { get; private set; }
        [field: Tooltip("显示在该效果区域内每个敌人身上的 Buff / Debuff 特效。")]
        [field: SerializeField] public GameObject EnemyVfxPrefab { get; private set; }

#if UNITY_EDITOR
        public void Configure(string id, string displayName, string description, Color color,
            CubeZoneEffectType effectType, float turretIntervalMultiplier,
            float turretDamageMultiplier, float enemyMovementMultiplier, float enemyDps,
            UpgradeNodeDefinition unlockRequirement = null)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            ZoneColor = color;
            EffectType = effectType;
            TurretFireIntervalMultiplier = Mathf.Clamp(turretIntervalMultiplier, 0.1f, 2f);
            TurretDamageMultiplier = Mathf.Max(1f, turretDamageMultiplier);
            EnemySpeedMultiplier = Mathf.Clamp(enemyMovementMultiplier, 0.1f, 2f);
            EnemyDamagePerSecond = Mathf.Max(0f, enemyDps);
            UnlockRequirement = unlockRequirement;
        }
#endif
    }
}
