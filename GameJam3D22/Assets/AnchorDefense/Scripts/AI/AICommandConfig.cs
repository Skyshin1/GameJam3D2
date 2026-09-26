using UnityEngine;

namespace AnchorDefense
{
    [CreateAssetMenu(menuName = "Anchor Defense/AI/Command Config", fileName = "AICommandConfig")]
    public sealed class AICommandConfig : ScriptableObject
    {
        [field: Header("DeepSeek / OpenAI-Compatible API")]
        [field: SerializeField] public string DefaultBaseUrl { get; private set; } = "https://api.deepseek.com";
        [field: SerializeField] public string DefaultModel { get; private set; } = "deepseek-flash";
        [field: SerializeField, Range(0f, 1f)] public float Temperature { get; private set; } = 0.1f;
        [field: SerializeField, Min(1f)] public float TimeoutSeconds { get; private set; } = 12f;

        [field: Header("Gameplay")]
        [field: SerializeField, Min(0)] public int CommandCost { get; private set; } = 10;
        [field: SerializeField, Min(1)] public int MaximumInputLength { get; private set; } = 120;
        [field: SerializeField, Range(1, 8)] public int MaximumOperations { get; private set; } = 6;
        [field: SerializeField, Min(0.1f)] public float CommandFieldDuration { get; private set; } = 6f;
        [field: SerializeField, Range(0f, 1.5f)] public float OperationStaggerSeconds { get; private set; } = 0.25f;
        [field: SerializeField] public GameObject FieldMarkerPrefab { get; private set; }
        [field: SerializeField] public ActiveSkillDefinition[] Skills { get; private set; }

#if UNITY_EDITOR
        public void Configure(string baseUrl, string model, float temperature, float timeoutSeconds,
            int commandCost, int maximumInputLength, ActiveSkillDefinition[] skills)
        {
            DefaultBaseUrl = baseUrl;
            DefaultModel = model;
            Temperature = Mathf.Clamp01(temperature);
            TimeoutSeconds = Mathf.Max(1f, timeoutSeconds);
            CommandCost = Mathf.Max(0, commandCost);
            MaximumInputLength = Mathf.Max(1, maximumInputLength);
            Skills = skills ?? new ActiveSkillDefinition[0];
        }
#endif
    }
}
