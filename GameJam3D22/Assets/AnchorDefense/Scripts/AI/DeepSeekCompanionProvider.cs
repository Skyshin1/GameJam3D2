using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace AnchorDefense
{
    public sealed class DeepSeekCompanionProvider : IAICompanionProvider
    {
        private readonly ResolvedAIProviderSettings settings;
        private readonly int timeout;
        public bool IsConfigured => settings.IsConfigured;
        public DeepSeekCompanionProvider(AICommandConfig config)
        {
            settings = AIProviderSettings.Load(config);
            timeout = config != null ? Mathf.CeilToInt(config.TimeoutSeconds) : 12;
        }
        public async Task<AICompanionReply> RequestAsync(AICompanionRequest request, CancellationToken cancellationToken)
        {
            if (!IsConfigured || cancellationToken.IsCancellationRequested) return AICompanionReply.Failure();
            string url = settings.BaseUrl.TrimEnd('/');
            if (!url.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)) url += "/chat/completions";
            var body = new Body { model = settings.Model, messages = request.Messages,
                temperature = request.Temperature, max_tokens = request.Purpose == CompanionPurpose.PlayerTurn ? 2500 : 1000 };
            using var web = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            web.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(body)));
            web.downloadHandler = new DownloadHandlerBuffer();
            web.timeout = Mathf.Max(1, timeout);
            web.SetRequestHeader("Content-Type", "application/json");
            web.SetRequestHeader("Authorization", "Bearer " + settings.ApiKey);
            try
            {
                var pending = web.SendWebRequest();
                while (!pending.isDone)
                {
                    if (cancellationToken.IsCancellationRequested) { web.Abort(); return AICompanionReply.Failure(); }
                    await Task.Yield();
                }
                if (cancellationToken.IsCancellationRequested || web.result != UnityWebRequest.Result.Success)
                    return AICompanionReply.Failure();
                Envelope envelope = JsonUtility.FromJson<Envelope>(web.downloadHandler.text);
                if (envelope?.choices == null || envelope.choices.Length == 0 ||
                    envelope.choices[0].finish_reason == "length") return AICompanionReply.Failure();
                return AICompanionReply.TryParse(envelope.choices[0].message?.content, out var reply)
                    ? reply : AICompanionReply.Failure();
            }
            catch (Exception) { return AICompanionReply.Failure(); }
        }
        [Serializable] private sealed class Body
        {
            public string model;
            public CompanionChatMessage[] messages;
            public float temperature;
            public int max_tokens;
            public bool stream;
            public Format response_format = new Format();
            public Thinking thinking = new Thinking();
        }
        [Serializable] private sealed class Format { public string type = "json_object"; }
        [Serializable] private sealed class Thinking { public string type = "disabled"; }
        [Serializable] private sealed class Envelope { public Choice[] choices; }
        [Serializable] private sealed class Choice { public CompanionChatMessage message; public string finish_reason; }
    }
}
