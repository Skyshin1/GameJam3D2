using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace AnchorDefense.Tests
{
    public sealed partial class AICommandProtocolPlayModeTests
    {
        private readonly List<Object> owned = new List<Object>();
        private AICommandService service;
        private CubeZoneGridController grid;
        private GameFlowController flow;
        private AICommandConfig config;
        private KillResourceWallet wallet;
        private Transform core;
        private Camera camera;
        private ActiveSkillDefinition strike;
        private ActiveSkillDefinition repair;
        private ActiveSkillDefinition slow;
        private ActiveSkillDefinition rotate;
        private OrbitRingController[] rings;
        private EnemyRegistry enemies;
        private TurretRegistry turrets;
        private UpgradeSystem upgrades;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            core = Create("Core").transform;
            flow = Create("Flow").AddComponent<GameFlowController>();
            camera = Create("Camera").AddComponent<Camera>();
            grid = Create("Grid").AddComponent<CubeZoneGridController>();
            CubeZoneConfig zones = Asset<CubeZoneConfig>();
            zones.Configure(7f, null, null);
            var cubes = new CubeZoneVolume[2];
            for (int i = 0; i < cubes.Length; i++)
            {
                GameObject cubeObject = Create("Cube " + i);
                cubeObject.transform.SetParent(grid.transform);
                cubeObject.transform.position = new Vector3(i * 10f, 0f, 0f);
                cubes[i] = cubeObject.AddComponent<CubeZoneVolume>();
                cubes[i].Configure(i, new Vector3Int(i, 0, 0), null,
                    cubeObject.AddComponent<BoxCollider>(), null);
            }
            grid.Configure(zones, cubes);
            enemies = new EnemyRegistry();
            turrets = new TurretRegistry();
            grid.Initialize(enemies, turrets, core);
            strike = Skill("anchor_strike", "攻击", Asset<AreaDamageSkillEffect>(), new[] { "攻击", "轰击" });
            repair = Skill("repair_pulse", "治疗", Asset<TurretRepairSkillEffect>(), new[] { "治疗", "修复" });
            slow = Skill("slow_field", "减速", Asset<EnemySlowSkillEffect>(), new[] { "减速", "迟缓" });
            rotate = Skill("rotate_ring", "星环调度", Asset<RingRotationSkillEffect>(), new[] { "轨道", "星环", "旋转" });
            config = Asset<AICommandConfig>();
            config.Configure("https://example.com", "test", 0f, 12f, 10, 120,
                new[] { strike, repair, slow, rotate });
            rings = new OrbitRingController[2];
            for (int i = 0; i < rings.Length; i++)
            {
                rings[i] = Create("Ring " + i).AddComponent<OrbitRingController>();
                rings[i].ConfigureTurretSlots((OrbitRingId)i, null, null);
            }
            wallet = new KillResourceWallet();
            wallet.RegisterKill(10);
        }

        [TearDown]
        public void TearDown()
        {
            service?.Dispose();
            service = null;
            upgrades?.Dispose();
            upgrades = null;
            foreach (AICommandBatch batch in Object.FindObjectsOfType<AICommandBatch>())
                Object.DestroyImmediate(batch.gameObject);
            for (int i = owned.Count - 1; i >= 0; i--)
                if (owned[i] != null) Object.DestroyImmediate(owned[i]);
            owned.Clear();
            Time.timeScale = 1f;
        }

        private GameObject Create(string name)
        { var result = new GameObject(name); owned.Add(result); return result; }
        private T Asset<T>() where T : ScriptableObject
        { T result = ScriptableObject.CreateInstance<T>(); owned.Add(result); return result; }
        private ActiveSkillDefinition Skill(string id, string label, ActiveSkillEffect effect, string[] keywords)
        {
            ActiveSkillDefinition skill = Asset<ActiveSkillDefinition>();
            skill.Configure(id, label, "test", keywords, ActiveSkillTargetPolicy.AnyValidZone, effect);
            return skill;
        }
        private static AICommandItem Item(string id, string index, string source, string mode = null, int? count = null) =>
            new AICommandItem { source = source, operate = new AIOperate
                { action = id, index = index, mode = mode, count = count } };
        private static AICommandGroup Group(string type, params AICommandItem[] items) =>
            new AICommandGroup { type = type, operations = items };
        private static AIParsedCommand Command(params AICommandGroup[] groups) =>
            new AIParsedCommand { action = "compose", commands = groups };
        private AICommandExecutionResult Submit(AIParsedCommand command, string text)
        {
            service = new AICommandService(config, new MockAICommandProvider(command), wallet,
                upgrades, grid, core, camera, flow, rings);
            return service.Submit(text).GetAwaiter().GetResult();
        }

        private const string RegionJson = "{\"action\":\"compose\",\"commands\":[{\"type\":\"区域\",\"operations\":[{\"operate\":{\"action\":\"anchor_strike\",\"index\":\"C01\"},\"source\":\"攻击 C01\"}],\"addSkill\":{}}]}";
        private const string RingJson = "{\"action\":\"compose\",\"commands\":[{\"type\":\"星环\",\"operations\":[{\"operate\":{\"action\":\"rotate_ring\",\"index\":\"01\",\"mode\":\"repeat\",\"count\":3},\"source\":\"第一星环转3次\"}],\"addSkill\":{}}]}";

        [Test]
        public void NewJsonParsesNestedOperationsAndNullableCounts()
        {
            Assert.That(AICommandJsonValidator.TryParse(RegionJson, out AIParsedCommand region), Is.True);
            Assert.That(region.commands[0].operations[0].operate.index, Is.EqualTo("C01"));
            Assert.That(AICommandJsonValidator.TryParse(RingJson, out AIParsedCommand ring), Is.True);
            Assert.That(ring.commands[0].operations[0].operate.count, Is.EqualTo(3));
            Assert.That(AICommandJsonValidator.TryParse(RingJson.Replace("\"repeat\",\"count\":3",
                "\"spin\",\"count\":null"), out ring), Is.True);
            Assert.That(ring.commands[0].operations[0].operate.count, Is.Null);
            Assert.That(AICommandJsonValidator.TryParse("{\"action\":\"reject\",\"commands\":[]}", out _), Is.True);
        }

        [Test]
        public void JsonRejectsUnknownNestedFieldsDuplicateKeysAndNonEmptyReservedSkills()
        {
            Assert.That(AICommandJsonValidator.TryParse(RegionJson.Replace("\"addSkill\":{}", "\"addSkill\":{\"damage\":99}"), out _), Is.False);
            Assert.That(AICommandJsonValidator.TryParse(RegionJson.Replace("\"index\":\"C01\"", "\"index\":\"C01\",\"mode\":\"spin\""), out _), Is.False);
            Assert.That(AICommandJsonValidator.TryParse(RegionJson.Replace("\"source\":\"攻击 C01\"", "\"source\":\"攻击 C01\",\"source\":\"攻击 C02\""), out _), Is.False);
            Assert.That(AICommandJsonValidator.TryParse(RingJson.Replace("\"count\":3", "\"count\":3.5"), out _), Is.False);
            Assert.That(AICommandJsonValidator.TryParse(RingJson.Replace("\"count\":3", "\"count\":21"), out _), Is.False);
            Assert.That(AICommandJsonValidator.TryParse(RingJson.Replace("\"count\":3", "\"count\":\"3\""), out _), Is.False);
            Assert.That(AICommandJsonValidator.TryParse(RegionJson + "{}", out _), Is.False);
            Assert.That(AICommandJsonValidator.TryParse(RegionJson.Substring(0, RegionJson.Length - 1), out _), Is.False);
        }

        [TestCase("攻击轨道附近的敌人", false)]
        [TestCase("在区域减速并控制敌人", false)]
        [TestCase("治疗第一轨道附近的炮塔", false)]
        [TestCase("攻击第一轨道转角处的敌人", false)]
        [TestCase("攻击旋转的轨道附近的敌人", false)]
        [TestCase("操作第一星环", true)]
        [TestCase("第一轨道逆时针转90度", true)]
        [TestCase("停止所有星环", true)]
        public void RingIntentRequiresAnActualControlVerb(string text, bool expected)
        { Assert.That(AICommandValidator.HasRingControlIntent(text), Is.EqualTo(expected)); }

        [TestCase("攻击 C01", "anchor_strike")]
        [TestCase("治疗 C01", "repair_pulse")]
        [TestCase("减速 C01", "slow_field")]
        public void WrongModelRingClassificationRecoversOnlyTheExplicitRegionSkill(string text, string id)
        {
            AICommandExecutionResult result = Submit(Command(Group("星环",
                Item("rotate_ring", "01", text, "spin"))), text);
            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.Operations[0].Skill.Id, Is.EqualTo(id));
            Assert.That(result.Operations[0].ZoneId, Is.Zero);
            Assert.That(rings[0].IsCommandRotating, Is.False);
            Assert.That(rings[1].IsCommandRotating, Is.False);
            Assert.That(wallet.AvailableKills, Is.Zero);
        }

        [Test]
        public void UnrecoverableMisclassificationDoesNotRotateOrSpend()
        {
            AICommandExecutionResult result = Submit(Command(Group("星环",
                Item("rotate_ring", "01", "保护区域", "spin"))), "保护区域");
            Assert.That(result.Succeeded, Is.False);
            Assert.That(wallet.AvailableKills, Is.EqualTo(10));
            Assert.That(rings[0].IsCommandRotating, Is.False);
        }

        [Test]
        public void SourceCannotOmitAnExplicitTargetAndTurnItIntoAutomaticSelection()
        {
            AICommandExecutionResult result = Submit(Command(Group("区域",
                Item("anchor_strike", "C01", "攻击"))), "攻击 C02");
            Assert.That(result.Succeeded, Is.False);
            Assert.That(wallet.AvailableKills, Is.EqualTo(10));
            Assert.That(Object.FindObjectsOfType<AICommandField>(), Is.Empty);
        }

        [Test]
        public void AutomaticCombinationSharesOneTargetAndRawSourceOrderIsEnforced()
        {
            const string text = "攻击并减速敌人最多的区域";
            AICommandExecutionResult result = Submit(Command(Group("区域",
                Item("anchor_strike", null, text), Item("slow_field", null, text))), text);
            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.Operations[0].ZoneId, Is.EqualTo(result.Operations[1].ZoneId));
            service.Dispose();
            wallet.RegisterKill(10);
            result = Submit(Command(Group("区域", Item("repair_pulse", "C02", "治疗 C02"),
                Item("anchor_strike", "C01", "攻击 C01"))), "攻击 C01，治疗 C02");
            Assert.That(result.Succeeded, Is.False);
            Assert.That(wallet.AvailableKills, Is.EqualTo(10));
        }

        [Test]
        public void EveryGroupContributesToTheGlobalOperationLimit()
        {
            var items = new AICommandItem[7];
            for (int i = 0; i < items.Length; i++) items[i] = Item("anchor_strike", "C01", "攻击 C01");
            AICommandExecutionResult result = Submit(Command(Group("区域", items)), "攻击 C01");
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Message, Does.Contain("数量"));
            Assert.That(wallet.AvailableKills, Is.EqualTo(10));
        }

        [TestCase("第四星环转45度")]
        [TestCase("第一星环转1045度")]
        [TestCase("第一星环转21次")]
        public void InvalidRingNumbersAnglesAndCountsAreRejected(string text)
        {
            var effect = (RingRotationSkillEffect)rotate.Effect;
            Assert.That(effect.ValidateCommand(text, rings, out _), Is.False);
            Assert.That(rings[0].IsCommandRotating, Is.False);
        }

        [UnityTest]
        public IEnumerator MultiTargetOperationsKeepTheirTargetsAndSpendOnce()
        {
            AICommandExecutionResult result = Submit(Command(Group("区域",
                Item("anchor_strike", "C01", "攻击 C01"),
                Item("repair_pulse", "C02", "治疗 C02"))), "攻击 C01，治疗 C02");
            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.Operations[0].ZoneId, Is.Zero);
            Assert.That(result.Operations[1].ZoneId, Is.EqualTo(1));
            Assert.That(result.Operations[1].Executed, Is.False);
            yield return new WaitForSeconds(0.4f);
            Assert.That(result.Operations[1].Executed, Is.True);
            Assert.That(result.OperationSummary, Does.Contain("攻击 → C01").And.Contain("治疗 → C02"));
            Assert.That(wallet.AvailableKills, Is.Zero);
            Assert.That(Object.FindObjectsOfType<AICommandField>(), Has.Length.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator ShortLivedFieldWaitsForItsQueuedOperations()
        {
            typeof(AICommandConfig).GetField("<CommandFieldDuration>k__BackingField",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(config, 0.1f);
            AICommandExecutionResult result = Submit(Command(Group("区域",
                Item("anchor_strike", "C01", "攻击 C01"),
                Item("slow_field", "C01", "减速 C01"))), "攻击 C01，减速 C01");
            Assert.That(result.Succeeded, Is.True, result.Message);
            yield return new WaitForSeconds(0.15f);
            Assert.That(result.Operations[1].Executed, Is.False);
            Assert.That(Object.FindObjectsOfType<AICommandField>(), Has.Length.EqualTo(1));
            yield return new WaitForSeconds(0.35f);
            Assert.That(result.Operations[1].Executed, Is.True);
            Assert.That(Object.FindObjectsOfType<AICommandField>(), Is.Empty);
        }

        [UnityTest]
        public IEnumerator MixedCommandsPreserveOrderAndDistinctRingTargets()
        {
            AICommandExecutionResult result = Submit(Command(
                Group("区域", Item("anchor_strike", "C01", "攻击 C01")),
                Group("星环", Item("rotate_ring", "01", "操作第一星环", "adaptive"),
                    Item("rotate_ring", "02", "第二星环一直转", "spin")),
                Group("区域", Item("repair_pulse", "C02", "治疗 C02"))),
                "攻击 C01，操作第一星环，第二星环一直转，治疗 C02");
            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(rings[0].IsCommandRotating, Is.False);
            Assert.That(rings[1].IsCommandRotating, Is.False);
            yield return new WaitForSeconds(0.35f);
            Assert.That(rings[0].IsCommandRotating, Is.True);
            Assert.That(rings[1].IsCommandRotating, Is.False);
            yield return new WaitForSeconds(0.55f);
            Assert.That(rings[1].IsCommandRotating, Is.True);
            Assert.That(result.Operations[3].Executed, Is.True);
            Assert.That(wallet.AvailableKills, Is.Zero);
        }

        [Test]
        public void InvalidLastTargetPreventsAllExecutionAndSpending()
        {
            AICommandExecutionResult result = Submit(Command(Group("区域",
                Item("anchor_strike", "C01", "攻击 C01"),
                Item("repair_pulse", "C03", "治疗 C03"))), "攻击 C01，治疗 C03");
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Message, Does.Contain("C03"));
            Assert.That(wallet.AvailableKills, Is.EqualTo(10));
            Assert.That(Object.FindObjectsOfType<AICommandField>(), Is.Empty);
        }

        [Test]
        public void SameSkillCanTargetDifferentZonesButCannotDuplicateOneZone()
        {
            AICommandExecutionResult result = Submit(Command(Group("区域",
                Item("anchor_strike", "C01", "攻击 C01"),
                Item("anchor_strike", "C02", "攻击 C02"))), "攻击 C01，攻击 C02");
            Assert.That(result.Succeeded, Is.True, result.Message);
            service.Dispose();
            wallet.RegisterKill(10);
            result = Submit(Command(Group("区域",
                Item("anchor_strike", "C01", "攻击 C01"),
                Item("anchor_strike", "C01", "攻击 C01"))), "攻击 C01");
            Assert.That(result.Succeeded, Is.False);
            Assert.That(wallet.AvailableKills, Is.EqualTo(10));
        }

        [UnityTest]
        public IEnumerator PureMultiRingStopIsFreeWithNoPoints()
        {
            wallet.TrySpend(10);
            rings[0].RotateContinuouslyByCommand(30f, 0f);
            rings[1].RotateContinuouslyByCommand(30f, 0f);
            AICommandExecutionResult result = Submit(Command(Group("星环",
                Item("rotate_ring", "01", "停止第一星环", "stop"),
                Item("rotate_ring", "02", "停止第二星环", "stop"))), "停止第一星环，停止第二星环");
            Assert.That(result.Succeeded, Is.True, result.Message);
            yield return new WaitForSeconds(0.4f);
            Assert.That(rings[0].IsCommandRotating, Is.False);
            Assert.That(rings[1].IsCommandRotating, Is.False);
            Assert.That(wallet.AvailableKills, Is.Zero);
        }

        [TestCase("攻击 C01", "治疗 C01")]
        [TestCase("不要攻击 C01", "攻击 C01")]
        public void ForgedOrNegationStrippedSourceCannotExecute(string text, string source)
        {
            AICommandExecutionResult result = Submit(Command(Group("区域",
                Item("anchor_strike", "C01", source))), text);
            Assert.That(result.Succeeded, Is.False);
            Assert.That(wallet.AvailableKills, Is.EqualTo(10));
        }

        [UnityTest]
        public IEnumerator FiniteRotationCompletesThreeStepsAndStops()
        {
            const string text = "第一星环每次逆时针转45度，转3次后停止";
            AICommandExecutionResult result = Submit(Command(Group("星环",
                Item("rotate_ring", "01", text, "repeat", 3))), text);
            Assert.That(result.Succeeded, Is.True, result.Message);
            yield return new WaitForSeconds(2.7f);
            Assert.That(rings[0].IsCommandRotating, Is.False);
            Assert.That(Mathf.DeltaAngle(0f, rings[0].transform.eulerAngles.y), Is.EqualTo(135f).Within(0.5f));
        }

        [UnityTest]
        public IEnumerator ManualControlInterruptsFiniteRotation()
        {
            const string text = "第一星环转3次";
            AICommandExecutionResult result = Submit(Command(Group("星环",
                Item("rotate_ring", "01", text, "repeat", 3))), text);
            Assert.That(result.Succeeded, Is.True, result.Message);
            yield return new WaitForSeconds(0.2f);
            rings[0].RotateByDrag(0f, 1f);
            float stoppedAngle = rings[0].transform.eulerAngles.y;
            yield return new WaitForSeconds(1f);
            Assert.That(rings[0].IsCommandRotating, Is.False);
            Assert.That(rings[0].transform.eulerAngles.y, Is.EqualTo(stoppedAngle).Within(0.1f));
        }

        [Test]
        public void InventedRepeatCountAndRingConflictsAreRejectedBeforeSpending()
        {
            AICommandExecutionResult result = Submit(Command(Group("星环",
                Item("rotate_ring", "01", "第一星环转3次", "repeat", 4))), "第一星环转3次");
            Assert.That(result.Succeeded, Is.False);
            Assert.That(wallet.AvailableKills, Is.EqualTo(10));
            service.Dispose();
            result = Submit(Command(Group("星环",
                Item("rotate_ring", "01", "操作第一星环", "adaptive"),
                Item("rotate_ring", "01", "第一星环一直转", "spin"))), "操作第一星环，第一星环一直转");
            Assert.That(result.Succeeded, Is.False);
            Assert.That(rings[0].IsCommandRotating, Is.False);
            Assert.That(wallet.AvailableKills, Is.EqualTo(10));
        }

        [Test]
        public void OldSingleTargetProtocolRemainsUsableWithPlayerTargetPrecedence()
        {
            AICommandExecutionResult result = Submit(new AIParsedCommand
                { action = "cast_skill", skill_id = "anchor_strike", target_zone_id = "C99" }, "攻击 C02");
            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.ZoneId, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator GameOverCancelsQueuedCommandsBeforeTheyActivate()
        {
            AICommandExecutionResult result = Submit(Command(
                Group("区域", Item("anchor_strike", "C01", "攻击 C01")),
                Group("星环", Item("rotate_ring", "01", "操作第一星环", "adaptive"))),
                "攻击 C01，操作第一星环");
            Assert.That(result.Succeeded, Is.True);
            flow.EndGame();
            Time.timeScale = 1f;
            yield return new WaitForSeconds(0.4f);
            Assert.That(result.Operations[1].Executed, Is.False);
            Assert.That(rings[0].IsCommandRotating, Is.False);
        }

        [UnityTest]
        public IEnumerator MultiTargetAttackSharesOneDamageBudget()
        {
            var targets = new EnemyController[2];
            for (int i = 0; i < targets.Length; i++)
            {
                targets[i] = Create("Enemy " + i).AddComponent<EnemyController>();
                targets[i].transform.position = grid.GetCubeById(i).transform.position;
                targets[i].Initialize(Asset<EnemyConfig>(), null, 1f, 0f, null, null, null);
                targets[i].enabled = false;
                enemies.Register(targets[i]);
            }
            AICommandExecutionResult result = Submit(Command(Group("区域",
                Item("anchor_strike", "C01", "攻击 C01"),
                Item("anchor_strike", "C02", "攻击 C02"))), "攻击 C01，攻击 C02");
            Assert.That(result.Succeeded, Is.True, result.Message);
            yield return new WaitForSeconds(0.4f);
            foreach (EnemyController target in targets)
            {
                // Default enemy HP is 24 and attack damage is 25: splitting the budget
                // must leave both enemies alive with 11.5 HP, rather than killing each.
                Assert.That(target.IsAlive, Is.True);
                target.TakeDamage(new DamageInfo(12f, target.transform.position, null));
                Assert.That(target.IsAlive, Is.False);
            }
        }

        [Test]
        public void FailureBeforeFirstActivationRefundsTheCharge()
        {
            strike.Configure("anchor_strike", "攻击", "test", new[] { "攻击" },
                ActiveSkillTargetPolicy.AnyValidZone, Asset<AIProtocolFailingEffect>());
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: 测试激活失败"));
            AICommandExecutionResult result = Submit(Command(Group("区域",
                Item("anchor_strike", "C01", "攻击 C01"))), "攻击 C01");
            Assert.That(result.Outcome, Is.EqualTo(AICommandOutcome.ExecutionFailed));
            Assert.That(wallet.AvailableKills, Is.EqualTo(10));
            Assert.That(result.Operations[0].Executed, Is.False);
        }

        [UnityTest]
        public IEnumerator DeferredFailureReportsPartialExecutionAndCancelsTheRemainingRing()
        {
            repair.Configure("repair_pulse", "治疗", "test", new[] { "治疗" },
                ActiveSkillTargetPolicy.AnyValidZone, Asset<AIProtocolFailingEffect>());
            AICommandExecutionResult result = Submit(Command(
                Group("区域", Item("anchor_strike", "C01", "攻击 C01"),
                    Item("repair_pulse", "C02", "治疗 C02")),
                Group("星环", Item("rotate_ring", "01", "操作第一星环", "adaptive"))),
                "攻击 C01，治疗 C02，操作第一星环");
            AICommandExecutionResult failure = null;
            service.ExecutionUpdated += update => { if (!update.Succeeded) failure = update; };
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: 测试激活失败"));
            yield return new WaitForSeconds(0.7f);
            Assert.That(failure, Is.Not.Null);
            Assert.That(failure.Message, Does.Contain("部分执行"));
            Assert.That(result.Operations[0].Executed, Is.True);
            Assert.That(result.Operations[1].Executed, Is.False);
            Assert.That(result.Operations[2].Executed, Is.False);
            Assert.That(rings[0].IsCommandRotating, Is.False);
            Assert.That(wallet.AvailableKills, Is.Zero);
            Assert.That(Object.FindObjectsOfType<AICommandField>(), Is.Empty);
        }
    }

    public sealed class AIProtocolFailingEffect : ActiveSkillEffect
    {
        public override void ApplyOnActivation(ActiveSkillContext context, string text,
            OrbitRingController[] rings, AIParsedCommand command) =>
            throw new InvalidOperationException("测试激活失败");
    }
}
