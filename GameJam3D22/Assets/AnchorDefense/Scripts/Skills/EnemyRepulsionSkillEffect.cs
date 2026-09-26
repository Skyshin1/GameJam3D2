using UnityEngine;

namespace AnchorDefense
{
    [CreateAssetMenu(menuName = "Anchor Defense/AI/Effects/Enemy Repulsion", fileName = "EnemyRepulsionEffect")]
    public sealed class EnemyRepulsionSkillEffect : ActiveSkillEffect
    {
        [field: SerializeField, Min(0f)] public float Distance { get; private set; } = 3.5f;

        public override bool HasValidTarget(ActiveSkillContext context)
        {
            return context != null && context.Enemies != null && context.Enemies.Count > 0;
        }

        public override bool Execute(ActiveSkillContext context)
        {
            if (!HasValidTarget(context)) return false;
            Vector3 origin = context.Core != null ? context.Core.position : context.Center;
            for (int i = 0; i < context.Enemies.Count; i++)
            {
                EnemyController enemy = context.Enemies[i];
                if (enemy == null || !enemy.IsAlive) continue;
                Vector3 direction = enemy.transform.position - origin;
                if (direction.sqrMagnitude < 0.0001f)
                {
                    direction = context.Center - origin;
                }
                if (direction.sqrMagnitude < 0.0001f)
                {
                    direction = Vector3.forward;
                }
                enemy.transform.position += direction.normalized * Distance;
            }
            return true;
        }

        public override void ApplyToEnemy(EnemyController enemy, ActiveSkillContext context,
            float strength, float deltaTime)
        {
            if (enemy == null || !enemy.IsAlive) return;
            Vector3 origin = context.Core != null ? context.Core.position : context.Center;
            Vector3 direction = enemy.transform.position - origin;
            if (direction.sqrMagnitude < 0.0001f) direction = context.Center - origin;
            if (direction.sqrMagnitude < 0.0001f) direction = Vector3.forward;
            enemy.transform.position += direction.normalized * (Distance * strength);
        }

#if UNITY_EDITOR
        public void Configure(float distance) => Distance = Mathf.Max(0f, distance);
#endif
    }
}
