using UnityEditor;
using UnityEngine;

namespace AnchorDefense.Editor
{
    public sealed class AIProviderSettingsWindow : EditorWindow
    {
        private string baseUrl = "https://api.deepseek.com";
        private string model = "deepseek-flash";
        private string apiKey = string.Empty;
        private string status = string.Empty;

        [MenuItem("Tools/Anchor Defense/AI/Configure Local Cloud API")]
        private static void Open()
        {
            AIProviderSettingsWindow window = GetWindow<AIProviderSettingsWindow>(true,
                "Anchor AI 本机配置", true);
            window.minSize = new Vector2(520f, 250f);
            window.LoadExisting();
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("本配置只保存在当前电脑，不会写进 Unity 项目。",
                EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(8f);
            baseUrl = EditorGUILayout.TextField("Base URL", baseUrl);
            model = EditorGUILayout.TextField("模型名", model);
            apiKey = EditorGUILayout.PasswordField("API Key", apiKey);
            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox("DeepSeek 默认使用 https://api.deepseek.com 和 deepseek-flash。\n" +
                                    "DeepSeek 地址会补全为 /chat/completions；其他兼容服务默认补全 /v1/chat/completions。\n" +
                                    "环境变量 ANCHOR_AI_API_KEY 或 DEEPSEEK_API_KEY 的优先级高于本机文件。",
                MessageType.Info);
            if (GUILayout.Button("保存本机配置", GUILayout.Height(36f))) Save();
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.Info);
            EditorGUILayout.Space(5f);
            EditorGUILayout.SelectableLabel(AIProviderSettings.LocalSettingsPath,
                EditorStyles.textField, GUILayout.Height(38f));
        }

        private void LoadExisting()
        {
            try
            {
                if (!System.IO.File.Exists(AIProviderSettings.LocalSettingsPath)) return;
                AIProviderLocalSettings data = JsonUtility.FromJson<AIProviderLocalSettings>(
                    System.IO.File.ReadAllText(AIProviderSettings.LocalSettingsPath));
                if (data == null) return;
                if (!string.IsNullOrWhiteSpace(data.base_url)) baseUrl = data.base_url;
                if (!string.IsNullOrWhiteSpace(data.model)) model = data.model;
                apiKey = data.api_key ?? string.Empty;
            }
            catch (System.Exception exception)
            {
                status = exception.Message;
            }
        }

        private void Save()
        {
            try
            {
                AIProviderSettings.SaveLocal(new AIProviderLocalSettings
                {
                    base_url = baseUrl.Trim(),
                    model = model.Trim(),
                    api_key = apiKey.Trim()
                });
                status = "配置已保存。重新进入游戏场景后生效。";
            }
            catch (System.Exception exception)
            {
                status = "保存失败：" + exception.Message;
            }
        }
    }
}
