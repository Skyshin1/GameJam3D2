using System;
using System.Collections.Generic;

namespace AnchorDefense
{
    [Serializable]
    public sealed class AIParsedCommand
    {
        public string action;
        public string skill_id;
        public string[] operation_ids;
        public string target_zone_id;
        public string ring_mode;
        public string ring_id;
        public int ring_count;
        public AICommandGroup[] commands;

        public string Action => action;
        public string SkillId => skill_id;
        public string[] OperationIds => operation_ids;
        public string TargetZoneId => target_zone_id;
        public string RingMode => ring_mode;
        public string RingId => ring_id;
    }

    [Serializable]
    public sealed class AICommandGroup
    {
        public string type;
        public AICommandItem[] operations;
        public AIReservedSkill addSkill = new AIReservedSkill();
    }

    [Serializable]
    public sealed class AIReservedSkill { }

    [Serializable]
    public sealed class AICommandItem
    {
        public AIOperate operate;
        public string source;
    }

    [Serializable]
    public sealed class AIOperate
    {
        public string action;
        public string index;
        public string mode;
        public int? count;
    }

    // A validated operation carries its own source and parameters, never the whole sentence.
    public sealed class AICommandOperation
    {
        public string Type;
        public string Source;
        public ActiveSkillDefinition Skill;
        public AIParsedCommand Parameters;
        public int ZoneId = -1;
        public OrbitRingController Ring;
        public bool StopsAllRings;
        public string Description;
        public bool Executed;
        public bool IsStop => Skill.Effect.IsWorldOperation && Parameters.RingMode == "stop";
    }

    public sealed class AICommandRequest
    {
        public AICommandRequest(string systemPrompt, string playerText, string[] allowedSkillIds)
        {
            SystemPrompt = systemPrompt;
            PlayerText = playerText;
            AllowedSkillIds = allowedSkillIds ?? Array.Empty<string>();
        }

        public string SystemPrompt { get; }
        public string PlayerText { get; }
        public string[] AllowedSkillIds { get; }
    }

    public enum AIProviderError
    {
        None,
        NotConfigured,
        Cancelled,
        Timeout,
        Network,
        Unauthorized,
        RateLimited,
        InvalidResponse,
        Server
    }

    public sealed class AIProviderResult
    {
        private AIProviderResult(bool success, AIParsedCommand command, AIProviderError error, string message)
        {
            Success = success;
            Command = command;
            Error = error;
            Message = message;
        }

        public bool Success { get; }
        public AIParsedCommand Command { get; }
        public AIProviderError Error { get; }
        public string Message { get; }

        public static AIProviderResult Ok(AIParsedCommand command) =>
            new AIProviderResult(true, command, AIProviderError.None, string.Empty);

        public static AIProviderResult Fail(AIProviderError error, string message) =>
            new AIProviderResult(false, null, error, message);
    }

    public enum AICommandOutcome
    {
        Success,
        Busy,
        EmptyInput,
        InsufficientPoints,
        NotConfigured,
        Rejected,
        InvalidCommand,
        InvalidTarget,
        Cancelled,
        NetworkError,
        GameUnavailable,
        ExecutionFailed
    }

    public sealed class AICommandExecutionResult
    {
        public AICommandExecutionResult(AICommandOutcome outcome, string message,
            ActiveSkillDefinition skill = null, int zoneId = -1, string operationSummary = null,
            IReadOnlyList<AICommandOperation> operations = null)
        {
            Outcome = outcome;
            Message = message;
            Skill = skill;
            ZoneId = zoneId;
            OperationSummary = operationSummary;
            Operations = operations ?? Array.Empty<AICommandOperation>();
        }

        public AICommandOutcome Outcome { get; }
        public string Message { get; }
        public ActiveSkillDefinition Skill { get; }
        public int ZoneId { get; }
        public string OperationSummary { get; }
        public IReadOnlyList<AICommandOperation> Operations { get; }
        public bool Succeeded => Outcome == AICommandOutcome.Success;
    }

    public static class AICommandJsonValidator
    {
        private static readonly HashSet<string> LegacyKeys = new HashSet<string>
        {
            "action", "skill_id", "target_zone_id"
        };

        private static readonly HashSet<string> ComposeKeys = new HashSet<string>
        {
            "action", "operation_ids", "target_zone_id"
        };

        private static readonly HashSet<string> ComposeWithRingKeys = new HashSet<string>
        {
            "action", "operation_ids", "target_zone_id", "ring_mode", "ring_id"
        };

        public static bool HasExactTopLevelFields(string json) => TryParse(json, out _);

        public static bool TryParse(string json, out AIParsedCommand command)
        {
            command = null;
            if (string.IsNullOrWhiteSpace(json)) return false;
            int cursor = 0;
            if (!TryReadValue(json, ref cursor, 0, out object value)) return false;
            SkipWhitespace(json, ref cursor);
            if (cursor != json.Length || !(value is Dictionary<string, object> root)) return false;
            if (!root.ContainsKey("commands"))
            {
                if (!HasExactLegacyFields(json)) return false;
                command = UnityEngine.JsonUtility.FromJson<AIParsedCommand>(json);
                return command != null;
            }
            if (!HasKeys(root, "action", "commands") || !(root["action"] is string action) ||
                !(root["commands"] is List<object> groups) ||
                (action != "compose" && action != "reject") ||
                (action == "reject" ? groups.Count != 0 : groups.Count == 0)) return false;
            var parsedGroups = new List<AICommandGroup>();
            foreach (object groupValue in groups)
            {
                if (!(groupValue is Dictionary<string, object> group) ||
                    !HasKeys(group, "type", "operations", "addSkill") ||
                    !(group["type"] is string type) || (type != "区域" && type != "星环") ||
                    !(group["addSkill"] is Dictionary<string, object> reserved) || reserved.Count != 0 ||
                    !(group["operations"] is List<object> items) || items.Count == 0) return false;
                var parsedItems = new List<AICommandItem>();
                foreach (object itemValue in items)
                {
                    if (!(itemValue is Dictionary<string, object> item) ||
                        !HasKeys(item, "operate", "source") ||
                        !(item["source"] is string source) || string.IsNullOrWhiteSpace(source) ||
                        !(item["operate"] is Dictionary<string, object> op) ||
                        !HasKeys(op, type == "区域" ? new[] { "action", "index" } :
                            new[] { "action", "index", "mode", "count" }) ||
                        !(op["action"] is string id) || string.IsNullOrWhiteSpace(id) ||
                        (op["index"] != null && !(op["index"] is string))) return false;
                    var spec = new AIOperate { action = id, index = op["index"] as string };
                    if (type == "星环")
                    {
                        if (!(op["mode"] is string mode) ||
                            (mode != "adaptive" && mode != "spin" && mode != "once" &&
                             mode != "repeat" && mode != "stop") ||
                            (mode == "repeat" ? !(op["count"] is int n) || n < 1 || n > 20 :
                                op["count"] != null)) return false;
                        spec.mode = mode;
                        spec.count = op["count"] as int?;
                    }
                    parsedItems.Add(new AICommandItem { operate = spec, source = source });
                }
                parsedGroups.Add(new AICommandGroup
                    { type = type, operations = parsedItems.ToArray(), addSkill = new AIReservedSkill() });
            }
            command = new AIParsedCommand { action = action, commands = parsedGroups.ToArray() };
            return true;
        }

        private static bool HasKeys(Dictionary<string, object> value, params string[] keys) =>
            value.Count == keys.Length && Array.TrueForAll(keys, value.ContainsKey);

        // Small bounded JSON reader: duplicate keys, trailing data and wrong primitive types
        // are rejected before Unity deserialization can silently discard them.
        private static bool TryReadValue(string json, ref int index, int depth, out object value)
        {
            value = null;
            if (depth > 8) return false;
            SkipWhitespace(json, ref index);
            if (index >= json.Length) return false;
            if (json[index] == '"')
            {
                if (!TryReadString(json, ref index, out string text)) return false;
                value = text;
                return true;
            }
            if (Consume(json, ref index, '{'))
            {
                var map = new Dictionary<string, object>(StringComparer.Ordinal);
                value = map;
                SkipWhitespace(json, ref index);
                if (Consume(json, ref index, '}')) return true;
                while (index < json.Length)
                {
                    if (!TryReadString(json, ref index, out string key) || map.ContainsKey(key)) return false;
                    SkipWhitespace(json, ref index);
                    if (!Consume(json, ref index, ':') ||
                        !TryReadValue(json, ref index, depth + 1, out object child)) return false;
                    map.Add(key, child);
                    SkipWhitespace(json, ref index);
                    if (Consume(json, ref index, '}')) return true;
                    if (!Consume(json, ref index, ',')) return false;
                }
                return false;
            }
            if (Consume(json, ref index, '['))
            {
                var list = new List<object>();
                value = list;
                SkipWhitespace(json, ref index);
                if (Consume(json, ref index, ']')) return true;
                while (index < json.Length)
                {
                    if (!TryReadValue(json, ref index, depth + 1, out object child)) return false;
                    list.Add(child);
                    SkipWhitespace(json, ref index);
                    if (Consume(json, ref index, ']')) return true;
                    if (!Consume(json, ref index, ',')) return false;
                }
                return false;
            }
            if (index + 4 <= json.Length && string.CompareOrdinal(json, index, "null", 0, 4) == 0)
            {
                index += 4;
                return true;
            }
            int start = index;
            if (json[index] == '-') index++;
            while (index < json.Length && json[index] >= '0' && json[index] <= '9') index++;
            string number = json.Substring(start, index - start);
            if (number.Length == 0 || number == "-" ||
                (number[0] == '0' && number.Length > 1) ||
                (number.StartsWith("-0", StringComparison.Ordinal) && number.Length > 2) ||
                !int.TryParse(number, System.Globalization.NumberStyles.AllowLeadingSign,
                    System.Globalization.CultureInfo.InvariantCulture, out int integer)) return false;
            value = integer;
            return true;
        }

        private static bool HasExactLegacyFields(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;
            int index = 0;
            SkipWhitespace(json, ref index);
            if (!Consume(json, ref index, '{')) return false;

            var found = new HashSet<string>();
            SkipWhitespace(json, ref index);
            if (Consume(json, ref index, '}')) return false;

            while (index < json.Length)
            {
                if (!TryReadString(json, ref index, out string key) ||
                    (!LegacyKeys.Contains(key) && !ComposeWithRingKeys.Contains(key)) || !found.Add(key))
                {
                    return false;
                }

                SkipWhitespace(json, ref index);
                if (!Consume(json, ref index, ':')) return false;
                SkipWhitespace(json, ref index);
                bool isNull = false;
                bool validValue = key == "operation_ids"
                    ? TryReadStringArray(json, ref index)
                    : TryReadStringOrNull(json, ref index, out isNull);
                if (!validValue) return false;
                if (key == "action" && isNull) return false;
                SkipWhitespace(json, ref index);

                if (Consume(json, ref index, '}'))
                {
                    SkipWhitespace(json, ref index);
                    return index == json.Length &&
                           (found.SetEquals(LegacyKeys) || found.SetEquals(ComposeKeys) ||
                            found.SetEquals(ComposeWithRingKeys));
                }

                if (!Consume(json, ref index, ',')) return false;
                SkipWhitespace(json, ref index);
            }

            return false;
        }

        private static bool TryReadStringArray(string json, ref int index)
        {
            if (!Consume(json, ref index, '[')) return false;
            SkipWhitespace(json, ref index);
            if (Consume(json, ref index, ']')) return true;
            while (index < json.Length)
            {
                if (!TryReadString(json, ref index, out _)) return false;
                SkipWhitespace(json, ref index);
                if (Consume(json, ref index, ']')) return true;
                if (!Consume(json, ref index, ',')) return false;
                SkipWhitespace(json, ref index);
            }
            return false;
        }

        private static bool TryReadStringOrNull(string json, ref int index, out bool isNull)
        {
            isNull = false;
            if (index < json.Length && json[index] == '"')
            {
                return TryReadString(json, ref index, out _);
            }
            if (index + 4 <= json.Length &&
                string.CompareOrdinal(json, index, "null", 0, 4) == 0)
            {
                index += 4;
                isNull = true;
                return true;
            }
            return false;
        }

        private static bool TryReadString(string json, ref int index, out string value)
        {
            value = null;
            SkipWhitespace(json, ref index);
            if (!Consume(json, ref index, '"')) return false;
            var result = new System.Text.StringBuilder();
            while (index < json.Length)
            {
                char c = json[index++];
                if (c == '"')
                {
                    value = result.ToString();
                    return true;
                }
                if (c < 0x20) return false;
                if (c != '\\')
                {
                    result.Append(c);
                    continue;
                }
                if (index >= json.Length) return false;
                char escape = json[index++];
                switch (escape)
                {
                    case '"': result.Append('"'); break;
                    case '\\': result.Append('\\'); break;
                    case '/': result.Append('/'); break;
                    case 'b': result.Append('\b'); break;
                    case 'f': result.Append('\f'); break;
                    case 'n': result.Append('\n'); break;
                    case 'r': result.Append('\r'); break;
                    case 't': result.Append('\t'); break;
                    case 'u':
                        if (index + 4 > json.Length ||
                            !ushort.TryParse(json.Substring(index, 4),
                                System.Globalization.NumberStyles.HexNumber,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out ushort codePoint)) return false;
                        result.Append((char)codePoint);
                        index += 4;
                        break;
                    default: return false;
                }
            }
            return false;
        }

        private static void SkipWhitespace(string value, ref int index)
        {
            while (index < value.Length && char.IsWhiteSpace(value[index])) index++;
        }

        private static bool Consume(string value, ref int index, char expected)
        {
            if (index >= value.Length || value[index] != expected) return false;
            index++;
            return true;
        }
    }
}
