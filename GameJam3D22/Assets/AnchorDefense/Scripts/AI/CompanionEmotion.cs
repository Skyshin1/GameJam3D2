using System;

namespace AnchorDefense
{
    public enum CompanionEmotion { Neutral, Happy, Helpless, Worried, Thinking, Curious }

    public static class CompanionEmotions
    {
        public static CompanionEmotion? Parse(string value)
        {
            switch (value)
            {
                case "neutral": return CompanionEmotion.Neutral;
                case "happy": return CompanionEmotion.Happy;
                case "helpless": return CompanionEmotion.Helpless;
                case "worried": return CompanionEmotion.Worried;
                case "thinking": return CompanionEmotion.Thinking;
                case "curious": return CompanionEmotion.Curious;
                default: return null;
            }
        }

        // Older providers may omit emotion. Keep their dialogue usable with a conservative fallback.
        public static CompanionEmotion Resolve(CompanionSpeech speech)
        {
            if (speech.Purpose == CompanionPurpose.RescueQuestion || speech.Purpose == CompanionPurpose.RescueAnswer)
                return CompanionEmotion.Worried;
            if (speech.Emotion.HasValue) return speech.Emotion.Value;
            string text = speech.Text ?? string.Empty;
            if (Contains(text, "担心", "危险", "小心", "撑不住", "害怕", "worried", "danger", "careful", "afraid")) return CompanionEmotion.Worried;
            if (Contains(text, "无奈", "唉", "好吧", "可惜", "没办法", "oh well", "sigh", "unfortunately")) return CompanionEmotion.Helpless;
            if (Contains(text, "开心", "太棒", "真棒", "谢谢", "好耶", "happy", "great", "thank", "wonderful")) return CompanionEmotion.Happy;
            if (Contains(text, "想想", "思考", "回应", "thinking", "let me think")) return CompanionEmotion.Thinking;
            if (Contains(text, "？", "?", "好奇", "curious")) return CompanionEmotion.Curious;
            return CompanionEmotion.Neutral;
        }

        private static bool Contains(string text, params string[] words)
        {
            foreach (string word in words)
                if (text.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
    }
}
