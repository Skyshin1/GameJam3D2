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

        public string Action => action;
        public string SkillId => skill_id;
        public string[] OperationIds => operation_ids;
        public string TargetZoneId => target_zone_id;
        public string RingMode => ring_mode;
        public string RingId => ring_id;
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
            ActiveSkillDefinition skill = null, int zoneId = -1, string operationSummary = null)
        {
            Outcome = outcome;
            Message = message;
            Skill = skill;
            ZoneId = zoneId;
            OperationSummary = operationSummary;
        }

        public AICommandOutcome Outcome { get; }
        public string Message { get; }
        public ActiveSkillDefinition Skill { get; }
        public int ZoneId { get; }
        public string OperationSummary { get; }
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

        public static bool HasExactTopLevelFields(string json)
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
