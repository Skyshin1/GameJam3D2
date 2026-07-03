using System;

namespace AnchorDefense
{
    public sealed class KillResourceWallet
    {
        public int TotalKills { get; private set; }
        public int AvailableKills { get; private set; }

        public event Action<int, int> Changed;

        public void RegisterKill(int amount = 1)
        {
            if (amount <= 0)
            {
                return;
            }

            TotalKills += amount;
            AvailableKills += amount;
            Changed?.Invoke(TotalKills, AvailableKills);
        }

        public bool CanSpend(int amount)
        {
            return amount >= 0 && AvailableKills >= amount;
        }

        public bool TrySpend(int amount)
        {
            if (!CanSpend(amount))
            {
                return false;
            }

            AvailableKills -= amount;
            Changed?.Invoke(TotalKills, AvailableKills);
            return true;
        }
    }
}
