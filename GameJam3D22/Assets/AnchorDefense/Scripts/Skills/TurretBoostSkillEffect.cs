using UnityEngine;

namespace AnchorDefense
{
    [CreateAssetMenu(menuName = "Anchor Defense/AI/Effects/Turret Boost", fileName = "TurretBoost")]
    public sealed class TurretBoostSkillEffect : ActiveSkillEffect
    {
        [field: SerializeField, Range(0.05f, 1f)]
        public float FireIntervalMultiplier { get; private set; } = 1f;
        [field: SerializeField, Range(1f, 3f)]
        public float DamageMultiplier { get; private set; } = 1f;

        public override bool RepeatWhileInside => true;
        public override bool HasValidTarget(ActiveSkillContext context) => context != null;
        public override bool Execute(ActiveSkillContext context) => context != null;

        public override void ApplyToTurret(TurretHealth turret, ActiveSkillContext context,
            float strength, float deltaTime)
        {
            if (turret == null || !turret.IsAlive) return;
            TurretController controller = turret.GetComponent<TurretController>();
            if (controller == null) return;
            controller.ApplyCommandBoost(
                Mathf.Lerp(1f, FireIntervalMultiplier, Mathf.Clamp01(strength)),
                Mathf.Lerp(1f, DamageMultiplier, Mathf.Clamp01(strength)), 0.2f);
        }

#if UNITY_EDITOR
        public void Configure(float fireIntervalMultiplier, float damageMultiplier)
        {
            FireIntervalMultiplier = Mathf.Clamp(fireIntervalMultiplier, 0.05f, 1f);
            DamageMultiplier = Mathf.Clamp(damageMultiplier, 1f, 3f);
        }
#endif
    }
}
