using UnityEngine;

namespace AnchorDefense
{
    public enum ActiveSkillTargetPolicy
    {
        EnemyRichZone,
        DamagedTurretZone,
        AnyValidZone,
        TurretRichZone
    }

    [CreateAssetMenu(menuName = "Anchor Defense/AI/Active Skill", fileName = "ActiveSkill")]
    public sealed class ActiveSkillDefinition : ScriptableObject
    {
        [field: SerializeField] public string Id { get; private set; }
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField, TextArea(2, 5)] public string ModelDescription { get; private set; }
        [field: SerializeField] public string[] Keywords { get; private set; }
        [field: SerializeField] public Sprite Icon { get; private set; }
        [field: SerializeField] public ActiveSkillTargetPolicy TargetPolicy { get; private set; }
        [field: SerializeField] public ActiveSkillEffect Effect { get; private set; }
        [field: SerializeField] public GameObject VfxPrefab { get; private set; }
        [field: SerializeField, Min(0.05f)] public float VfxLifetime { get; private set; } = 2f;
        [field: SerializeField] public UpgradeNodeDefinition UnlockRequirement { get; private set; }

        public bool IsUnlocked(UpgradeSystem upgrades)
        {
            return UnlockRequirement == null ||
                   (upgrades != null && upgrades.GetState(UnlockRequirement) == UpgradeNodeState.Purchased);
        }

#if UNITY_EDITOR
        public void Configure(string id, string displayName, string modelDescription,
            string[] keywords, ActiveSkillTargetPolicy targetPolicy, ActiveSkillEffect effect,
            GameObject vfxPrefab = null, float vfxLifetime = 2f,
            UpgradeNodeDefinition unlockRequirement = null)
        {
            Id = id;
            DisplayName = displayName;
            ModelDescription = modelDescription;
            Keywords = keywords ?? new string[0];
            TargetPolicy = targetPolicy;
            Effect = effect;
            VfxPrefab = vfxPrefab;
            VfxLifetime = Mathf.Max(0.05f, vfxLifetime);
            UnlockRequirement = unlockRequirement;
        }
#endif
    }
}
