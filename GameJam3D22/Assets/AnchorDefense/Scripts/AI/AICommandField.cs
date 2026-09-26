using System.Collections.Generic;
using UnityEngine;

namespace AnchorDefense
{
    // A short-lived, zone-bound command. Instant operations affect each actor once, including
    // actors arriving after the cast; continuous operations refresh while they remain inside.
    public sealed class AICommandField : MonoBehaviour
    {
        private readonly List<EnemyController> enemies = new List<EnemyController>();
        private readonly List<TurretHealth> turrets = new List<TurretHealth>();

        private HashSet<long>[] enemyApplications;
        private HashSet<int>[] turretApplications;
        private ActiveSkillDefinition[] operations;
        private CubeZoneGridController grid;
        private Transform core;
        private GameFlowController gameFlow;
        private OrbitRingController[] rings;
        private string playerText;
        private AIParsedCommand parsedCommand;
        private int zoneId;
        private float remaining;
        private float duration;
        private float elapsed;
        private float staggerSeconds;
        private float strength;
        private bool[] activated;
        private bool[] failed;

        public void Initialize(CubeZoneGridController zoneGrid, Transform coreTransform,
            GameFlowController flow, int targetZoneId, ActiveSkillDefinition[] commandOperations,
            float lifetime, float operationStaggerSeconds,
            string originalText = null, OrbitRingController[] orbitRings = null,
            AIParsedCommand command = null)
        {
            grid = zoneGrid;
            core = coreTransform;
            gameFlow = flow;
            rings = orbitRings;
            playerText = originalText;
            parsedCommand = command;
            zoneId = targetZoneId;
            operations = commandOperations;
            remaining = Mathf.Max(0.1f, lifetime);
            duration = remaining;
            staggerSeconds = Mathf.Clamp(operationStaggerSeconds, 0f, 1.5f);
            strength = 1f / Mathf.Max(1, operations.Length);
            activated = new bool[operations.Length];
            failed = new bool[operations.Length];
            enemyApplications = new HashSet<long>[operations.Length];
            turretApplications = new HashSet<int>[operations.Length];
            for (int i = 0; i < operations.Length; i++)
            {
                enemyApplications[i] = new HashSet<long>();
                turretApplications[i] = new HashSet<int>();
            }
            ActivateReadyOperations();
            ApplyToCurrentActors(0f);
        }

        private void Update()
        {
            if (grid == null || gameFlow == null || !gameFlow.IsPlaying ||
                grid.GetCubeById(zoneId) == null)
            {
                Destroy(gameObject);
                return;
            }

            remaining -= Time.deltaTime;
            elapsed += Time.deltaTime;
            if (remaining <= 0f)
            {
                Destroy(gameObject);
                return;
            }
            ActivateReadyOperations();
            ApplyToCurrentActors(Time.deltaTime);
        }

        private void ActivateReadyOperations()
        {
            for (int i = 0; i < operations.Length; i++)
            {
                if (activated[i] || elapsed < i * staggerSeconds) continue;
                activated[i] = true;
                try
                {
                    ActiveSkillEffect effect = operations[i] != null ? operations[i].Effect : null;
                    if (effect != null)
                    {
                        grid.TryGetActorsInZone(zoneId, enemies, turrets, out _);
                        var context = new ActiveSkillContext(zoneId, grid.GetCubeById(zoneId),
                            core, enemies, turrets, duration);
                        effect.ApplyOnActivation(context, playerText, rings, parsedCommand);
                    }
                }
                catch (System.Exception exception)
                {
                    Debug.LogException(exception);
                    failed[i] = true;
                }
                try { SpawnVfx(operations[i]); }
                catch (System.Exception exception) { Debug.LogException(exception); }
            }
        }

        private void ApplyToCurrentActors(float deltaTime)
        {
            if (grid == null || !grid.TryGetActorsInZone(zoneId, enemies, turrets, out _)) return;
            ActiveSkillContext context = new ActiveSkillContext(zoneId,
                grid.GetCubeById(zoneId), core, enemies, turrets, duration);

            for (int operationIndex = 0; operationIndex < operations.Length; operationIndex++)
            {
                ActiveSkillEffect effect = operations[operationIndex] != null
                    ? operations[operationIndex].Effect : null;
                if (effect == null || !activated[operationIndex] || failed[operationIndex]) continue;

                try
                {
                    for (int i = 0; i < enemies.Count; i++)
                    {
                        EnemyController enemy = enemies[i];
                        if (enemy == null || !enemy.IsAlive) continue;
                        long key = ((long)enemy.GetInstanceID() << 32) ^ (uint)enemy.SpawnVersion;
                        if (effect.RepeatWhileInside || enemyApplications[operationIndex].Add(key))
                            effect.ApplyToEnemy(enemy, context, strength, deltaTime);
                    }

                    for (int i = 0; i < turrets.Count; i++)
                    {
                        TurretHealth turret = turrets[i];
                        if (turret == null || !turret.IsAlive) continue;
                        if (effect.RepeatWhileInside ||
                            turretApplications[operationIndex].Add(turret.GetInstanceID()))
                            effect.ApplyToTurret(turret, context, strength, deltaTime);
                    }
                }
                catch (System.Exception exception)
                {
                    Debug.LogException(exception);
                    failed[operationIndex] = true;
                }
            }
        }

        private void SpawnVfx(ActiveSkillDefinition operation)
        {
            if (operation == null || operation.VfxPrefab == null) return;
            CubeZoneVolume zone = grid != null ? grid.GetCubeById(zoneId) : null;
            if (zone == null) return;
            GameObject instance = Instantiate(operation.VfxPrefab, zone.transform.position,
                Quaternion.identity, zone.transform);
            instance.name = operation.VfxPrefab.name + " (Command VFX)";
            float lifetime = Mathf.Max(0.05f, operation.VfxLifetime);
            PooledParticleEffect burst = instance.GetComponentInChildren<PooledParticleEffect>(true);
            if (burst != null)
            {
                burst.PlayBurst(zone.transform.position, new Color(0.25f, 0.9f, 1f), 42,
                    lifetime, effect => { if (effect != null) Destroy(effect.gameObject); });
                if (burst.gameObject != instance) Destroy(instance, lifetime + 0.05f);
                return;
            }
            ParticleSystem[] particles = instance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++) particles[i].Play(true);
            Destroy(instance, lifetime);
        }
    }
}
