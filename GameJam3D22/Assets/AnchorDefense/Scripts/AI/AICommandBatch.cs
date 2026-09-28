using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnchorDefense
{
    // Owns one paid request. Region fields share its budget; ring operations are never
    // activated by a region field, so deleting a cube cannot schedule a ring command.
    public sealed class AICommandBatch : MonoBehaviour
    {
        private AICommandOperation[] operations;
        private readonly Dictionary<int, AICommandField> fields = new Dictionary<int, AICommandField>();
        private GameFlowController flow;
        private OrbitRingController[] rings;
        private KillResourceWallet wallet;
        private int cost;
        private bool paid;
        private bool started;
        private bool finished;
        private bool producedEffect;
        private float elapsed;
        private float stagger;
        private int next;
        private Action<AICommandExecutionResult> progress;
        public AICommandExecutionResult CurrentResult { get; private set; }

        public void Prepare(AICommandOperation[] plan, CubeZoneGridController grid,
            Transform core, GameFlowController gameFlow, OrbitRingController[] orbitRings,
            AICommandConfig config, Camera camera, KillResourceWallet commandWallet,
            Action<AICommandExecutionResult> onProgress)
        {
            operations = plan;
            flow = gameFlow;
            rings = orbitRings;
            wallet = commandWallet;
            cost = config.CommandCost;
            stagger = config.OperationStaggerSeconds;
            progress = onProgress;
            int budget = Array.FindAll(plan, operation => !operation.IsStop).Length;
            var perZone = new Dictionary<int, List<AICommandOperation>>();
            foreach (AICommandOperation operation in plan)
            {
                if (operation.Skill.Effect.IsWorldOperation) continue;
                if (!perZone.TryGetValue(operation.ZoneId, out List<AICommandOperation> zoneOperations))
                {
                    zoneOperations = new List<AICommandOperation>();
                    perZone.Add(operation.ZoneId, zoneOperations);
                }
                zoneOperations.Add(operation);
            }
            foreach (var pair in perZone)
            {
                var fieldObject = new GameObject("Anchor Command Field");
                fieldObject.transform.SetParent(grid.GetCubeById(pair.Key).transform, false);
                AICommandField field = fieldObject.AddComponent<AICommandField>();
                fields.Add(pair.Key, field);
                field.Prepare(grid, core, flow, pair.Key, pair.Value.ToArray(),
                    config.CommandFieldDuration, budget, rings);
                field.ExecutionFailed = exception => Fail(exception.Message);
                if (config.FieldMarkerPrefab == null) continue;
                try
                {
                    GameObject marker = Instantiate(config.FieldMarkerPrefab, fieldObject.transform);
                    marker.name = "Anchor Command Field Marker";
                    marker.GetComponent<AICommandFieldMarker>()?.Initialize(
                        Array.ConvertAll(pair.Value.ToArray(), operation => operation.Skill),
                        grid.Config != null ? grid.Config.CubeSize : 10.5f, stagger, camera);
                    // Presentation starts with the first operation, after the charge succeeds.
                    marker.SetActive(false);
                }
                catch (Exception exception) { Debug.LogException(exception); }
            }
            enabled = false;
        }

        public void Begin(bool charged)
        {
            paid = charged;
            started = true;
            enabled = true;
            flow.StateChanged += HandleStateChanged;
            ActivateReady();
        }

        private void Update()
        {
            if (!started || finished) return;
            if (flow == null || !flow.IsPlaying) { Cancel(); return; }
            elapsed += Time.deltaTime;
            ActivateReady();
            if (next == operations.Length && AllFieldsExpired())
            {
                finished = true;
                Destroy(gameObject);
            }
        }

        private void ActivateReady()
        {
            int previous = next;
            while (!finished && next < operations.Length && elapsed >= next * stagger)
            {
                AICommandOperation operation = operations[next];
                try
                {
                    if (operation.Skill.Effect.IsWorldOperation)
                    {
                        if (operation.Ring == null || !operation.Ring.gameObject.activeInHierarchy)
                            throw new InvalidOperationException("指定星环已不可用");
                        operation.Skill.Effect.ApplyOnActivation(null, operation.Source, rings, operation.Parameters);
                        producedEffect = true;
                    }
                    else
                    {
                        AICommandField field = fields[operation.ZoneId];
                        if (field == null) throw new InvalidOperationException("指定区域已不存在");
                        field.ActivateOperation(operation);
                        if (finished) return;
                        producedEffect |= field.HasProducedEffect;
                        foreach (Transform child in field.transform)
                            if (child.name == "Anchor Command Field Marker") child.gameObject.SetActive(true);
                    }
                    operation.Executed = true;
                    next++;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    Fail(exception.Message);
                    return;
                }
            }
            if (!finished && (next != previous || CurrentResult == null))
                Publish(AICommandOutcome.Success, next < operations.Length ? "已开始执行" : "实际执行");
        }

        private bool AllFieldsExpired()
        {
            foreach (AICommandField field in fields.Values) if (field != null) return false;
            return true;
        }

        private void Fail(string message)
        {
            if (finished) return;
            foreach (AICommandField field in fields.Values)
                if (field != null) producedEffect |= field.HasProducedEffect;
            finished = true;
            CleanupFields();
            // Stop only controls started by this batch that still own the same ring generation.
            foreach (AICommandOperation operation in operations)
                if (operation.Executed && !operation.IsStop && operation.Ring != null)
                    operation.Ring.StopCommandRotationIfOwned(operation.Parameters);
            if (paid && !producedEffect) wallet.RefundAvailable(cost);
            Publish(AICommandOutcome.ExecutionFailed, producedEffect ?
                $"部分执行后失败：{message}；剩余操作已取消" :
                $"执行失败：{message}；" + (paid ? "指令点已返还" : "未扣指令点"));
            Destroy(gameObject);
        }

        private void Publish(AICommandOutcome outcome, string prefix)
        {
            string summary = string.Join("；", Array.ConvertAll(operations, operation =>
                operation.Description + (operation.Executed ? "" : "（未执行）")));
            int zone = operations[0].ZoneId;
            if (Array.Exists(operations, operation => operation.ZoneId != zone)) zone = -1;
            CurrentResult = new AICommandExecutionResult(outcome, $"{prefix} {summary}",
                operations[0].Skill, zone, summary, operations);
            progress?.Invoke(CurrentResult);
        }

        public void Cancel()
        {
            if (started && !finished) Fail("指令已取消");
            else { CleanupFields(); Destroy(gameObject); }
        }

        private void HandleStateChanged(GameState state) { if (state == GameState.GameOver) Cancel(); }

        private void CleanupFields()
        {
            foreach (AICommandField field in fields.Values)
                if (field != null) { field.enabled = false; Destroy(field.gameObject); }
        }

        private void OnDestroy()
        {
            if (flow != null) flow.StateChanged -= HandleStateChanged;
            // External teardown must not leave dormant fields or queued operations behind.
            if (!finished) CleanupFields();
        }
    }
}
