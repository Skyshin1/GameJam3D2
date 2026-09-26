using System.Collections.Generic;
using UnityEngine;

namespace AnchorDefense
{
    public sealed class ActiveSkillContext
    {
        public ActiveSkillContext(int zoneId, CubeZoneVolume zone, Transform core,
            IReadOnlyList<EnemyController> enemies, IReadOnlyList<TurretHealth> turrets,
            float commandDuration = 0f)
        {
            ZoneId = zoneId;
            Zone = zone;
            Core = core;
            Enemies = enemies;
            Turrets = turrets;
            CommandDuration = commandDuration;
        }

        public int ZoneId { get; }
        public CubeZoneVolume Zone { get; }
        public Transform Core { get; }
        public IReadOnlyList<EnemyController> Enemies { get; }
        public IReadOnlyList<TurretHealth> Turrets { get; }
        public float CommandDuration { get; }
        public Vector3 Center => Zone != null ? Zone.transform.position : Vector3.zero;
    }

    public abstract class ActiveSkillEffect : ScriptableObject
    {
        // Legacy single-skill hooks remain for existing assets. New command primitives can
        // override only ApplyToEnemy/ApplyToTurret and leave these defaults untouched.
        public virtual bool HasValidTarget(ActiveSkillContext context) => context != null;
        public virtual bool Execute(ActiveSkillContext context) => context != null;

        // A command field owns per-cast state. New primitives override only affected actors.
        public virtual bool RepeatWhileInside => false;
        public virtual bool IsWorldOperation => false;
        public virtual bool ValidateCommand(string playerText, OrbitRingController[] rings,
            out string error)
        {
            error = null;
            return true;
        }
        public virtual bool ValidateCommand(string playerText, OrbitRingController[] rings,
            AIParsedCommand command, out string error) =>
            ValidateCommand(playerText, rings, out error);
        public virtual void ApplyOnActivation(ActiveSkillContext context,
            string playerText, OrbitRingController[] rings) { }
        public virtual void ApplyOnActivation(ActiveSkillContext context,
            string playerText, OrbitRingController[] rings, AIParsedCommand command) =>
            ApplyOnActivation(context, playerText, rings);
        public virtual string DescribeCommand(string playerText, OrbitRingController[] rings)
        {
            return null;
        }
        public virtual string DescribeCommand(string playerText, OrbitRingController[] rings,
            AIParsedCommand command) => DescribeCommand(playerText, rings);
        public virtual void ApplyToEnemy(EnemyController enemy, ActiveSkillContext context,
            float strength, float deltaTime) { }
        public virtual void ApplyToTurret(TurretHealth turret, ActiveSkillContext context,
            float strength, float deltaTime) { }
    }
}
