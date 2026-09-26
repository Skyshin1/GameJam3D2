using UnityEngine;

namespace AnchorDefense
{
    [CreateAssetMenu(menuName = "Anchor Defense/AI/Effects/Turret Repair", fileName = "TurretRepairEffect")]
    public sealed class TurretRepairSkillEffect : ActiveSkillEffect
    {
        [field: SerializeField, Min(0f)] public float Healing { get; private set; } = 35f;
        public override bool RepeatWhileInside => true;

        public override bool HasValidTarget(ActiveSkillContext context)
        {
            if (context?.Turrets == null) return false;
            for (int i = 0; i < context.Turrets.Count; i++)
            {
                TurretHealth turret = context.Turrets[i];
                if (turret != null && turret.IsAlive && turret.CurrentHealth < turret.MaxHealth)
                {
                    return true;
                }
            }
            return false;
        }

        public override bool Execute(ActiveSkillContext context)
        {
            if (!HasValidTarget(context)) return false;
            for (int i = 0; i < context.Turrets.Count; i++)
            {
                TurretHealth turret = context.Turrets[i];
                if (turret != null && turret.IsAlive)
                {
                    turret.Heal(Healing);
                }
            }
            return true;
        }

        public override void ApplyToTurret(TurretHealth turret, ActiveSkillContext context,
            float strength, float deltaTime)
        {
            if (turret != null && turret.IsAlive)
                turret.Heal(Healing * strength * deltaTime /
                    Mathf.Max(0.1f, context.CommandDuration));
        }

#if UNITY_EDITOR
        public void Configure(float healing) => Healing = Mathf.Max(0f, healing);
#endif
    }
}
