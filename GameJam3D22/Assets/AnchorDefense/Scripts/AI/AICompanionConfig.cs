using UnityEngine;

namespace AnchorDefense
{
    public enum CompanionChatterTopic { Joke, WorldLore, Encouragement }

    [CreateAssetMenu(menuName = "Anchor Defense/AI/Companion Config", fileName = "AICompanionConfig")]
    public sealed class AICompanionConfig : ScriptableObject
    {
        [Header("Conversation timing (active gameplay seconds)")]
        [Min(1f)] public float recommendationInterval = 30f;
        [Min(1f)] public float recommendationLifetime = 60f;
        [Min(1f)] public float chatterMinimum = 4f;
        [Min(1f)] public float chatterMaximum = 7f;
        [Tooltip("主动闲聊按此顺序循环；只有有效回复才推进轮次，避免世界观被随机跳过。")]
        public CompanionChatterTopic[] chatterSequence =
        {
            CompanionChatterTopic.Joke, CompanionChatterTopic.WorldLore,
            CompanionChatterTopic.Joke, CompanionChatterTopic.Encouragement,
            CompanionChatterTopic.Joke, CompanionChatterTopic.WorldLore
        };
        [Min(1f)] public float bubbleSeconds = 7f;
        [Tooltip("猫咪主动闲聊的信笺底色；玩家回复、推荐和救援保留原底色。")]
        public Color proactiveChatterBubbleColor = new Color(.86f, .83f, .96f, 1f);
        [Range(0f, 1f)] public float temperature = 0.8f;
        [Header("Bubble typewriter / 气泡逐字显示")]
        [Tooltip("闲聊和推荐气泡逐字出现；关闭后立即显示全文。")]
        public bool typewriterEnabled = true;
        [Tooltip("每秒显示的字符数。Bubble Seconds 从全文显示完成后开始计时。")]
        [Min(1f)] public float typewriterCharactersPerSecond = 28f;
        [Header("Rescue (once per run)")]
        [Range(0.01f, 0.99f)] public float rescueHealthRatio = 0.3f;
        [Min(0f)] public float rescueVisualSeconds = 0.8f;
        public GameObject rescueVfxPrefab;
        [Header("Voice and bounded context")]
        [Min(10)] public int chineseCharacters = 45;
        [Min(10)] public int recommendationCharacters = 60;
        [Min(5)] public int englishWords = 30;
        [Min(5)] public int recommendationWords = 40;
        [TextArea(2, 5)] public string personality = "Warm, playful, confident planetary companion. Tease a tactical choice gently, never insult the player. Speak like a close teammate, not a lecturer.";
        [TextArea(3, 8)] public string worldLore = "The invaders come from a distant fairy-tale planet. They have destroyed many peaceful planets. Now they surround our planet; together we must stop them. Reveal one small detail at a time. Do not invent new historical facts, characters, levels or abilities.";
    }
}
