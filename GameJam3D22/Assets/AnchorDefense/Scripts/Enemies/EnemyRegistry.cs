using System.Collections.Generic;

namespace AnchorDefense
{
    public sealed class EnemyRegistry
    {
        private readonly List<EnemyController> activeEnemies = new List<EnemyController>(256);

        public IReadOnlyList<EnemyController> ActiveEnemies => activeEnemies;
        public int Count => activeEnemies.Count;

        public void Register(EnemyController enemy)
        {
            if (enemy != null && !activeEnemies.Contains(enemy))
            {
                activeEnemies.Add(enemy);
            }
        }

        public void Unregister(EnemyController enemy)
        {
            int index = activeEnemies.IndexOf(enemy);
            if (index < 0)
            {
                return;
            }

            int lastIndex = activeEnemies.Count - 1;
            activeEnemies[index] = activeEnemies[lastIndex];
            activeEnemies.RemoveAt(lastIndex);
        }
    }
}
