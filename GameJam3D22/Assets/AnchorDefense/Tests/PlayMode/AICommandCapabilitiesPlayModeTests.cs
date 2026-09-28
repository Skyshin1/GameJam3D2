using System;
using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace AnchorDefense.Tests
{
    public sealed partial class AICommandProtocolPlayModeTests
    {
        private UpgradeNodeDefinition Node(string id, string name, int cost, TurretRuntimeStat stat = TurretRuntimeStat.Damage,
            params UpgradeNodeDefinition[] prerequisites)
        {
            var effect = Asset<TurretStatUpgradeEffect>();
            effect.Configure(stat, 1.25f);
            var node = Asset<UpgradeNodeDefinition>();
            node.Configure(id, name, "动态测试升级", name, cost, false, prerequisites, new UpgradeEffect[] { effect });
            return node;
        }

        private TurretRuntimeStats SetUpgrades(params UpgradeNodeDefinition[] nodes)
        {
            var tree = Asset<UpgradeTreeConfig>();
            tree.Configure(nodes);
            var stats = new TurretRuntimeStats(Asset<TurretConfig>());
            upgrades = new UpgradeSystem(tree, wallet, new UpgradeContext(stats, rings));
            return stats;
        }

        private EnemyController EnemyAt(Vector3 position)
        {
            var enemy = Create("Selector Enemy").AddComponent<EnemyController>();
            enemy.transform.position = position;
            enemy.Initialize(Asset<EnemyConfig>(), null, 1f, 0f, null, null, null);
            enemy.enabled = false;
            enemies.Register(enemy);
            return enemy;
        }

        [Test]
        public void JsonAcceptsUpgradeAndRegionSelectorsButRejectsCrossTypeParameters()
        {
            string upgradeJson = RegionJson.Replace("区域", "升级").Replace("anchor_strike", "purchase_upgrade").Replace("C01", "turret.custom.01");
            Assert.That(AICommandJsonValidator.TryParse(upgradeJson, out _), Is.True);
            string selectorJson = RegionJson.Replace("\"index\":\"C01\"", "\"index\":null,\"selector\":{\"metric\":\"enemy_count\",\"order\":\"desc\"}");
            Assert.That(AICommandJsonValidator.TryParse(selectorJson, out var command), Is.True);
            Assert.That(command.commands[0].operations[0].operate.selector.metric, Is.EqualTo("enemy_count"));
            Assert.That(AICommandJsonValidator.TryParse(selectorJson.Replace("desc", "highest"), out _), Is.False);
            Assert.That(AICommandJsonValidator.TryParse(selectorJson.Replace("enemy_count", "invented"), out _), Is.False);
            Assert.That(AICommandJsonValidator.TryParse(selectorJson.Replace("区域", "升级"), out _), Is.False);
        }

        [Test]
        public void PureUpgradeUsesNativeCostAndEffectsWithoutCommandFeeOrSkills()
        {
            var node = Node("custom.new.damage", "新配置增幅", 2);
            var stats = SetUpgrades(node);
            float damageBefore = stats.Damage;
            config.Configure("https://example.com", "test", 0f, 12f, 10, 120, Array.Empty<ActiveSkillDefinition>());
            wallet.TrySpend(8);
            var result = Submit(Command(Group("升级", Item("purchase_upgrade", node.Id, "升级新配置增幅"))), "升级新配置增幅");
            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(wallet.AvailableKills, Is.Zero);
            Assert.That(stats.Damage, Is.EqualTo(damageBefore * 1.25f));
            Assert.That(upgrades.GetState(node), Is.EqualTo(UpgradeNodeState.Purchased));
            Assert.That(result.Operations[0].Executed, Is.True);
            Assert.That(result.OperationSummary, Does.Contain(node.DisplayName));
            Assert.That(Object.FindObjectsOfType<AICommandField>(), Is.Empty);
        }

        [Test]
        public void GenericUpgradeRecoversModelRejectionAndChoosesCheapestAvailableConfiguredNode()
        {
            var costly = Node("brand.new.damage", "伤害增幅", 4);
            var cheap = Node("brand.new.health", "耐久增幅", 2, TurretRuntimeStat.MaxHealth);
            SetUpgrades(costly, cheap);
            var result = Submit(new AIParsedCommand { action = "reject", commands = Array.Empty<AICommandGroup>() }, "升级炮塔");
            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.Operations[0].Upgrade, Is.SameAs(cheap));
            Assert.That(wallet.AvailableKills, Is.EqualTo(8));
            Assert.That(upgrades.GetState(costly), Is.EqualTo(UpgradeNodeState.Available));
        }

        [Test]
        public void DynamicCatalogIncludesFreshNodeCostsStatesAndRequirementsInPrompt()
        {
            var prerequisite = Node("fresh.base", "基础升级", 3);
            var node = Node("fresh.special", "新增能力", 6, TurretRuntimeStat.Range, prerequisite);
            SetUpgrades(prerequisite, node);
            var provider = new MockAICommandProvider(new AIParsedCommand { action = "reject" });
            service = new AICommandService(config, provider, wallet, upgrades, grid, core, camera, flow, rings);
            service.Submit("查看能力").GetAwaiter().GetResult();
            Assert.That(provider.LastRequest.SystemPrompt, Does.Contain("fresh.special").And.Contain("新增能力")
                .And.Contain("费用=6").And.Contain("状态=Locked").And.Contain("前置=fresh.base"));
            Assert.That(provider.LastRequest.AllowedSkillIds, Does.Contain("purchase_upgrade"));
        }

        [TestCase("升级炮塔伤害", "range", false)]
        [TestCase("升级炮塔伤害", "damage", true)]
        [TestCase("不要升级炮塔伤害", "damage", false)]
        [TestCase("升级炮塔伤害", "invented", false)]
        public void WrongDirectionNegationAndUnknownUpgradeIdsCannotSpend(string source, string choice, bool success)
        {
            var damage = Node("damage", "增幅能力", 2);
            var range = Node("range", "射程能力", 3, TurretRuntimeStat.Range);
            SetUpgrades(damage, range);
            var result = Submit(Command(Group("升级", Item("purchase_upgrade", choice, source))), source);
            Assert.That(result.Succeeded, Is.EqualTo(success), result.Message);
            Assert.That(wallet.AvailableKills, Is.EqualTo(success ? 8 : 10));
        }

        [Test]
        public void UpgradeSourceCannotEraseTheExplicitDirection()
        {
            SetUpgrades(Node("range", "射程能力", 3, TurretRuntimeStat.Range));
            var result = Submit(Command(Group("升级", Item("purchase_upgrade", "range", "升级"))), "升级炮塔伤害");
            Assert.That(result.Succeeded, Is.False);
            Assert.That(wallet.AvailableKills, Is.EqualTo(10));
        }

        [Test]
        public void LockedPurchasedAndUnaffordableUpgradesGiveSpecificFeedbackWithoutSpending()
        {
            var prerequisite = Node("base", "基础增幅", 2);
            var locked = Node("advanced", "进阶增幅", 3, TurretRuntimeStat.Damage, prerequisite);
            var expensive = Node("expensive", "昂贵增幅", 20);
            SetUpgrades(prerequisite, locked, expensive);
            var result = Submit(Command(Group("升级", Item("purchase_upgrade", "advanced", "升级进阶增幅"))), "升级进阶增幅");
            Assert.That(result.Message, Does.Contain("缺少前置").And.Contain("基础增幅"));
            Assert.That(wallet.AvailableKills, Is.EqualTo(10));
            service.Dispose();
            result = Submit(Command(Group("升级", Item("purchase_upgrade", "expensive", "升级昂贵增幅"))), "升级昂贵增幅");
            Assert.That(result.Outcome, Is.EqualTo(AICommandOutcome.InsufficientPoints));
            Assert.That(wallet.AvailableKills, Is.EqualTo(10));
            service.Dispose();
            upgrades.TryPurchase(prerequisite);
            result = Submit(Command(Group("升级", Item("purchase_upgrade", "base", "升级基础增幅"))), "升级基础增幅");
            Assert.That(result.Message, Does.Contain("已购买"));
            Assert.That(wallet.AvailableKills, Is.EqualTo(8));
        }

        [UnityTest]
        public IEnumerator PrerequisitesCanBePurchasedEarlierInTheSameOrderedPlan()
        {
            var prerequisite = Node("base", "基础增幅", 2);
            var next = Node("next", "高级增幅", 3, TurretRuntimeStat.Damage, prerequisite);
            SetUpgrades(prerequisite, next);
            var result = Submit(Command(Group("升级", Item("purchase_upgrade", "base", "升级基础增幅"),
                Item("purchase_upgrade", "next", "升级高级增幅"))), "升级基础增幅，升级高级增幅");
            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.Operations[1].Executed, Is.False);
            yield return new WaitForSeconds(0.4f);
            Assert.That(upgrades.GetState(next), Is.EqualTo(UpgradeNodeState.Purchased));
            Assert.That(wallet.AvailableKills, Is.EqualTo(5));
        }

        [Test]
        public void ReversePrerequisiteOrderAndRepeatedUpgradeRejectTheWholePlan()
        {
            var prerequisite = Node("base", "基础增幅", 2);
            var next = Node("next", "高级增幅", 3, TurretRuntimeStat.Damage, prerequisite);
            SetUpgrades(prerequisite, next);
            var result = Submit(Command(Group("升级", Item("purchase_upgrade", "next", "升级高级增幅"),
                Item("purchase_upgrade", "base", "升级基础增幅"))), "升级高级增幅，升级基础增幅");
            Assert.That(result.Succeeded, Is.False);
            Assert.That(wallet.AvailableKills, Is.EqualTo(10));
            service.Dispose();
            result = Submit(Command(Group("升级", Item("purchase_upgrade", "base", "升级基础增幅"),
                Item("purchase_upgrade", "base", "升级基础增幅"))), "升级基础增幅");
            Assert.That(result.Succeeded, Is.False);
            Assert.That(wallet.AvailableKills, Is.EqualTo(10));
        }

        [Test]
        public void CombinedCostsAreCheckedBeforeEitherAttackOrPurchase()
        {
            var node = Node("damage", "基础增幅", 2);
            SetUpgrades(node);
            var result = Submit(Command(Group("区域", Item("anchor_strike", "C01", "攻击 C01")),
                Group("升级", Item("purchase_upgrade", "damage", "升级基础增幅"))), "攻击 C01，升级基础增幅");
            Assert.That(result.Outcome, Is.EqualTo(AICommandOutcome.InsufficientPoints));
            Assert.That(wallet.AvailableKills, Is.EqualTo(10));
            Assert.That(upgrades.GetState(node), Is.EqualTo(UpgradeNodeState.Available));
            Assert.That(Object.FindObjectsOfType<AICommandField>(), Is.Empty);
        }

        [UnityTest]
        public IEnumerator UpgradeCanUnlockASkillAndDoesNotReduceItsPowerBudget()
        {
            var node = Node("unlock", "攻击解锁", 2);
            SetUpgrades(node);
            wallet.RegisterKill(2);
            strike.Configure("anchor_strike", "攻击", "test", new[] { "攻击" }, ActiveSkillTargetPolicy.EnemyRichZone,
                strike.Effect, null, 2f, node);
            var target = EnemyAt(Vector3.zero);
            var result = Submit(Command(Group("升级", Item("purchase_upgrade", "unlock", "升级攻击解锁")),
                Group("区域", Item("anchor_strike", "C01", "攻击 C01"))), "升级攻击解锁，攻击 C01");
            Assert.That(result.Succeeded, Is.True, result.Message);
            yield return new WaitForSeconds(0.4f);
            Assert.That(result.Operations[1].Executed, Is.True);
            Assert.That(target.IsAlive, Is.False, "升级不能占用攻击威力预算");
            Assert.That(wallet.AvailableKills, Is.Zero);
        }

        [Test]
        public void LockedSkillCannotExecuteWithoutAnEarlierUnlockPurchase()
        {
            var node = Node("unlock", "攻击解锁", 2);
            SetUpgrades(node);
            strike.Configure("anchor_strike", "攻击", "test", new[] { "攻击" }, ActiveSkillTargetPolicy.EnemyRichZone,
                strike.Effect, null, 2f, node);
            var result = Submit(Command(Group("区域", Item("anchor_strike", "C01", "攻击 C01"))), "攻击 C01");
            Assert.That(result.Succeeded, Is.False);
            Assert.That(wallet.AvailableKills, Is.EqualTo(10));
        }

        [UnityTest]
        public IEnumerator FailedAttackAfterSuccessfulPurchaseRefundsOnlyUnusedCommandFee()
        {
            var node = Node("damage", "基础增幅", 2);
            SetUpgrades(node);
            wallet.RegisterKill(2);
            strike.Configure("anchor_strike", "攻击", "test", new[] { "攻击" }, ActiveSkillTargetPolicy.AnyValidZone, Asset<AIProtocolFailingEffect>());
            var result = Submit(Command(Group("升级", Item("purchase_upgrade", "damage", "升级基础增幅")),
                Group("区域", Item("anchor_strike", "C01", "攻击 C01"))), "升级基础增幅，攻击 C01");
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: 测试激活失败"));
            yield return new WaitForSeconds(0.4f);
            Assert.That(result.Operations[0].Executed, Is.True);
            Assert.That(result.Operations[1].Executed, Is.False);
            Assert.That(upgrades.GetState(node), Is.EqualTo(UpgradeNodeState.Purchased));
            Assert.That(wallet.AvailableKills, Is.EqualTo(10));
        }

        [TestCase("enemy_count", "desc", 0)]
        [TestCase("enemy_count", "asc", 1)]
        [TestCase("enemy_pressure", "desc", 1)]
        public void RegionSelectorsUseIndependentExactMetrics(string metric, string order, int expectedZone)
        {
            EnemyAt(Vector3.zero);
            EnemyAt(Vector3.one);
            EnemyAt(new Vector3(10f, 0f, 0f));
            for (int i = 0; i < 20; i++) EnemyAt(new Vector3(14f, 0f, 0f));
            var item = Item("anchor_strike", null, "攻击区域");
            item.operate.selector = new AITargetSelector { metric = metric, order = order };
            var result = Submit(Command(Group("区域", item)), "攻击区域");
            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.ZoneId, Is.EqualTo(expectedZone));
        }

        [Test]
        public void MostDenseRawIntentOverridesModelPressureAndCannotBeOmittedFromSource()
        {
            EnemyAt(Vector3.zero);
            EnemyAt(Vector3.one);
            EnemyAt(new Vector3(10f, 0f, 0f));
            for (int i = 0; i < 20; i++) EnemyAt(new Vector3(14f, 0f, 0f));
            const string text = "对敌人最密集的区域发动攻击";
            var item = Item("anchor_strike", null, text);
            item.operate.selector = new AITargetSelector { metric = "enemy_pressure", order = "desc" };
            var result = Submit(Command(Group("区域", item)), text);
            Assert.That(result.ZoneId, Is.Zero, result.Message);
            service.Dispose();
            wallet.RegisterKill(10);
            result = Submit(Command(Group("区域", Item("anchor_strike", null, "攻击"))), text);
            Assert.That(result.Succeeded, Is.False);
            Assert.That(wallet.AvailableKills, Is.EqualTo(10));
        }

        [TestCase("turret_count", "desc", 0)]
        [TestCase("turret_count", "asc", 1)]
        [TestCase("missing_health", "desc", 1)]
        [TestCase("missing_health", "asc", 0)]
        public void TurretAndMissingHealthSelectorsUseActualActors(string metric, string order, int expectedZone)
        {
            for (int i = 0; i < 3; i++)
            {
                var turret = Create("Selector Turret").AddComponent<TurretHealth>();
                turret.transform.position = new Vector3(i == 2 ? 10f : 0f, 0f, 0f);
                turret.Initialize(new TurretRuntimeStats(Asset<TurretConfig>()));
                turret.TakeDamage(new DamageInfo(i == 2 ? 10f : 1f, turret.transform.position, null));
                turrets.Register(turret);
            }
            var item = Item("repair_pulse", null, "治疗区域");
            item.operate.selector = new AITargetSelector { metric = metric, order = order };
            var result = Submit(Command(Group("区域", item)), "治疗区域");
            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.ZoneId, Is.EqualTo(expectedZone));
        }

        [UnityTest]
        public IEnumerator ThenSeparatedUpgradeFallbackPreservesDependencyOrder()
        {
            var first = Node("base", "基础增幅", 2);
            var next = Node("next", "高级增幅", 3, TurretRuntimeStat.Damage, first);
            SetUpgrades(first, next);
            var result = Submit(new AIParsedCommand { action = "reject" }, "升级基础增幅然后升级高级增幅");
            Assert.That(result.Succeeded, Is.True, result.Message);
            yield return new WaitForSeconds(0.4f);
            Assert.That(upgrades.GetState(next), Is.EqualTo(UpgradeNodeState.Purchased));
            Assert.That(wallet.AvailableKills, Is.EqualTo(5));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void RingCapacityUpgradeValidatesTheRingAndNeverRotates(bool correctRing)
        {
            var effect = Asset<RingTurretUpgradeEffect>();
            effect.Configure(OrbitRingId.Middle, 1);
            var node = Asset<UpgradeNodeDefinition>();
            node.Configure("capacity.custom", "动态扩容", "中环新增炮塔", "扩容", 2, false,
                Array.Empty<UpgradeNodeDefinition>(), new UpgradeEffect[] { effect });
            SetUpgrades(node);
            var turret = Create("Unlockable Turret").AddComponent<TurretController>();
            rings[1].ConfigureTurretSlots(OrbitRingId.Middle, null, new[] { turret });
            rings[1].InitializeTurretSlots();
            string text = correctRing ? "解锁第二星环炮塔" : "解锁第一星环炮塔";
            var result = Submit(Command(Group("升级", Item("purchase_upgrade", node.Id, text))), text);
            Assert.That(result.Succeeded, Is.EqualTo(correctRing), result.Message);
            Assert.That(rings[1].ActiveTurretCount, Is.EqualTo(correctRing ? 1 : 0));
            Assert.That(wallet.AvailableKills, Is.EqualTo(correctRing ? 8 : 10));
            foreach (var ring in rings) Assert.That(ring.IsCommandRotating, Is.False);
        }

        [Test]
        public void AlreadyPurchasedGenericUpgradeReportsCompletionAndFreeUpgradeWorksWithoutPoints()
        {
            var node = Node("free", "免费增幅", 0);
            SetUpgrades(node);
            wallet.TrySpend(10);
            var result = Submit(new AIParsedCommand { action = "reject" }, "升级炮塔");
            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(wallet.AvailableKills, Is.Zero);
            service.Dispose();
            result = Submit(new AIParsedCommand { action = "reject" }, "升级炮塔");
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Message, Does.Contain("已全部购买"));
        }

        [Test]
        public void UnknownExplicitUpgradeCannotMatchTheSuffixOfAKnownId()
        {
            SetUpgrades(Node("damage", "增幅能力", 2));
            var result = Submit(Command(Group("升级", Item("purchase_upgrade", null, "升级fake.damage.99"))), "升级fake.damage.99");
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Message, Does.Contain("不存在"));
            Assert.That(wallet.AvailableKills, Is.EqualTo(10));
        }

        [TestCase("I", 0)]
        [TestCase("II", 1)]
        [TestCase("III", 2)]
        public void RomanUpgradeLabelMatchesTheWholeTokenInsteadOfItsPrefix(string label, int chosen)
        {
            var first = Node("ring.first", "内环扩容", 1);
            var second = Node("ring.second", "中环扩容", 2);
            var third = Node("ring.third", "外环扩容", 3);
            first.Configure(first.Id, first.DisplayName, "test", "I", 1, false,
                Array.Empty<UpgradeNodeDefinition>(), first.Effects);
            second.Configure(second.Id, second.DisplayName, "test", "II", 2, false,
                Array.Empty<UpgradeNodeDefinition>(), second.Effects);
            third.Configure(third.Id, third.DisplayName, "test", "III", 3, false,
                Array.Empty<UpgradeNodeDefinition>(), third.Effects);
            var nodes = new[] { first, second, third };
            SetUpgrades(nodes);
            var result = Submit(Command(Group("升级", Item("purchase_upgrade", null, "购买" + label))), "购买" + label);
            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.Operations[0].Upgrade, Is.SameAs(nodes[chosen]));
            Assert.That(wallet.AvailableKills, Is.EqualTo(10 - nodes[chosen].KillCost));
        }

        [Test]
        public void ModelRingMisclassificationOfAnUpgradeRecoversOnlyTheNativePurchase()
        {
            var node = Node("damage", "增幅能力", 2);
            SetUpgrades(node);
            var result = Submit(Command(Group("星环", Item("rotate_ring", "01", "升级炮塔", "spin"))), "升级炮塔");
            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(upgrades.GetState(node), Is.EqualTo(UpgradeNodeState.Purchased));
            Assert.That(wallet.AvailableKills, Is.EqualTo(8));
            foreach (var ring in rings) Assert.That(ring.IsCommandRotating, Is.False);
        }

        [Test]
        public void ModelRegionMisclassificationOfAnUpgradeDoesNotCreateARegionField()
        {
            var node = Node("damage", "增幅能力", 2);
            SetUpgrades(node);
            var result = Submit(Command(Group("区域", Item("anchor_strike", null, "升级炮塔"))), "升级炮塔");
            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(upgrades.GetState(node), Is.EqualTo(UpgradeNodeState.Purchased));
            Assert.That(wallet.AvailableKills, Is.EqualTo(8));
            Assert.That(Object.FindObjectsOfType<AICommandField>(), Is.Empty);
        }

        [Test]
        public void NativePurchaseEffectFailureReportsCommittedNodeAndRefundsUnusedSkillFee()
        {
            var node = Asset<UpgradeNodeDefinition>();
            node.Configure("fault", "异常升级", "test", "异常升级", 2, false,
                Array.Empty<UpgradeNodeDefinition>(), new UpgradeEffect[] { Asset<AIProtocolFailingUpgradeEffect>() });
            SetUpgrades(node);
            wallet.RegisterKill(2);
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: 测试升级效果失败"));
            var result = Submit(Command(Group("升级", Item("purchase_upgrade", "fault", "升级异常升级")),
                Group("区域", Item("anchor_strike", "C01", "攻击 C01"))), "升级异常升级，攻击 C01");
            Assert.That(result.Outcome, Is.EqualTo(AICommandOutcome.ExecutionFailed));
            Assert.That(result.Operations[0].Executed, Is.True);
            Assert.That(result.Operations[1].Executed, Is.False);
            Assert.That(result.Message, Does.Contain("应用效果失败"));
            Assert.That(upgrades.GetState(node), Is.EqualTo(UpgradeNodeState.Purchased));
            Assert.That(wallet.AvailableKills, Is.EqualTo(10));
        }
    }

    public sealed class AIProtocolFailingUpgradeEffect : UpgradeEffect
    {
        public override void Apply(UpgradeContext context) => throw new InvalidOperationException("测试升级效果失败");
    }
}
