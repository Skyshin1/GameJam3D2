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
        private AICommandOperation[] plannedOperations;
        private bool externallyActivated;
        private float[] activationTimes;
        private AICommandFieldMarker presentation;
        private Camera gameplayCamera;
        private readonly Dictionary<(int id, int version, int style), AICommandActorFeedback> actorFeedback =
            new Dictionary<(int, int, int), AICommandActorFeedback>();
        private readonly List<(int id, int version, int style)> expiredFeedback = new List<(int, int, int)>();
        public bool HasProducedEffect { get; private set; }
        public System.Action<System.Exception> ExecutionFailed;

        public void SetPresentation(AICommandFieldMarker marker, Camera view)
        { presentation = marker; gameplayCamera = view; }

        public void Prepare(CubeZoneGridController zoneGrid, Transform coreTransform,
            GameFlowController flow, int targetZoneId, AICommandOperation[] plan,
            float lifetime, int budgetOperations, OrbitRingController[] orbitRings)
        {
            plannedOperations = plan;
            externallyActivated = true;
            Initialize(zoneGrid, coreTransform, flow, targetZoneId,
                System.Array.ConvertAll(plan, operation => operation.Skill), lifetime, 0f,
                null, orbitRings);
            strength = 1f / Mathf.Max(1, budgetOperations);
            enabled = false;
        }

        public void ActivateOperation(AICommandOperation operation)
        {
            int index = System.Array.IndexOf(plannedOperations, operation);
            if (index < 0) throw new System.InvalidOperationException("操作不属于此区域指令场");
            enabled = true;
            Activate(index);
            if (failed[index]) throw new System.InvalidOperationException("区域技能激活失败");
            ApplyToCurrentActors(0f);
        }

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
            activationTimes = new float[operations.Length];
            activated = new bool[operations.Length];
            failed = new bool[operations.Length];
            enemyApplications = new HashSet<long>[operations.Length];
            turretApplications = new HashSet<int>[operations.Length];
            for (int i = 0; i < operations.Length; i++)
            {
                enemyApplications[i] = new HashSet<long>();
                turretApplications[i] = new HashSet<int>();
            }
            if (!externallyActivated)
            {
                ActivateReadyOperations();
                ApplyToCurrentActors(0f);
            }
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
            if (remaining <= 0f && (!externallyActivated ||
                System.Array.TrueForAll(activated, value => value)))
            {
                Destroy(gameObject);
                return;
            }
            if (!externallyActivated) ActivateReadyOperations();
            ApplyToCurrentActors(Time.deltaTime);
        }

        private void ActivateReadyOperations()
        {
            for (int i = 0; i < operations.Length; i++)
            {
                if (activated[i] || elapsed < i * staggerSeconds) continue;
                Activate(i);
            }
        }

        private void Activate(int i)
        {
            if (activated[i]) return;
            activated[i] = true;
            activationTimes[i] = elapsed;
            if (externallyActivated) remaining = Mathf.Max(remaining, duration);
            try
            {
                ActiveSkillEffect effect = operations[i] != null ? operations[i].Effect : null;
                if (effect != null)
                {
                    grid.TryGetActorsInZone(zoneId, enemies, turrets, out _);
                    var context = new ActiveSkillContext(zoneId, grid.GetCubeById(zoneId),
                        core, enemies, turrets, duration);
                    effect.ApplyOnActivation(context, plannedOperations != null ? plannedOperations[i].Source : playerText,
                        rings, plannedOperations != null ? plannedOperations[i].Parameters : parsedCommand);
                    HasProducedEffect = true;
                }
            }
            catch (System.Exception exception)
            {
                failed[i] = true;
                if (externallyActivated) throw;
                Debug.LogException(exception);
            }
            try
            {
                if (presentation != null && !failed[i])
                {
                    presentation.gameObject.SetActive(true);
                    presentation.NotifyActivation(operations[i]);
                }
                SpawnVfx(operations[i]);
            }
            catch (System.Exception exception) { Debug.LogException(exception); }
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
                float operationDelta = deltaTime;
                if (externallyActivated)
                {
                    float activeElapsed = elapsed - activationTimes[operationIndex];
                    operationDelta = Mathf.Min(deltaTime, Mathf.Max(0f, duration - activeElapsed + deltaTime));
                    if (activeElapsed >= duration && operationDelta <= 0f) continue;
                }

                try
                {
                    for (int i = 0; i < enemies.Count; i++)
                    {
                        EnemyController enemy = enemies[i];
                        if (enemy == null || !enemy.IsAlive) continue;
                        long key = ((long)enemy.GetInstanceID() << 32) ^ (uint)enemy.SpawnVersion;
                        if (effect.RepeatWhileInside || enemyApplications[operationIndex].Add(key))
                        {
                            HasProducedEffect = true;
                            Renderer visual = effect is AreaDamageSkillEffect ? AICommandActorFeedback.VisualFor(enemy.transform) : null;
                            Vector3 impact = AICommandActorFeedback.FrontPosition(enemy.transform, visual, gameplayCamera);
                            effect.ApplyToEnemy(enemy, context, strength, operationDelta);
                            if (effect is AreaDamageSkillEffect damage && damage.Damage * strength > 0f)
                                ShowActorFeedback(enemy.transform, enemy, 1, effect, impact);
                            else if (effect is EnemySlowSkillEffect slow && slow.SpeedMultiplier < 1f && strength > 0f)
                                ShowActorFeedback(enemy.transform, enemy, 3, effect, enemy.transform.position);
                            else if (effect is EnemyRepulsionSkillEffect repulsion && repulsion.Distance * strength > 0f)
                                ShowActorFeedback(enemy.transform, enemy, 4, effect, enemy.transform.position);
                        }
                    }

                    for (int i = 0; i < turrets.Count; i++)
                    {
                        TurretHealth turret = turrets[i];
                        if (turret == null || !turret.IsAlive) continue;
                        if (effect.RepeatWhileInside ||
                            turretApplications[operationIndex].Add(turret.GetInstanceID()))
                        {
                            HasProducedEffect = true;
                            float healthBefore = turret.CurrentHealth;
                            effect.ApplyToTurret(turret, context, strength, operationDelta);
                            if (effect is TurretRepairSkillEffect && turret.CurrentHealth > healthBefore)
                                ShowActorFeedback(turret.transform, null, 2, effect, turret.transform.position);
                            else if (effect is TurretBoostSkillEffect boost && turret.GetComponent<TurretController>() != null &&
                                (boost.FireIntervalMultiplier < 1f || boost.DamageMultiplier > 1f) && strength > 0f)
                                ShowActorFeedback(turret.transform, null, 4, effect, turret.transform.position);
                        }
                    }
                }
                catch (System.Exception exception)
                {
                    Debug.LogException(exception);
                    failed[operationIndex] = true;
                    if (externallyActivated)
                    {
                        enabled = false;
                        ExecutionFailed?.Invoke(exception);
                        return;
                    }
                }
            }
        }

        private void SpawnVfx(ActiveSkillDefinition operation)
        {
            // Built-in skills now signal on affected actors, rather than spawning a large central cloud.
            if (operation != null && AICommandActorFeedback.Supports(operation.Effect)) return;
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

        private void ShowActorFeedback(Transform actor, EnemyController enemy, int style, ActiveSkillEffect effect, Vector3 position)
        {
            try { RefreshActorFeedback(actor, enemy, style, effect, position); }
            catch (System.Exception exception)
            { Debug.LogWarning("单位反馈显示失败：" + exception.Message); }
        }

        private void RefreshActorFeedback(Transform actor, EnemyController enemy, int style, ActiveSkillEffect effect, Vector3 position)
        {
            var key = (actor.GetInstanceID(), enemy != null ? enemy.SpawnVersion : 0, style);
            if (actorFeedback.TryGetValue(key, out AICommandActorFeedback existing) && existing != null)
            {
                if (style == 1) existing.transform.position = position;
                existing.Refresh();
                return;
            }
            if (actorFeedback.Count > 64)
            {
                expiredFeedback.Clear();
                foreach (var pair in actorFeedback) if (pair.Value == null) expiredFeedback.Add(pair.Key);
                foreach (var expired in expiredFeedback) actorFeedback.Remove(expired);
            }
            var cue = new GameObject("Command Actor Feedback");
            cue.transform.SetParent(transform, false);
            AICommandActorFeedback feedback = cue.AddComponent<AICommandActorFeedback>();
            float size = presentation != null ? presentation.ActorCueWorldSize : 0.9f;
            Renderer visual = AICommandActorFeedback.VisualFor(actor);
            if (visual != null) size = Mathf.Clamp(Mathf.Max(visual.bounds.size.x, visual.bounds.size.y, visual.bounds.size.z) * 1.35f, size, size * 3f);
            feedback.Initialize(actor, enemy, gameplayCamera, style, AICommandActorFeedback.ColorFor(effect), position,
                size, presentation != null ? presentation.ActorCueOpacity : 0.9f, presentation != null ? presentation.SignalMaterial : null);
            actorFeedback[key] = feedback;
        }
    }
}
