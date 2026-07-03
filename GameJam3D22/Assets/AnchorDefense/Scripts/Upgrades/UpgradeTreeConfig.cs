using UnityEngine;

namespace AnchorDefense
{
    [CreateAssetMenu(menuName = "Anchor Defense/Upgrades/Upgrade Tree", fileName = "UpgradeTreeConfig")]
    public sealed class UpgradeTreeConfig : ScriptableObject
    {
        [field: SerializeField] public UpgradeNodeDefinition[] Nodes { get; private set; }

        public UpgradeNodeDefinition FindNode(string id)
        {
            if (Nodes == null)
            {
                return null;
            }

            for (int i = 0; i < Nodes.Length; i++)
            {
                UpgradeNodeDefinition node = Nodes[i];
                if (node != null && node.Id == id)
                {
                    return node;
                }
            }

            return null;
        }

#if UNITY_EDITOR
        public void Configure(UpgradeNodeDefinition[] nodes)
        {
            Nodes = nodes ?? new UpgradeNodeDefinition[0];
        }
#endif
    }
}
