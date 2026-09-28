using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace AnchorDefense
{
    // Discovers configured nodes on every request; purchases always use the existing game system.
    public sealed class AIGameCapabilityCatalog
    {
        private readonly UpgradeSystem upgrades;
        public AIGameCapabilityCatalog(UpgradeSystem upgradeSystem) { upgrades = upgradeSystem; }
        public bool HasUpgrades => upgrades?.Config?.Nodes != null &&
            Array.Exists(upgrades.Config.Nodes, node => node != null && !node.Placeholder && !string.IsNullOrWhiteSpace(node.Id));

        public static bool HasUpgradeIntent(string source) => !string.IsNullOrWhiteSpace(source) &&
            Regex.IsMatch(source, "升级|购买|买入|买个|买一个|解锁|扩容|永久强化|提升等级");

        public UpgradeNodeDefinition Find(string id) => upgrades?.Config?.FindNode(id);

        public bool TryResolve(string source, string index, out UpgradeNodeDefinition node, out string error)
        {
            node = null;
            error = "当前没有接入升级功能";
            if (!HasUpgrades) return false;
            if (!HasUpgradeIntent(source)) { error = "原文没有购买或升级的意图"; return false; }
            if (index != null && (Find(index) == null || Find(index).Placeholder))
            { error = "不存在的升级节点：" + index; return false; }
            var named = new List<UpgradeNodeDefinition>();
            foreach (UpgradeNodeDefinition candidate in upgrades.Config.Nodes)
            {
                if (candidate == null || candidate.Placeholder || string.IsNullOrWhiteSpace(candidate.Id)) continue;
                if (Regex.IsMatch(source, @"(?<![A-Za-z0-9_.])" + Regex.Escape(candidate.Id) + @"(?![A-Za-z0-9_.])") || ContainsName(source, candidate.DisplayName) ||
                    ContainsName(source, candidate.ShortLabel)) named.Add(candidate);
            }
            if (named.Count > 1) { error = "一个升级操作指定了多个节点，请分别列出操作"; return false; }
            if (named.Count == 1)
            {
                node = named[0];
                if (index != null && index != node.Id) { error = "升级节点与玩家指定的名称不一致"; return false; }
                return true;
            }
            // Explicit IDs are authoritative even when unknown, rather than silently auto-selecting.
            Match explicitId = Regex.Match(source, @"[A-Za-z][A-Za-z0-9_]*(?:\.[A-Za-z0-9_]+)+");
            if (explicitId.Success) { error = "不存在的升级节点：" + explicitId.Value; return false; }
            bool cannon = Regex.IsMatch(source, "炮塔|炮台");
            bool ring = Regex.IsMatch(source, "星环|轨道|[内中外]环");
            bool generic = Regex.IsMatch(source.Trim(), @"^(?:请|帮我|给我)?升级(?:一下|所有|全部|我的)?炮[塔台](?:一下|吧|。)?$");
            if (index == null && cannon && !ring && !generic && !Regex.IsMatch(source,
                "伤害|攻击力|威力|射速|攻速|发射间隔|生命|血量|耐久|射程|范围|弹速|子弹速度|弹丸速度|命中|碰撞半径|瘫痪|失效时间|减伤|承伤"))
            { error = "无法明确炮塔升级方向，请指定升级名称"; return false; }
            if (index != null && !generic)
            {
                node = Find(index);
                if (node == null || node.Placeholder) { error = "不存在的升级节点：" + index; return false; }
                if ((ring && !MatchesRingUpgrade(node, source)) || (cannon && !ring && !MatchesDirection(node, source)))
                { error = "升级节点与炮塔升级方向不匹配"; node = null; return false; }
                return true;
            }
            var candidates = new List<UpgradeNodeDefinition>();
            foreach (UpgradeNodeDefinition candidate in upgrades.Config.Nodes)
            {
                if (candidate == null || candidate.Placeholder || string.IsNullOrWhiteSpace(candidate.Id)) continue;
                bool match = ring ? MatchesRingUpgrade(candidate, source) : cannon ? MatchesDirection(candidate, source) :
                    ContainsName(source, candidate.DisplayName) || ContainsName(source, candidate.ShortLabel);
                if (match) candidates.Add(candidate);
            }
            candidates.Sort((a, b) => a.KillCost != b.KillCost ? a.KillCost.CompareTo(b.KillCost) :
                string.CompareOrdinal(a.Id, b.Id));
            node = candidates.Find(candidate => upgrades.GetState(candidate) == UpgradeNodeState.Available);
            if (node != null) return true;
            node = candidates.Find(candidate => upgrades.GetState(candidate) != UpgradeNodeState.Purchased);
            if (node != null) return true; // The executor reports its precise unmet requirement.
            error = candidates.Count > 0 ? "匹配的升级已全部购买" : "没有匹配的升级节点，请指定能力目录中的升级方向或名称";
            return false;
        }

        private static bool ContainsName(string source, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return Regex.IsMatch(name, @"^[A-Za-z0-9_.]+$") ?
                Regex.IsMatch(source, @"(?<![A-Za-z0-9_.])" + Regex.Escape(name) + @"(?![A-Za-z0-9_.])") : name.Length >= 2 && source.Contains(name);
        }

        private static bool MatchesRingUpgrade(UpgradeNodeDefinition node, string source)
        {
            var targets = new List<OrbitRingId>();
            if (Regex.IsMatch(source, "内环|内轨|第一(?:条|个)?(?:星环|轨道|环)|1号(?:星环|轨道|环)")) targets.Add(OrbitRingId.Inner);
            if (Regex.IsMatch(source, "中环|中轨|第二(?:条|个)?(?:星环|轨道|环)|2号(?:星环|轨道|环)")) targets.Add(OrbitRingId.Middle);
            if (Regex.IsMatch(source, "外环|外轨|第三(?:条|个)?(?:星环|轨道|环)|3号(?:星环|轨道|环)")) targets.Add(OrbitRingId.Outer);
            if (targets.Count > 1 || Regex.IsMatch(source, "第[四五六七八九]|[4-9]号")) return false;
            foreach (UpgradeEffect effect in node.Effects ?? Array.Empty<UpgradeEffect>())
                if (effect is RingTurretUpgradeEffect capacity && (targets.Count == 0 || targets[0] == capacity.RingId)) return true;
            return false;
        }

        private static bool MatchesDirection(UpgradeNodeDefinition node, string source)
        {
            string pattern = Regex.IsMatch(source, "伤害|攻击力|威力") ? "Damage" :
                Regex.IsMatch(source, "射速|攻速|发射间隔") ? "FireInterval" :
                Regex.IsMatch(source, "生命|血量|耐久") ? "MaxHealth" :
                Regex.IsMatch(source, "射程|范围") ? "Range" :
                Regex.IsMatch(source, "弹速|子弹速度|弹丸速度") ? "ProjectileSpeed" :
                Regex.IsMatch(source, "命中|碰撞半径") ? "ProjectileHitRadius" :
                Regex.IsMatch(source, "瘫痪|失效时间") ? "DisableDuration" :
                Regex.IsMatch(source, "减伤|承伤") ? "DamageTaken" : null;
            foreach (UpgradeEffect effect in node.Effects ?? Array.Empty<UpgradeEffect>())
                if (effect is TurretStatUpgradeEffect stat && (pattern == null || stat.Stat.ToString() == pattern)) return true;
            return false;
        }

        public void AppendPrompt(StringBuilder builder)
        {
            builder.AppendLine("动态升级能力目录（来自当前升级树，升级费用单独扣除）：");
            if (!HasUpgrades) { builder.AppendLine("无升级功能"); return; }
            foreach (UpgradeNodeDefinition node in upgrades.Config.Nodes)
            {
                if (node == null || node.Placeholder || string.IsNullOrWhiteSpace(node.Id)) continue;
                builder.Append("- purchase_upgrade | index=").Append(node.Id).Append(" | ")
                    .Append(node.DisplayName).Append(" | 标签=").Append(node.ShortLabel).Append(" | ").Append(node.Description)
                    .Append(" | 费用=").Append(node.KillCost).Append(" | 状态=").Append(upgrades.GetState(node))
                    .Append(" | 前置=");
                foreach (UpgradeNodeDefinition prerequisite in node.Prerequisites ?? Array.Empty<UpgradeNodeDefinition>())
                    if (prerequisite != null) builder.Append(prerequisite.Id).Append(' ');
                builder.Append(" | 固定效果=");
                foreach (UpgradeEffect effect in node.Effects ?? Array.Empty<UpgradeEffect>())
                {
                    if (effect is TurretStatUpgradeEffect stat)
                        builder.Append(stat.Stat).Append(" × ").Append(stat.Multiplier.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(' ');
                    else if (effect is RingTurretUpgradeEffect capacity)
                        builder.Append(capacity.RingId).Append(" 增加炮塔 ").Append(capacity.TurretCount).Append(' ');
                }
                builder.AppendLine();
            }
        }

        public static string PurchaseFailure(UpgradeSystem system, UpgradeNodeDefinition node)
        {
            if (system == null || node == null) return "升级功能不可用";
            switch (system.GetState(node))
            {
                case UpgradeNodeState.Purchased: return node.DisplayName + "已购买";
                case UpgradeNodeState.Locked:
                    var missing = new List<string>();
                    foreach (UpgradeNodeDefinition requirement in node.Prerequisites ?? Array.Empty<UpgradeNodeDefinition>())
                        if (requirement != null && system.GetState(requirement) != UpgradeNodeState.Purchased) missing.Add(requirement.DisplayName);
                    return node.DisplayName + "缺少前置升级：" + string.Join("、", missing);
                case UpgradeNodeState.InsufficientKills: return node.DisplayName + "资源不足，需要 " + node.KillCost + " 点";
                default: return node.DisplayName + "当前不可购买";
            }
        }
    }
}
