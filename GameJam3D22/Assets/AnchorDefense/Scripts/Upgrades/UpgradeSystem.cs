using System;
using System.Collections.Generic;

namespace AnchorDefense
{
    public enum UpgradeNodeState
    {
        Placeholder,
        Locked,
        InsufficientKills,
        Available,
        Purchased
    }

    public sealed class UpgradeSystem : IDisposable
    {
        private readonly UpgradeTreeConfig config;
        private readonly KillResourceWallet wallet;
        private readonly UpgradeContext context;
        private readonly HashSet<string> purchasedNodeIds = new HashSet<string>();

        public UpgradeSystem(UpgradeTreeConfig treeConfig, KillResourceWallet killWallet, UpgradeContext upgradeContext)
        {
            config = treeConfig;
            wallet = killWallet;
            context = upgradeContext;
            wallet.Changed += HandleWalletChanged;
        }

        public UpgradeTreeConfig Config => config;
        public KillResourceWallet Wallet => wallet;

        public event Action Changed;

        public UpgradeNodeState GetState(UpgradeNodeDefinition node)
        {
            if (node == null || node.Placeholder)
            {
                return UpgradeNodeState.Placeholder;
            }

            if (purchasedNodeIds.Contains(node.Id))
            {
                return UpgradeNodeState.Purchased;
            }

            UpgradeNodeDefinition[] prerequisites = node.Prerequisites;
            if (prerequisites != null)
            {
                for (int i = 0; i < prerequisites.Length; i++)
                {
                    UpgradeNodeDefinition prerequisite = prerequisites[i];
                    if (prerequisite != null && !purchasedNodeIds.Contains(prerequisite.Id))
                    {
                        return UpgradeNodeState.Locked;
                    }
                }
            }

            return wallet.CanSpend(node.KillCost)
                ? UpgradeNodeState.Available
                : UpgradeNodeState.InsufficientKills;
        }

        public bool TryPurchase(UpgradeNodeDefinition node)
        {
            if (GetState(node) != UpgradeNodeState.Available || !wallet.TrySpend(node.KillCost))
            {
                return false;
            }

            purchasedNodeIds.Add(node.Id);
            UpgradeEffect[] effects = node.Effects;
            if (effects != null)
            {
                for (int i = 0; i < effects.Length; i++)
                {
                    effects[i]?.Apply(context);
                }
            }

            Changed?.Invoke();
            return true;
        }

        public void Dispose()
        {
            wallet.Changed -= HandleWalletChanged;
        }

        private void HandleWalletChanged(int totalKills, int availableKills)
        {
            Changed?.Invoke();
        }
    }
}
