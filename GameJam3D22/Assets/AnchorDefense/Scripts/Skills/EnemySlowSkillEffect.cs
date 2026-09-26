using UnityEngine;

namespace AnchorDefense
{
    [CreateAssetMenu(menuName = "Anchor Defense/AI/Effects/Enemy Slow Field", fileName = "EnemySlowField")]
    public sealed class EnemySlowSkillEffect : ActiveSkillEffect
    {
        [field: SerializeField, Range(0.05f, 1f)]
        public float SpeedMultiplier { get; private set; } = 0.45f;

        public override bool RepeatWhileInside => true;
        public override bool HasValidTarget(ActiveSkillContext context) => context != null;
        public override bool Execute(ActiveSkillContext context) => context != null;

        public override void ApplyToEnemy(EnemyController enemy, ActiveSkillContext context,
            float strength, float deltaTime)
        {
            if (enemy == null || !enemy.IsAlive) return;
            float scaledMultiplier = Mathf.Lerp(1f, SpeedMultiplier, Mathf.Clamp01(strength));
            enemy.ApplyCommandSlow(scaledMultiplier, 0.2f);
        }

#if UNITY_EDITOR
        public void Configure(float speedMultiplier) => SpeedMultiplier = Mathf.Clamp(speedMultiplier, 0.05f, 1f);
#endif
    }
}
