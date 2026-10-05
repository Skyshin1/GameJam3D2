using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AnchorDefense
{
    public enum CompanionPurpose { Chatter, Recommendation, PurchaseFeedback, PlayerTurn, RescueQuestion, RescueAnswer, RescueResult, CommandResult }
    [Serializable] public sealed class CompanionChatMessage
    {
        public string role;
        public string content;
        public CompanionChatMessage(string speaker, string text) { role = speaker; content = text; }
    }
    public sealed class AICompanionRequest
    {
        public CompanionPurpose Purpose;
        public CompanionChatMessage[] Messages;
        public float Temperature;
    }
    public sealed class AICompanionReply
    {
        public string Kind, Text, NodeId, Decision, Summary;
        public AIParsedCommand Command;
        public bool Success;
        public static AICompanionReply Failure() => new AICompanionReply();

        public static bool TryParse(string json, out AICompanionReply reply)
        {
            reply = null;
            if (!AICommandJsonValidator.TryReadObject(json, out var root) || root.Count != 6) return false;
            foreach (string key in new[] { "kind", "text", "node_id", "decision", "command", "summary" })
                if (!root.ContainsKey(key)) return false;
            foreach (string key in new[] { "kind", "text", "node_id", "decision", "summary" })
                if (root[key] != null && !(root[key] is string)) return false;
            string kind = root["kind"] as string;
            if (kind != "chat" && kind != "recommendation" && kind != "rescue" && kind != "command") return false;
            AIParsedCommand command = null;
            if (kind == "command")
            {
                if (!(root["command"] is Dictionary<string, object>) ||
                    !AICommandJsonValidator.TryParse(Serialize(root["command"]), out command)) return false;
            }
            else if (root["command"] != null) return false;
            string decision = root["decision"] as string;
            if (decision != null && decision != "accept" && decision != "decline" && decision != "unclear") return false;
            reply = new AICompanionReply { Success = true, Kind = kind, Text = root["text"] as string,
                NodeId = root["node_id"] as string, Decision = decision, Command = command,
                Summary = root["summary"] as string };
            return true;
        }

        private static string Serialize(object value)
        {
            if (value == null) return "null";
            if (value is string s) return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                .Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t") + "\"";
            if (value is Dictionary<string, object> map)
                return "{" + string.Join(",", map.Select(p => Serialize(p.Key) + ":" + Serialize(p.Value))) + "}";
            if (value is List<object> list) return "[" + string.Join(",", list.Select(Serialize)) + "]";
            return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
    public interface IAICompanionProvider
    {
        bool IsConfigured { get; }
        Task<AICompanionReply> RequestAsync(AICompanionRequest request, CancellationToken cancellationToken);
    }

    public sealed class CompanionMemory
    {
        private readonly List<CompanionChatMessage> archive = new List<CompanionChatMessage>();
        private readonly List<string> facts = new List<string>();
        public IReadOnlyList<string> Facts => facts;
        public void RecordFact(string fact)
        {
            facts.Add(fact);
            if (facts.Count > 40) facts.RemoveAt(0);
        }
        private int summarizedThrough;
        public string Summary { get; private set; } = string.Empty;
        public IReadOnlyList<CompanionChatMessage> Archive => archive;
        public void Add(string role, string content)
        {
            if (!string.IsNullOrWhiteSpace(content)) archive.Add(new CompanionChatMessage(role, content));
        }
        public CompanionChatMessage[] Recent() => archive.Skip(Math.Max(0, archive.Count - 20)).ToArray();
        public string EarlierContext()
        {
            var text = new StringBuilder(Summary);
            int until = Math.Max(0, archive.Count - 20);
            for (int i = Math.Max(summarizedThrough, until - 40); i < until; i++)
                text.Append('\n').Append(archive[i].role).Append(": ").Append(archive[i].content);
            return text.ToString();
        }
        public void UpdateSummary(string summary, int requestMessageCount)
        {
            if (string.IsNullOrWhiteSpace(summary) || summary.Length > 1000) return;
            Summary = summary;
            summarizedThrough = Math.Max(summarizedThrough, Math.Max(0, requestMessageCount - 20));
        }
    }
}
