using System;
using System.Collections.Generic;

namespace AnchorDefense
{
    public sealed class AICommandValidator
    {
        // Conservative local fallback for simple phrases when cloud JSON is malformed or
        // rejects an obviously supported combination. Uses asset keywords, not hard-coded IDs.
        public bool TryResolveComposableIntent(string playerText,
            IReadOnlyList<ActiveSkillDefinition> availableSkills, int maximumOperations,
            out AIParsedCommand command)
        {
            command = null;
            if (string.IsNullOrWhiteSpace(playerText) || availableSkills == null) return false;
            string text = playerText.Trim();
            bool stopRing = text.Contains("停止") || text.Contains("停下") ||
                text.Contains("不再转") || text.Contains("别转") ||
                text.Contains("结束星环控制") || text.Contains("结束轨道控制");
            if (text.Contains("不要停止") || text.Contains("不想停止") ||
                text.Contains("不要停") || text.Contains("不许停") ||
                text.Contains("不能停") || text.Contains("别停")) return false;
            string[] negations = { "不要", "别", "禁止", "取消", "不想" };
            for (int i = 0; i < negations.Length; i++)
                if (!stopRing && text.Contains(negations[i])) return false;

            var matches = new List<KeyValuePair<int, string>>();
            for (int i = 0; i < availableSkills.Count; i++)
            {
                ActiveSkillDefinition skill = availableSkills[i];
                if (skill == null || skill.Effect == null || skill.Keywords == null) continue;
                int firstMatch = int.MaxValue;
                for (int j = 0; j < skill.Keywords.Length && j < 3; j++)
                {
                    string keyword = skill.Keywords[j];
                    if (string.IsNullOrWhiteSpace(keyword) || keyword.Length < 2 ||
                        IsSharedKeyword(keyword, availableSkills)) continue;
                    int position = text.IndexOf(keyword, StringComparison.Ordinal);
                    if (position >= 0) firstMatch = Math.Min(firstMatch, position);
                }
                if (firstMatch != int.MaxValue)
                    matches.Add(new KeyValuePair<int, string>(firstMatch, skill.Id));
            }

            if (matches.Count == 0 || matches.Count > Math.Max(1, maximumOperations)) return false;
            matches.Sort((a, b) => a.Key.CompareTo(b.Key));
            string[] ids = new string[matches.Count];
            for (int i = 0; i < ids.Length; i++) ids[i] = matches[i].Value;
            if (stopRing && (ids.Length != 1 || ids[0] != "rotate_ring")) return false;
            command = new AIParsedCommand
            {
                action = "compose", operation_ids = ids, target_zone_id = null,
                ring_mode = stopRing && Array.Exists(ids, id => id == "rotate_ring")
                    ? "stop" : null
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
                error = "模型返回了不支持的星环控制方式";
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
