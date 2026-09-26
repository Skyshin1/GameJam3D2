using UnityEngine;

namespace AnchorDefense
{
    [CreateAssetMenu(menuName = "Anchor Defense/AI/Effects/Area Damage", fileName = "AreaDamageEffect")]
    public sealed class AreaDamageSkillEffect : ActiveSkillEffect
    {
        [field: SerializeField, Min(0f)] public float Damage { get; private set; } = 25f;

        public override bool HasValidTarget(ActiveSkillContext context)
        {
            return context != null && context.Enemies != null && context.Enemies.Count > 0;
        }

        public override bool Execute(ActiveSkillContext context)
        {
            if (!HasValidTarget(context)) return false;
            for (int i = 0; i < context.Enemies.Count; i++)
            {
                EnemyController enemy = context.Enemies[i];
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.TakeDamage(new DamageInfo(Damage, enemy.transform.position, null));
                }
            }
            return true;
        }

        public override void ApplyToEnemy(EnemyController enemy, ActiveSkillContext context,
            float strength, float deltaTime)
        {
            if (enemy != null && enemy.IsAlive)
                enemy.TakeDamage(new DamageInfo(Damage * strength, enemy.transform.position, null));
        }

#if UNITY_EDITOR
        public void Configure(float damage) => Damage = Mathf.Max(0f, damage);
#endif
    }
}
