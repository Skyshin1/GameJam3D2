using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace AnchorDefense
{
    public sealed class OpenAICompatibleCommandProvider : IAICommandProvider
    {
        private readonly AICommandConfig config;
        private readonly ResolvedAIProviderSettings settings;

        public OpenAICompatibleCommandProvider(AICommandConfig commandConfig)
        {
            config = commandConfig;
            settings = AIProviderSettings.Load(commandConfig);
        }

        public bool IsConfigured => config != null && settings.IsConfigured;

        public async Task<AIProviderResult> RequestAsync(
            AICommandRequest commandRequest, CancellationToken cancellationToken)
        {
            if (!IsConfigured)
            {
                return AIProviderResult.Fail(AIProviderError.NotConfigured, "AI 服务未配置");
            }

            string payload;
            try
            {
                payload = SerializeRequest(commandRequest, settings, config);
            }
            catch (Exception exception)
            {
                return AIProviderResult.Fail(AIProviderError.InvalidResponse, exception.Message);
            }

            using var request = new UnityWebRequest(BuildEndpoint(settings.BaseUrl), UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = Mathf.Max(1, Mathf.CeilToInt(config.TimeoutSeconds));
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + settings.ApiKey);

            UnityWebRequestAsyncOperation operation;
            try
            {
                operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        request.Abort();
                        return AIProviderResult.Fail(AIProviderError.Cancelled, "请求已取消");
                    }
                    await Task.Yield();
                }
            }
            catch (Exception exception)
            {
                return cancellationToken.IsCancellationRequested
                    ? AIProviderResult.Fail(AIProviderError.Cancelled, "请求已取消")
                    : AIProviderResult.Fail(AIProviderError.Network, exception.Message);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return AIProviderResult.Fail(AIProviderError.Cancelled, "请求已取消");
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                return BuildFailure(request);
            }

            return ParseResponse(request.downloadHandler.text);
        }

        private static string SerializeRequest(AICommandRequest request,
            ResolvedAIProviderSettings providerSettings, AICommandConfig commandConfig)
        {
            bool isDeepSeek = IsDeepSeekEndpoint(providerSettings.BaseUrl);
            ChatCompletionRequest body = isDeepSeek
                ? new DeepSeekChatCompletionRequest
                {
                    // Command extraction is short and deterministic; DeepSeek's default
                    // high-effort thinking can consume the entire output token budget.
                    thinking = new ThinkingMode { type = "disabled" }
                }
                : new ChatCompletionRequest();
            body.model = providerSettings.Model;
            body.temperature = commandConfig.Temperature;
            body.max_tokens = 512;
            body.stream = false;
            body.messages = new[]
            {
                new ChatMessage { role = "system", content = request.SystemPrompt },
                new ChatMessage { role = "user", content = request.PlayerText }
            };
            body.response_format = new ResponseFormat { type = "json_object" };
            return isDeepSeek
                ? JsonUtility.ToJson((DeepSeekChatCompletionRequest)body)
                : JsonUtility.ToJson(body);
        }

        private static AIProviderResult ParseResponse(string responseJson)
        {
            try
            {
                ChatCompletionResponse response = JsonUtility.FromJson<ChatCompletionResponse>(responseJson);
                if (response?.choices == null || response.choices.Length == 0 ||
                    response.choices[0]?.message == null)
                {
                    return AIProviderResult.Fail(AIProviderError.InvalidResponse, "模型没有返回命令");
                }

                ChatChoice choice = response.choices[0];
                if (string.Equals(choice.finish_reason, "length", StringComparison.Ordinal))
                {
                    return AIProviderResult.Fail(AIProviderError.InvalidResponse,
                        "模型输出被截断，请重试");
                }
                string content = choice.message.content;
                if (string.IsNullOrWhiteSpace(content))
                {
                    return AIProviderResult.Fail(AIProviderError.InvalidResponse,
                        "模型返回了空内容，请重试");
                }
                if (!AICommandJsonValidator.HasExactTopLevelFields(content))
                {
                    return AIProviderResult.Fail(AIProviderError.InvalidResponse,
                        "模型未按约定返回技能指令");
                }

                AIParsedCommand command = JsonUtility.FromJson<AIParsedCommand>(content);
                return command != null
                    ? AIProviderResult.Ok(command)
                    : AIProviderResult.Fail(AIProviderError.InvalidResponse, "模型命令无法解析");
            }
            catch (Exception exception)
            {
                return AIProviderResult.Fail(AIProviderError.InvalidResponse, exception.Message);
            }
        }

        private static AIProviderResult BuildFailure(UnityWebRequest request)
        {
            long code = request.responseCode;
            if (code == 401 || code == 403)
            {
                return AIProviderResult.Fail(AIProviderError.Unauthorized, "API Key 无效或无权限");
            }
            if (code == 429)
            {
                return AIProviderResult.Fail(AIProviderError.RateLimited, "AI 请求过于频繁或额度不足");
            }
            if (code >= 500)
            {
                return AIProviderResult.Fail(AIProviderError.Server, "AI 服务暂时不可用");
            }
            if (request.error != null && request.error.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return AIProviderResult.Fail(AIProviderError.Timeout, "AI 请求超时");
            }
            return AIProviderResult.Fail(AIProviderError.Network,
                string.IsNullOrWhiteSpace(request.error) ? "网络请求失败" : request.error);
        }

        private static string BuildEndpoint(string baseUrl)
        {
            string value = (baseUrl ?? string.Empty).Trim().TrimEnd('/');
            if (value.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)) return value;
            if (value.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)) return value + "/chat/completions";
            if (IsDeepSeekEndpoint(value))
            {
                return value + "/chat/completions";
            }
            return value + "/v1/chat/completions";
        }

        private static bool IsDeepSeekEndpoint(string baseUrl)
        {
            return Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri uri) &&
                   (string.Equals(uri.Host, "deepseek.com", StringComparison.OrdinalIgnoreCase) ||
                    uri.Host.EndsWith(".deepseek.com", StringComparison.OrdinalIgnoreCase));
        }

        [Serializable]
        private class ChatCompletionRequest
        {
            public string model;
            public float temperature;
            public int max_tokens;
            public bool stream;
            public ChatMessage[] messages;
            public ResponseFormat response_format;
        }

        [Serializable]
        private sealed class DeepSeekChatCompletionRequest : ChatCompletionRequest
        {
            public ThinkingMode thinking;
        }

        [Serializable]
        private sealed class ThinkingMode
        {
            public string type;
        }

        [Serializable]
        private sealed class ChatMessage
        {
            public string role;
            public string content;
        }

        [Serializable]
        private sealed class ResponseFormat
        {
            public string type;
        }

        [Serializable]
        private sealed class ChatCompletionResponse
        {
            public ChatChoice[] choices;
        }

        [Serializable]
        private sealed class ChatChoice
        {
            public ChatResponseMessage message;
            public string finish_reason;
        }

        [Serializable]
        private sealed class ChatResponseMessage
        {
            public string content;
        }
    }
}
