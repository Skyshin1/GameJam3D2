using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnchorDefense.Tests
{
    public sealed class AICommandPlayModeTests
    {
        [UnityTest]
        public IEnumerator AssistantPortraitChangesMoodAndReturnsToListening()
        {
            GameObject portrait = new GameObject("Assistant", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = portrait.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(10f, 20f);
            AICommandAssistantMotion motion = portrait.AddComponent<AICommandAssistantMotion>();
            motion.SetMood(AICommandAssistantMood.Thinking);
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(motion.Mood, Is.EqualTo(AICommandAssistantMood.Thinking));
            Assert.That(rect.anchoredPosition, Is.Not.EqualTo(new Vector2(10f, 20f)));
            motion.React(true);
            Assert.That(motion.Mood, Is.EqualTo(AICommandAssistantMood.Success));
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.That(motion.Mood, Is.EqualTo(AICommandAssistantMood.Listening));
            Object.Destroy(portrait);
        }

        [UnityTest]
        public IEnumerator SustainedRingCommandKeepsRotatingAcrossFrames()
        {
            GameObject ringObject = new GameObject("Inner Ring");
            RingRotationSkillEffect effect = ScriptableObject.CreateInstance<RingRotationSkillEffect>();
            OrbitRingController ring = ringObject.AddComponent<OrbitRingController>();
            ring.ConfigureTurretSlots(OrbitRingId.Inner, null, null);
            effect.ApplyOnActivation(null, "持续旋转第一轨道", new[] { ring });
            float start = ring.transform.localEulerAngles.y;
            yield return new WaitForSeconds(0.25f);
            float first = Mathf.Abs(Mathf.DeltaAngle(start, ring.transform.localEulerAngles.y));
            yield return new WaitForSeconds(0.25f);
            float second = Mathf.Abs(Mathf.DeltaAngle(start, ring.transform.localEulerAngles.y));
            Assert.That(first, Is.GreaterThan(2f));
            Assert.That(second, Is.GreaterThan(first + 2f));
            Object.Destroy(ringObject);
            Object.Destroy(effect);
        }

        [UnityTest]
        public IEnumerator AmbiguousRingCommandKeepsRunningUntilStopped()
        {
            GameObject ringObject = new GameObject("Inner Ring");
            RingRotationSkillEffect effect = ScriptableObject.CreateInstance<RingRotationSkillEffect>();
            OrbitRingController ring = ringObject.AddComponent<OrbitRingController>();
            ring.ConfigureTurretSlots(OrbitRingId.Inner, null, null);
            var command = new AIParsedCommand
            {
                action = "compose", operation_ids = new[] { "rotate_ring" },
                ring_mode = "once", ring_id = "inner"
            };
            effect.ApplyOnActivation(null, "操作第一轨道", new[] { ring }, command);
            yield return new WaitForSeconds(1.1f);
            Assert.That(ring.IsCommandRotating, Is.True,
                "Model's one-shot guess must not override an ambiguous player request.");
            command.ring_mode = "stop";
            effect.ApplyOnActivation(null, "停止第一轨道", new[] { ring }, command);
            Assert.That(ring.IsCommandRotating, Is.False);
            Object.Destroy(ringObject);
            Object.Destroy(effect);
        }

        [Test]
        public void RingCommandDistinguishesFixedAndSustainedRotation()
        {
            GameObject ringObject = new GameObject("Inner Ring");
            RingRotationSkillEffect effect = ScriptableObject.CreateInstance<RingRotationSkillEffect>();
            try
            {
                OrbitRingController ring = ringObject.AddComponent<OrbitRingController>();
                ring.ConfigureTurretSlots(OrbitRingId.Inner, null, null);
                var rings = new[] { ring };
                Assert.That(effect.ValidateCommand("第一条轨道逆时针转90度", rings, out _), Is.True);
                Assert.That(effect.DescribeCommand("第一条轨道逆时针转90度", rings),
                    Does.Contain("直到停止"));
                Assert.That(effect.DescribeCommand("第一条轨道只逆时针转90度就停", rings),
                    Does.Contain("90°"));
                Assert.That(effect.ValidateCommand("持续操作第一轨道", rings, out _), Is.True);
                Assert.That(effect.DescribeCommand("持续操作第一轨道", rings),
                    Does.Contain("持续接管").And.Contain("直到停止"));
                Assert.That(effect.DescribeCommand("操作第一轨道", rings),
                    Does.Contain("持续接管"));
                Assert.That(effect.DescribeCommand("第一轨道转90度", rings),
                    Does.Contain("持续接管"));
                Assert.That(effect.DescribeCommand("第一轨道只转一次", rings),
                    Does.Contain("45°"));
                Assert.That(effect.ValidateCommand("第四轨道转45度", rings, out _), Is.False);
                Assert.That(effect.ValidateCommand("第10轨道转45度", rings, out _), Is.False);
                Assert.That(effect.ValidateCommand("第一轨道转360度", rings, out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(ringObject);
                Object.DestroyImmediate(effect);
            }
        }

        [Test]
        public void CornerMarkerCreatesEightThreeAxisCornersWithoutTheOldRing()
        {
            GameObject markerObject = new GameObject("Marker");
            GameObject cameraObject = new GameObject("Marker Camera");
            try
            {
                AICommandFieldMarker marker = markerObject.AddComponent<AICommandFieldMarker>();
                Camera camera = cameraObject.AddComponent<Camera>();
                Assert.DoesNotThrow(() => marker.Initialize(
                    new ActiveSkillDefinition[0], 10f, 0.25f, camera));
                LineRenderer[] segments = markerObject.GetComponentsInChildren<LineRenderer>();
                Assert.That(segments, Has.Length.EqualTo(24));
                for (int i = 0; i < segments.Length; i++)
                    Assert.That(segments[i].positionCount, Is.EqualTo(2));
                Assert.That(markerObject.GetComponentsInChildren<Collider>(), Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(markerObject);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void JsonValidatorRequiresAnExactWhitelistedCommandShape()
        {
            Assert.That(AICommandJsonValidator.HasExactTopLevelFields(
                "{\"action\":\"cast_skill\",\"skill_id\":\"anchor_strike\",\"target_zone_id\":\"C04\"}"),
                Is.True);
            Assert.That(AICommandJsonValidator.HasExactTopLevelFields(
                "{\"action\":\"cast_skill\",\"skill_id\":\"anchor_strike\",\"target_zone_id\":\"C04\",\"damage\":99999}"),
                Is.False);
            Assert.That(AICommandJsonValidator.HasExactTopLevelFields(
                "{\"action\":\"cast_skill\",\"skill_id\":\"anchor_strike\"}"),
                Is.False);
            Assert.That(AICommandJsonValidator.HasExactTopLevelFields(
                "{\"action\":\"cast_skill\",\"skill_id\":\"anchor_strike\",\"target_zone_id\":{}}"),
                Is.False);
            Assert.That(AICommandJsonValidator.HasExactTopLevelFields(
                "{\"action\":\"cast_skill\",\"skill_id\":\"anchor_strike\",\"target_zone_id\":null}"),
                Is.True);
            Assert.That(AICommandJsonValidator.HasExactTopLevelFields(
                "{\"action\":\"compose\",\"operation_ids\":[\"anchor_strike\",\"slow_field\"],\"target_zone_id\":\"C04\"}"),
                Is.True);
            Assert.That(AICommandJsonValidator.HasExactTopLevelFields(
                "{\"action\":\"compose\",\"operation_ids\":[\"anchor_strike\",999],\"target_zone_id\":\"C04\"}"),
                Is.False);
            Assert.That(AICommandJsonValidator.HasExactTopLevelFields(
                "{\"action\":\"compose\",\"operation_ids\":[\"anchor_strike\"],\"target_zone_id\":\"C04\",\"damage\":99999}"),
                Is.False);
            Assert.That(AICommandJsonValidator.HasExactTopLevelFields(
                "{\"action\":\"compose\",\"operation_ids\":[\"rotate_ring\"],\"target_zone_id\":null,\"ring_mode\":\"adaptive\",\"ring_id\":\"inner\"}"),
                Is.True);
            Assert.That(AICommandJsonValidator.HasExactTopLevelFields(
                "{\"action\":\"compose\",\"operation_ids\":[\"rotate_ring\"],\"target_zone_id\":null,\"ring_mode\":\"adaptive\",\"ring_id\":\"inner\",\"damage\":99999}"),
                Is.False);
        }

        [Test]
        public void CompositeCommandAcceptsKnownOperationsAndRejectsDuplicatesOrUnknownIds()
        {
            AreaDamageSkillEffect damage = ScriptableObject.CreateInstance<AreaDamageSkillEffect>();
            EnemySlowSkillEffect slow = ScriptableObject.CreateInstance<EnemySlowSkillEffect>();
            ActiveSkillDefinition strike = ScriptableObject.CreateInstance<ActiveSkillDefinition>();
            ActiveSkillDefinition slowField = ScriptableObject.CreateInstance<ActiveSkillDefinition>();
            strike.Configure("anchor_strike", "锚星轰击", "test", new[] { "轰炸" },
                ActiveSkillTargetPolicy.EnemyRichZone, damage);
            slowField.Configure("slow_field", "凝滞力场", "test", new[] { "减速" },
                ActiveSkillTargetPolicy.EnemyRichZone, slow);
            try
            {
                var validator = new AICommandValidator();
                var available = new List<ActiveSkillDefinition> { strike, slowField };
                var command = new AIParsedCommand
                {
                    action = "compose", operation_ids = new[] { "anchor_strike", "slow_field" },
                    target_zone_id = "C02"
                };
                Assert.That(validator.TryValidateOperations(command, available, 3,
                    out ActiveSkillDefinition[] operations, out _), Is.True);
                Assert.That(operations, Has.Length.EqualTo(2));
                command.operation_ids = new[] { "anchor_strike", "anchor_strike" };
                Assert.That(validator.TryValidateOperations(command, available, 3,
                    out _, out _), Is.False);
                command.operation_ids = new[] { "anchor_strike", "invented" };
                Assert.That(validator.TryValidateOperations(command, available, 3,
                    out _, out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(strike);
                Object.DestroyImmediate(slowField);
                Object.DestroyImmediate(damage);
                Object.DestroyImmediate(slow);
            }
        }

        [Test]
        public void KeywordFallbackCanComposeWithoutGuessingUnsupportedOrNegatedActions()
        {
            AreaDamageSkillEffect damage = ScriptableObject.CreateInstance<AreaDamageSkillEffect>();
            EnemySlowSkillEffect slow = ScriptableObject.CreateInstance<EnemySlowSkillEffect>();
            ActiveSkillDefinition strike = ScriptableObject.CreateInstance<ActiveSkillDefinition>();
            ActiveSkillDefinition slowField = ScriptableObject.CreateInstance<ActiveSkillDefinition>();
            strike.Configure("anchor_strike", "轰击", "test", new[] { "轰击", "攻击" },
                ActiveSkillTargetPolicy.EnemyRichZone, damage);
            slowField.Configure("slow_field", "减速", "test", new[] { "减速", "迟缓" },
                ActiveSkillTargetPolicy.EnemyRichZone, slow);
            try
            {
                var validator = new AICommandValidator();
                var available = new List<ActiveSkillDefinition> { strike, slowField };
                Assert.That(validator.TryResolveComposableIntent(
                    "在右上区域轰击并减速敌人", available, 3, out AIParsedCommand parsed), Is.True);
                Assert.That(parsed.OperationIds, Is.EqualTo(new[] { "anchor_strike", "slow_field" }));
                Assert.That(validator.TryResolveComposableIntent(
                    "不要轰击", available, 3, out _), Is.False);
                Assert.That(validator.TryResolveComposableIntent(
                    "召唤没有配置的新炮台", available, 3, out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(strike);
                Object.DestroyImmediate(slowField);
                Object.DestroyImmediate(damage);
                Object.DestroyImmediate(slow);
            }
        }

        [Test]
        public void RingFallbackUnderstandsStopWithoutInventingOneShotRotation()
        {
            RingRotationSkillEffect effect = ScriptableObject.CreateInstance<RingRotationSkillEffect>();
            ActiveSkillDefinition skill = ScriptableObject.CreateInstance<ActiveSkillDefinition>();
            skill.Configure("rotate_ring", "星环调度", "test",
                new[] { "轨道", "星环", "旋转" }, ActiveSkillTargetPolicy.AnyValidZone, effect);
            try
            {
                var validator = new AICommandValidator();
                var available = new List<ActiveSkillDefinition> { skill };
                Assert.That(validator.TryResolveComposableIntent(
                    "停止第一轨道", available, 3, out AIParsedCommand stop), Is.True);
                Assert.That(stop.RingMode, Is.EqualTo("stop"));
                Assert.That(validator.TryResolveComposableIntent(
                    "操作第一轨道", available, 3, out AIParsedCommand ongoing), Is.True);
                Assert.That(ongoing.RingMode, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(skill);
                Object.DestroyImmediate(effect);
            }
        }

        [Test]
        public void EmptyZoneStillAcceptsACompositeCommandAndCreatesAVisibleField()
        {
            GameObject core = new GameObject("Core");
            GameObject gridObject = new GameObject("Grid");
            GameObject cubeObject = new GameObject("Cube");
            GameObject flowObject = new GameObject("Flow");
            GameObject cameraObject = new GameObject("Camera");
            cubeObject.transform.SetParent(gridObject.transform);
            CubeZoneConfig zoneConfig = ScriptableObject.CreateInstance<CubeZoneConfig>();
            AICommandConfig commandConfig = ScriptableObject.CreateInstance<AICommandConfig>();
            AreaDamageSkillEffect damage = ScriptableObject.CreateInstance<AreaDamageSkillEffect>();
            EnemySlowSkillEffect slow = ScriptableObject.CreateInstance<EnemySlowSkillEffect>();
            ActiveSkillDefinition strike = ScriptableObject.CreateInstance<ActiveSkillDefinition>();
            ActiveSkillDefinition slowField = ScriptableObject.CreateInstance<ActiveSkillDefinition>();
            AICommandService service = null;
            try
            {
                CubeZoneVolume cube = cubeObject.AddComponent<CubeZoneVolume>();
                cube.Configure(0, Vector3Int.zero, null, cubeObject.AddComponent<BoxCollider>(), null);
                CubeZoneGridController grid = gridObject.AddComponent<CubeZoneGridController>();
                zoneConfig.Configure(7f, null, null);
                grid.Configure(zoneConfig, new[] { cube });
                grid.Initialize(new EnemyRegistry(), new TurretRegistry(), core.transform);

                strike.Configure("anchor_strike", "轰击", "test", new[] { "轰击" },
                    ActiveSkillTargetPolicy.EnemyRichZone, damage);
                slowField.Configure("slow_field", "减速", "test", new[] { "减速" },
                    ActiveSkillTargetPolicy.EnemyRichZone, slow);
                commandConfig.Configure("https://api.deepseek.com", "deepseek-flash",
                    0.1f, 12f, 10, 120, new[] { strike, slowField });

                var wallet = new KillResourceWallet();
                wallet.RegisterKill(10);
                var provider = new MockAICommandProvider(new AIParsedCommand
                {
                    action = "compose",
                    operation_ids = new[] { "anchor_strike", "slow_field" },
                    target_zone_id = "C99" // A model-invented target must not override the player.
                });
                service = new AICommandService(commandConfig, provider, wallet, null, grid,
                    core.transform, cameraObject.AddComponent<Camera>(),
                    flowObject.AddComponent<GameFlowController>());

                AICommandExecutionResult result = service.Submit("在 C01 轰击并减速").GetAwaiter().GetResult();
                Assert.That(result.Succeeded, Is.True, result.Message);
                Assert.That(result.OperationSummary, Does.Contain("轰击").And.Contain("减速"));
                Assert.That(wallet.AvailableKills, Is.Zero);
                Assert.That(provider.RequestCount, Is.EqualTo(1));
                Assert.That(Object.FindObjectOfType<AICommandField>(), Is.Not.Null);

                wallet.RegisterKill(10);
                AICommandExecutionResult autoResult = service.Submit(
                    "敌人太多了，想轰击并减速").GetAwaiter().GetResult();
                Assert.That(autoResult.Succeeded, Is.True, autoResult.Message);
                Assert.That(autoResult.ZoneId, Is.EqualTo(0));
                Assert.That(provider.RequestCount, Is.EqualTo(2));
                Assert.That(wallet.AvailableKills, Is.Zero);
            }
            finally
            {
                service?.Dispose();
                AICommandField field = Object.FindObjectOfType<AICommandField>();
                if (field != null) Object.DestroyImmediate(field.gameObject);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(flowObject);
                Object.DestroyImmediate(gridObject);
                Object.DestroyImmediate(core);
                Object.DestroyImmediate(commandConfig);
                Object.DestroyImmediate(zoneConfig);
                Object.DestroyImmediate(strike);
                Object.DestroyImmediate(slowField);
                Object.DestroyImmediate(damage);
                Object.DestroyImmediate(slow);
            }
        }

        [Test]
        public void ValidatorRejectsUnknownSkillAndUnknownZone()
        {
            AreaDamageSkillEffect effect = ScriptableObject.CreateInstance<AreaDamageSkillEffect>();
            ActiveSkillDefinition skill = ScriptableObject.CreateInstance<ActiveSkillDefinition>();
            effect.Configure(25f);
            skill.Configure("anchor_strike", "锚星轰击", "test", new[] { "攻击" },
                ActiveSkillTargetPolicy.EnemyRichZone, effect, null, 1f);

            try
            {
                var validator = new AICommandValidator();
                var skills = new List<ActiveSkillDefinition> { skill };
                Assert.That(validator.TryValidate(new AIParsedCommand
                {
                    action = "cast_skill",
                    skill_id = "invented_skill",
                    target_zone_id = "C01"
                }, skills, out _, out _), Is.False);

                Assert.That(validator.TryValidate(new AIParsedCommand
                {
                    action = "cast_skill",
                    skill_id = "anchor_strike",
                    target_zone_id = "C99"
                }, skills, out _, out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(skill);
                Object.DestroyImmediate(effect);
            }
        }

        [Test]
        public void ClearRepairIntentSurvivesModelRejectWithoutGuessingAnExplicitZone()
        {
            TurretRepairSkillEffect effect = ScriptableObject.CreateInstance<TurretRepairSkillEffect>();
            ActiveSkillDefinition skill = ScriptableObject.CreateInstance<ActiveSkillDefinition>();
            skill.Configure("repair_pulse", "修复脉冲", "test", new[] { "修复", "治疗" },
                ActiveSkillTargetPolicy.DamagedTurretZone, effect);
            try
            {
                var validator = new AICommandValidator();
                var skills = new List<ActiveSkillDefinition> { skill };
                Assert.That(validator.TryResolveObviousIntent(
                    "修复受损最严重的区域", skills, out AIParsedCommand parsed), Is.True);
                Assert.That(parsed.SkillId, Is.EqualTo("repair_pulse"));
                Assert.That(parsed.TargetZoneId, Is.Null);
                Assert.That(validator.TryResolveObviousIntent(
                    "修复右上区域", skills, out _), Is.False);
                Assert.That(validator.TryResolveObviousIntent(
                    "召唤新技能", skills, out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(skill);
                Object.DestroyImmediate(effect);
            }
        }

        [Test]
        public void WalletRefundOnlyRestoresAvailablePoints()
        {
            var wallet = new KillResourceWallet();
            wallet.RegisterKill(20);
            Assert.That(wallet.TrySpend(10), Is.True);
            wallet.RefundAvailable(10);

            Assert.That(wallet.TotalKills, Is.EqualTo(20));
            Assert.That(wallet.AvailableKills, Is.EqualTo(20));
        }

        [Test]
        public void MockProviderRecordsRequestAndHonorsCancellation()
        {
            var provider = new MockAICommandProvider(new AIParsedCommand
            {
                action = "reject",
                skill_id = null,
                target_zone_id = null
            });
            var request = new AICommandRequest("system", "玩家文字", new[] { "anchor_strike" });
            AIProviderResult first = provider.RequestAsync(request, CancellationToken.None).Result;

            Assert.That(first.Success, Is.True);
            Assert.That(provider.RequestCount, Is.EqualTo(1));
            Assert.That(provider.LastRequest, Is.SameAs(request));

            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            AIProviderResult cancelled = provider.RequestAsync(request, cancellation.Token).Result;
            Assert.That(cancelled.Success, Is.False);
            Assert.That(cancelled.Error, Is.EqualTo(AIProviderError.Cancelled));
        }

        [Test]
        public void DeepSeekRequestDisablesThinkingAndAllowsEnoughOutputTokens()
        {
            AICommandConfig config = ScriptableObject.CreateInstance<AICommandConfig>();
            config.Configure("https://api.deepseek.com", "deepseek-flash", 0.1f, 12f,
                10, 120, new ActiveSkillDefinition[0]);
            try
            {
                MethodInfo serialize = typeof(OpenAICompatibleCommandProvider).GetMethod(
                    "SerializeRequest", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.That(serialize, Is.Not.Null);
                string payload = (string)serialize.Invoke(null, new object[]
                {
                    new AICommandRequest("返回 JSON", "轰击右上角", new[] { "anchor_strike" }),
                    new ResolvedAIProviderSettings("https://api.deepseek.com", "deepseek-flash", "test"),
                    config
                });

                Assert.That(payload, Does.Contain("\"thinking\":{\"type\":\"disabled\"}"));
                Assert.That(payload, Does.Contain("\"max_tokens\":2048"));
                Assert.That(payload, Does.Contain("\"json_object\""));

                string otherProviderPayload = (string)serialize.Invoke(null, new object[]
                {
                    new AICommandRequest("返回 JSON", "轰击右上角", new[] { "anchor_strike" }),
                    new ResolvedAIProviderSettings("https://example.com/v1", "another-model", "test"),
                    config
                });
                Assert.That(otherProviderPayload, Does.Not.Contain("\"thinking\""));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void EmptyAndTruncatedResponsesHaveDistinctSafeMessages()
        {
            MethodInfo parse = typeof(OpenAICompatibleCommandProvider).GetMethod(
                "ParseResponse", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(parse, Is.Not.Null);

            AIProviderResult empty = (AIProviderResult)parse.Invoke(null, new object[]
            {
                "{\"choices\":[{\"finish_reason\":\"stop\",\"message\":{\"content\":\"\"}}]}"
            });
            AIProviderResult truncated = (AIProviderResult)parse.Invoke(null, new object[]
            {
                "{\"choices\":[{\"finish_reason\":\"length\",\"message\":{\"content\":\"{\"}}]}"
            });

            Assert.That(empty.Error, Is.EqualTo(AIProviderError.InvalidResponse));
            Assert.That(empty.Message, Does.Contain("空内容"));
            Assert.That(truncated.Error, Is.EqualTo(AIProviderError.InvalidResponse));
            Assert.That(truncated.Message, Does.Contain("截断"));
        }
    }
}
