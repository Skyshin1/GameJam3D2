using System;
using System.IO;
using UnityEngine;

namespace AnchorDefense
{
    [Serializable]
    public sealed class AIProviderLocalSettings
    {
        public string base_url;
        public string model;
        public string api_key;
    }

    public readonly struct ResolvedAIProviderSettings
    {
        public ResolvedAIProviderSettings(string baseUrl, string model, string apiKey)
        {
            BaseUrl = baseUrl;
            Model = model;
            ApiKey = apiKey;
        }

        public string BaseUrl { get; }
        public string Model { get; }
        public string ApiKey { get; }
        public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl) &&
                                    !string.IsNullOrWhiteSpace(Model) &&
                                    !string.IsNullOrWhiteSpace(ApiKey);
    }

    public static class AIProviderSettings
    {
        public const string ApiKeyEnvironmentVariable = "ANCHOR_AI_API_KEY";
        public const string DeepSeekApiKeyEnvironmentVariable = "DEEPSEEK_API_KEY";
        private const string DirectoryName = "AnchorDefense";
        private const string FileName = "ai-provider.json";

        public static string LocalSettingsPath =>
            Path.Combine(Application.persistentDataPath, DirectoryName, FileName);

        public static ResolvedAIProviderSettings Load(AICommandConfig config)
        {
            string baseUrl = config != null ? config.DefaultBaseUrl : string.Empty;
            string model = config != null ? config.DefaultModel : string.Empty;
            string apiKey = string.Empty;

            try
            {
                if (File.Exists(LocalSettingsPath))
                {
                    AIProviderLocalSettings local = JsonUtility.FromJson<AIProviderLocalSettings>(
                        File.ReadAllText(LocalSettingsPath));
                    if (local != null)
                    {
                        if (!string.IsNullOrWhiteSpace(local.base_url)) baseUrl = local.base_url.Trim();
                        if (!string.IsNullOrWhiteSpace(local.model)) model = local.model.Trim();
                        if (!string.IsNullOrWhiteSpace(local.api_key)) apiKey = local.api_key.Trim();
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"AI 本地配置读取失败：{exception.Message}");
            }

            string environmentKey = Environment.GetEnvironmentVariable(ApiKeyEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(environmentKey))
            {
                environmentKey = Environment.GetEnvironmentVariable(DeepSeekApiKeyEnvironmentVariable);
            }
            if (!string.IsNullOrWhiteSpace(environmentKey)) apiKey = environmentKey.Trim();
            return new ResolvedAIProviderSettings(baseUrl, model, apiKey);
        }

        public static void SaveLocal(AIProviderLocalSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            string directory = Path.GetDirectoryName(LocalSettingsPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(LocalSettingsPath, JsonUtility.ToJson(settings, true));
        }
    }
}
