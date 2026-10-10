using System.Text.RegularExpressions;

namespace AnchorDefense
{
    // A conservative guard for explicit conversation requests, not a replacement for
    // the model's general intent classification or the command validator.
    public static class CompanionConversationIntent
    {
        public static bool IsConversationOnly(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            const RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
            string text = input.Trim();
            bool asksForInformation = Regex.IsMatch(text,
                @"^(?:(?:please|can you|could you|would you)\s+)*(?:tell me|explain|describe|talk (?:to me )?about|what\b|who\b|why\b|how (?:do|does|can|should|would|to)\b)|^(?:请|能不能|可以|你能|帮我|给我)*(?:介绍|讲讲|讲个|讲一个|讲一下|说说|聊聊|解释|告诉我|为什么|什么是|怎么才能|如何)", options);
            if (!asksForInformation) return false;

            // Keep mixed requests in the general planner: "tell me ... then rotate ...".
            // An action mentioned INSIDE an explanation is not authorization to execute it.
            return !Regex.IsMatch(text,
                @"(?:[,;.!?\n，；。！？]|\b(?:and|then|also)\b|然后|同时|并且|并|再)\s*(?:(?:please|can you|could you|would you)\s+|请|帮我|给我)*(?:rotate|spin|turn|control|operate|manage|take over|stop|halt|pause|attack|strike|heal|repair|slow|freeze|upgrade|buy|unlock)\b|(?:[,;.!?\n，；。！？]|然后|同时|并且|并|再)\s*(?:请|帮我|给我)*(?:旋转|转动|接管|控制|停止|攻击|治疗|修复|减速|冻结|升级|购买|解锁)", options);
        }
    }
}
