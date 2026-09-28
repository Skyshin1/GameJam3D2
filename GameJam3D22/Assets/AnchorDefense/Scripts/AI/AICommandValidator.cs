using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AnchorDefense
{
    public sealed class AICommandValidator
    {
        public bool TryNormalize(AIParsedCommand command, string playerText,
            IReadOnlyList<ActiveSkillDefinition> skills, int maximumOperations,
            out AICommandOperation[] operations, out string error)
        {
            operations = null;
            error = "模型没有返回有效命令";
            if (command == null || string.IsNullOrWhiteSpace(playerText)) return false;
            if (command.Action == "reject")
            {
                error = "这条指令超出当前锚核能力，未扣指令点";
                return false;
            }
            if (command.Action != "compose" && command.Action != "cast_skill") return false;
            AICommandGroup[] groups = command.commands;
            if (groups == null)
            {
                // Legacy targets are advisory. Resolve them from the original text below.
                string[] ids = command.Action == "cast_skill" ? new[] { command.SkillId } : command.OperationIds;
                if (ids == null || ids.Length == 0) return false;
                var adapted = new List<AICommandGroup>();
                foreach (string id in ids)
                {
                    ActiveSkillDefinition skill = FindSkill(id, skills);
                    if (skill == null) { error = "模型选择了不存在或尚未解锁的操作"; return false; }
                    bool ring = skill.Effect.IsWorldOperation;
                    if (!ring && (command.RingMode != null || command.RingId != null))
                    { error = "星环控制参数与区域操作不匹配"; return false; }
                    if (ring && command.RingId != null && command.RingId != "inner" &&
                        command.RingId != "middle" && command.RingId != "outer")
                    { error = "模型返回了不存在的星环"; return false; }
                    adapted.Add(new AICommandGroup
                    {
                        type = ring ? "星环" : "区域",
                        operations = new[] { new AICommandItem
                        {
                            source = playerText,
                            operate = new AIOperate { action = id, index = null,
                                mode = ring ? command.RingMode ?? "adaptive" : null,
                                count = command.ring_count > 0 ? (int?)command.ring_count : null }
                        } }
                    });
                }
                groups = adapted.ToArray();
            }
            var normalized = new List<AICommandOperation>();
            int previousPosition = 0;
            foreach (AICommandGroup group in groups)
            {
                if (group == null || (group.type != "区域" && group.type != "星环") ||
                    group.addSkill == null || group.operations == null || group.operations.Length == 0)
                { error = "指令分组无效"; return false; }
                foreach (AICommandItem item in group.operations)
                {
                    AIOperate spec = item?.operate;
                    if (spec == null || string.IsNullOrWhiteSpace(item.source))
                    { error = "指令缺少操作或原文片段"; return false; }
                    int position = playerText.IndexOf(item.source, previousPosition, StringComparison.Ordinal);
                    if (position < 0)
                    { error = "操作原文不存在或顺序不匹配，未扣指令点"; return false; }
                    previousPosition = position;
                    ActiveSkillDefinition skill = FindSkill(spec.action, skills);
                    if (skill == null)
                    { error = "模型选择了不存在或尚未解锁的操作"; return false; }
                    bool ring = skill.Effect.IsWorldOperation;
                    if (ring != (group.type == "星环"))
                    { error = "区域与星环操作类型不匹配，未扣指令点"; return false; }
                    if (IsNegatedSource(playerText, item.source, position, ring))
                    { error = "操作与玩家的否定指令冲突，未扣指令点"; return false; }
                    var parameters = new AIParsedCommand
                        { action = "compose", operation_ids = new[] { skill.Id } };
                    if (ring)
                    {
                        if (!HasRingControlIntent(item.source))
                        { error = "原文没有操控星环的意图，未扣指令点"; return false; }
                        if (spec.index != null && spec.index != "01" && spec.index != "02" && spec.index != "03")
                        { error = "模型返回了不存在的星环"; return false; }
                        if (spec.mode != "adaptive" && spec.mode != "spin" && spec.mode != "once" &&
                            spec.mode != "repeat" && spec.mode != "stop")
                        { error = "模型返回了不支持的星环控制方式"; return false; }
                        if (spec.mode == "repeat" ? !spec.count.HasValue || spec.count < 1 || spec.count > 20 :
                            spec.count.HasValue)
                        { error = "星环旋转次数无效"; return false; }
                        parameters.ring_mode = spec.mode;
                        parameters.ring_count = spec.count ?? 0;
                        // The ring effect resolves the source's explicit ring before advisory indices.
                        parameters.ring_id = spec.index == "01" ? "inner" : spec.index == "02" ? "middle" :
                            spec.index == "03" ? "outer" : null;
                    }
                    else
                    {
                        if (spec.mode != null || spec.count.HasValue ||
                            (spec.index != null && !TryParseZoneId(spec.index, out _)))
                        { error = "区域参数无效"; return false; }
                        // A fragment such as '攻击' taken from '攻击 C02' must not erase
                        // the explicit player target and let automatic scoring choose C01.
                        int clauseStart = position;
                        int clauseEnd = position + item.source.Length;
                        while (clauseStart > 0 && "，,、；;。\n".IndexOf(playerText[clauseStart - 1]) < 0) clauseStart--;
                        while (clauseEnd < playerText.Length && "，,、；;。\n".IndexOf(playerText[clauseEnd]) < 0) clauseEnd++;
                        string clause = playerText.Substring(clauseStart, clauseEnd - clauseStart);
                        const string targetWords = @"(?i)C\d+|这里|这块|选中|当前区域|左上|左下|右上|右下|左侧|左边|左区|右侧|右边|右区|上方|上侧|上区|下方|下侧|下区";
                        if (Regex.IsMatch(clause, targetWords) && !Regex.IsMatch(item.source, targetWords))
                        { error = "操作原文遗漏了玩家指定的区域，未扣指令点"; return false; }
                        parameters.target_zone_id = spec.index;
                    }
                    normalized.Add(new AICommandOperation
                        { Type = group.type, Source = item.source, Skill = skill, Parameters = parameters });
                }
            }
            if (normalized.Count == 0 || normalized.Count > Math.Max(1, maximumOperations))
            { error = "指令操作数量无效"; return false; }
            operations = normalized.ToArray();
            error = string.Empty;
            return true;
        }

        private static ActiveSkillDefinition FindSkill(string id, IReadOnlyList<ActiveSkillDefinition> skills)
        {
            for (int i = 0; skills != null && i < skills.Count; i++)
                if (skills[i] != null && skills[i].Effect != null && skills[i].Id == id) return skills[i];
            return null;
        }

        public static bool HasRingControlIntent(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            string subject = @"(?:星环|轨道|[内中外]环|[内中外]轨|第[一二三四五六七八九\d]+(?:条|个)?(?:星环|轨道|轨|环)|[一二三\d]+号(?:星环|轨道|轨|环))";
            string verb = @"(?:旋转|转动|操作|操控|控制|接管|调度|停止|停下|不再转|别转|转(?!角)|结束)";
            // Adjacent verb/subject expressions avoid treating '攻击轨道附近的敌人' as control.
            return Regex.IsMatch(text, verb + @"(?:一下|所有的?|全部的?|这[个条]|第|持续|一直|\s)*" + subject) ||
                   Regex.IsMatch(text, subject + @"(?:[一二三\d]*|一直|持续|继续|只|每次|顺时针|逆时针|反时针|向[左右]|由星核|自动|\s)*" + verb);
        }

        private static bool IsNegatedSource(string playerText, string source, int position, bool ring)
        {
            int start = position;
            while (start > 0 && "，,、；;。\n".IndexOf(playerText[start - 1]) < 0) start--;
            string scope = playerText.Substring(start, position - start) + source;
            if (ring && RingRotationSkillEffect.IsExplicitStop(scope)) return false;
            if (ring && (RingRotationSkillEffect.TryReadRepeatCount(source, out _) ||
                Regex.IsMatch(source, "转一下|就停|后停")))
                return Regex.IsMatch(scope, "不要|别|禁止|取消|不想|不许|不能");
            return Regex.IsMatch(scope, "不要|别|禁止|取消|不想|不许|不能|停止|停下");
        }

        // Conservative local fallback for simple phrases when cloud JSON is malformed or
        // rejects an obviously supported combination. Uses asset keywords, not hard-coded IDs.
        public bool TryResolveComposableIntent(string playerText,
            IReadOnlyList<ActiveSkillDefinition> availableSkills, int maximumOperations,
            out AIParsedCommand command)
        {
            command = null;
            if (string.IsNullOrWhiteSpace(playerText) || availableSkills == null) return false;
            string text = playerText.Trim();
            var groups = new List<AICommandGroup>();
            var compatibilityIds = new List<string>();
            string[] clauses = Regex.Split(text, @"[，,、；;。\\n]|然后");
            // A finite ring rotation often uses a comma between its angle and count.
            bool areaAction = false;
            foreach (ActiveSkillDefinition skill in availableSkills)
                if (skill != null && skill.Effect != null && !skill.Effect.IsWorldOperation &&
                    skill.Keywords != null && Array.Exists(skill.Keywords, keyword =>
                        !string.IsNullOrWhiteSpace(keyword) && keyword.Length >= 2 &&
                        !IsSharedKeyword(keyword, availableSkills) && text.Contains(keyword))) areaAction = true;
            if (!areaAction && HasRingControlIntent(text) &&
                RingRotationSkillEffect.TryReadRepeatCount(text, out _)) clauses = new[] { text };

            foreach (string rawClause in clauses)
            {
                string clause = rawClause.Trim();
                if (clause.Length == 0) continue;
                int position = text.IndexOf(clause, StringComparison.Ordinal);
                var matches = new List<KeyValuePair<int, ActiveSkillDefinition>>();
                foreach (ActiveSkillDefinition skill in availableSkills)
                {
                    if (skill == null || skill.Effect == null) continue;
                    int firstMatch = int.MaxValue;
                    if (skill.Effect.IsWorldOperation)
                    {
                        if (!HasRingControlIntent(clause)) continue;
                        firstMatch = Regex.Match(clause,
                            "旋转|转动|操作|操控|控制|接管|调度|停止|停下|不再转|别转|转|结束").Index;
                    }
                    else
                    {
                        for (int j = 0; skill.Keywords != null && j < skill.Keywords.Length && j < 3; j++)
                        {
                            string keyword = skill.Keywords[j];
                            if (string.IsNullOrWhiteSpace(keyword) || keyword.Length < 2 ||
                                IsSharedKeyword(keyword, availableSkills)) continue;
                            int keywordPosition = clause.IndexOf(keyword, StringComparison.Ordinal);
                            if (keywordPosition >= 0) firstMatch = Math.Min(firstMatch, keywordPosition);
                        }
                    }
                    if (firstMatch == int.MaxValue) continue;
                    if (IsNegatedSource(text, clause, position, skill.Effect.IsWorldOperation)) return false;
                    matches.Add(new KeyValuePair<int, ActiveSkillDefinition>(firstMatch, skill));
                }
                matches.Sort((a, b) => a.Key.CompareTo(b.Key));
                foreach (var match in matches)
                {
                    ActiveSkillDefinition skill = match.Value;
                    bool ring = skill.Effect.IsWorldOperation;
                    int? count = null;
                    string mode = null;
                    if (ring)
                    {
                        if (RingRotationSkillEffect.TryReadRepeatCount(clause, out int parsedCount))
                        { mode = parsedCount == 1 ? "once" : "repeat"; if (parsedCount != 1) count = parsedCount; }
                        else mode = RingRotationSkillEffect.IsExplicitStop(clause) ? "stop" : "adaptive";
                    }
                    groups.Add(new AICommandGroup { type = ring ? "星环" : "区域",
                        operations = new[] { new AICommandItem { source = clause,
                            operate = new AIOperate { action = skill.Id, mode = mode, count = count } } } });
                    compatibilityIds.Add(skill.Id);
                }
            }
            if (groups.Count == 0 || groups.Count > Math.Max(1, maximumOperations)) return false;
            command = new AIParsedCommand
            {
                action = "compose", commands = groups.ToArray(), operation_ids = compatibilityIds.ToArray(),
                ring_mode = groups.Count == 1 && groups[0].operations[0].operate.mode == "stop" ? "stop" : null
            };
            return true;
        }

        private static bool IsSharedKeyword(string keyword,
            IReadOnlyList<ActiveSkillDefinition> availableSkills)
        {
            int count = 0;
            for (int i = 0; i < availableSkills.Count; i++)
            {
                string[] keywords = availableSkills[i] != null ? availableSkills[i].Keywords : null;
                if (keywords == null) continue;
                for (int j = 0; j < keywords.Length && j < 3; j++)
                {
                    if (!string.Equals(keyword, keywords[j], StringComparison.Ordinal)) continue;
                    count++;
                    break;
                }
                if (count > 1) return true;
            }
            return false;
        }

        // Only used when the model rejects a clear, location-free command.
        // Keywords live on the skill assets so adding a skill needs no parser changes.
        public bool TryResolveObviousIntent(string playerText,
            IReadOnlyList<ActiveSkillDefinition> availableSkills, out AIParsedCommand command)
        {
            command = null;
            if (string.IsNullOrWhiteSpace(playerText) || availableSkills == null) return false;
            string text = playerText.Trim();
            if (HasExplicitLocation(text)) return false;

            ActiveSkillDefinition match = null;
            for (int i = 0; i < availableSkills.Count; i++)
            {
                ActiveSkillDefinition skill = availableSkills[i];
                if (skill == null || skill.Effect == null || skill.Keywords == null) continue;
                if (skill.Effect.IsWorldOperation && !HasRingControlIntent(text)) continue;
                bool matches = false;
                for (int j = 0; j < skill.Keywords.Length; j++)
                {
                    string keyword = skill.Keywords[j];
                    if (!string.IsNullOrWhiteSpace(keyword) && keyword.Length >= 2 &&
                        text.StartsWith(keyword, StringComparison.Ordinal))
                    {
                        matches = true;
                        break;
                    }
                }

                if (!matches) continue;
                if (match != null) return false; // Ambiguous: do not guess or spend points.
                match = skill;
            }

            if (match == null) return false;
            command = new AIParsedCommand
            {
                action = "cast_skill",
                skill_id = match.Id,
                target_zone_id = null
            };
            return true;
        }

        public bool TryValidate(AIParsedCommand command,
            IReadOnlyList<ActiveSkillDefinition> availableSkills,
            out ActiveSkillDefinition skill, out string error)
        {
            skill = null;
            error = string.Empty;
            if (command == null)
            {
                error = "模型没有返回有效命令";
                return false;
            }

            if (string.Equals(command.Action, "reject", StringComparison.Ordinal))
            {
                error = "星核无法理解这条指令";
                return false;
            }

            if (!string.Equals(command.Action, "cast_skill", StringComparison.Ordinal))
            {
                error = "模型返回了不允许的操作";
                return false;
            }

            if (availableSkills != null)
            {
                for (int i = 0; i < availableSkills.Count; i++)
                {
                    ActiveSkillDefinition candidate = availableSkills[i];
                    if (candidate != null &&
                        string.Equals(candidate.Id, command.SkillId, StringComparison.Ordinal))
                    {
                        skill = candidate;
                        break;
                    }
                }
            }

            if (skill == null || skill.Effect == null)
            {
                error = "模型选择了不存在或尚未解锁的技能";
                return false;
            }

            if (command.TargetZoneId != null &&
                !TryParseZoneId(command.TargetZoneId, out _))
            {
                error = "模型选择了不存在的区域";
                return false;
            }

            return true;
        }

        public bool TryValidateOperations(AIParsedCommand command,
            IReadOnlyList<ActiveSkillDefinition> availableSkills, int maximumOperations,
            out ActiveSkillDefinition[] operations, out string error)
        {
            operations = null;
            error = string.Empty;
            if (command == null)
            {
                error = "模型没有返回有效命令";
                return false;
            }
            if (string.Equals(command.Action, "reject", StringComparison.Ordinal))
            {
                error = "这条指令超出当前锚核能力，未扣指令点";
                return false;
            }

            string[] ids;
            if (string.Equals(command.Action, "compose", StringComparison.Ordinal))
                ids = command.OperationIds;
            else if (string.Equals(command.Action, "cast_skill", StringComparison.Ordinal))
                ids = new[] { command.SkillId }; // Support old providers and saved mocks.
            else
            {
                error = "模型返回了不允许的操作";
                return false;
            }

            if (ids == null || ids.Length == 0 || ids.Length > Math.Max(1, maximumOperations))
            {
                error = "指令操作数量无效";
                return false;
            }
            if (command.TargetZoneId != null && !TryParseZoneId(command.TargetZoneId, out _))
            {
                error = "模型选择了不存在的区域";
                return false;
            }

            operations = new ActiveSkillDefinition[ids.Length];
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < ids.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(ids[i]) || !seen.Add(ids[i]))
                {
                    error = "指令包含重复或空操作";
                    return false;
                }
                for (int j = 0; availableSkills != null && j < availableSkills.Count; j++)
                {
                    ActiveSkillDefinition candidate = availableSkills[j];
                    if (candidate != null && candidate.Effect != null &&
                        string.Equals(candidate.Id, ids[i], StringComparison.Ordinal))
                    {
                        operations[i] = candidate;
                        break;
                    }
                }
                if (operations[i] != null) continue;
                error = "模型选择了不存在或尚未解锁的操作";
                return false;
            }

            bool hasRingOperation = Array.Exists(operations,
                operation => operation != null && operation.Id == "rotate_ring");
            if (command.RingMode != null && command.RingMode != "adaptive" &&
                command.RingMode != "spin" && command.RingMode != "once" &&
                command.RingMode != "stop")
            {
                error = "模型返回了不支持的星环控制方式 1111";
                return false;
            }
            if (command.RingId != null && command.RingId != "inner" &&
                command.RingId != "middle" && command.RingId != "outer")
            {
                error = "模型返回了不存在的星环";
                return false;
            }
            if (!hasRingOperation && (command.RingMode != null || command.RingId != null) ||
                command.RingMode == "stop" && ids.Length != 1)
            {
                error = "星环控制参数与操作不匹配";
                return false;
            }
            return true;
        }

        public static bool TryParseZoneId(string value, out int zoneId)
        {
            zoneId = -1;
            if (string.IsNullOrWhiteSpace(value) || value.Length != 3 ||
                (value[0] != 'C' && value[0] != 'c') ||
                !int.TryParse(value.Substring(1), out int oneBased))
            {
                return false;
            }
            zoneId = oneBased - 1;
            return zoneId >= 0 && zoneId < CubeZoneConfig.ZoneCount;
        }

        private static bool HasExplicitLocation(string text)
        {
            string[] locationWords = { "左", "右", "上方", "下方", "前方", "后方", "这里", "这块", "选中" };
            for (int i = 0; i < locationWords.Length; i++)
            {
                if (text.Contains(locationWords[i])) return true;
            }
            for (int i = 1; i <= CubeZoneConfig.ZoneCount; i++)
            {
                if (text.IndexOf($"C{i:00}", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }
    }
}
