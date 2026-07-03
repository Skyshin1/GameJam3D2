using UnityEngine;

namespace AnchorDefense
{
    [CreateAssetMenu(menuName = "Anchor Defense/Upgrades/Upgrade Node", fileName = "UpgradeNode")]
    public sealed class UpgradeNodeDefinition : ScriptableObject
    {
        [field: SerializeField] public string Id { get; private set; }
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField, TextArea(2, 5)] public string Description { get; private set; }
        [field: SerializeField] public string ShortLabel { get; private set; } = "?";
        [field: SerializeField] public Sprite Icon { get; private set; }
        [field: SerializeField, Min(0)] public int KillCost { get; private set; }
        [field: SerializeField] public bool Placeholder { get; private set; }
        [field: SerializeField] public UpgradeNodeDefinition[] Prerequisites { get; private set; }
        [field: SerializeField] public UpgradeEffect[] Effects { get; private set; }

#if UNITY_EDITOR
        public void Configure(
            string id,
            string displayName,
            string description,
            string shortLabel,
            int killCost,
            bool placeholder,
            UpgradeNodeDefinition[] prerequisites,
            UpgradeEffect[] effects)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            ShortLabel = shortLabel;
            KillCost = Mathf.Max(0, killCost);
            Placeholder = placeholder;
            Prerequisites = prerequisites ?? new UpgradeNodeDefinition[0];
            Effects = effects ?? new UpgradeEffect[0];
        }
#endif
    }
}
