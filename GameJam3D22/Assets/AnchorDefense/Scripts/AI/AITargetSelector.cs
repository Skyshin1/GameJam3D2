using System;
using System.Text.RegularExpressions;

namespace AnchorDefense
{
    [Serializable]
    public sealed class AITargetSelector
    {
        public string metric;
        public string order;
        public bool IsValid => (metric == "enemy_count" || metric == "missing_health" ||
            metric == "turret_count" || metric == "enemy_pressure") && (order == "asc" || order == "desc");
        public string Key => metric + ":" + order;

        public static AITargetSelector FromSource(string source)
        {
            if (string.IsNullOrWhiteSpace(source)) return null;
            string metric = null;
            // Natural wording often omits “enemies” or says “where” instead of “zone”.
            if (Regex.IsMatch(source, "最密集|最密的|最集中的|最拥挤|最扎堆|扎堆最多|\\b(?:densest|most crowded|most densely packed|highest enemy density)\\b", RegexOptions.IgnoreCase))
                return new AITargetSelector { metric = "enemy_count", order = "desc" };
            if (Regex.IsMatch(source, "敌人最密集|敌人最多|最多的?敌人|敌人最少|最少的?敌人|敌人数量最[多少]|最密集的?区域|\\b(?:most|fewest|least)\\s+(?:enemies|monsters|hostiles)\\b|\\b(?:densest|least crowded)\\s+(?:enemy|monster|hostile)\\b", RegexOptions.IgnoreCase)) metric = "enemy_count";
            else if (Regex.IsMatch(source, "受损最|损伤最|损坏最|缺血最|缺失生命最|血量缺失最|\\b(?:most damaged|worst damaged|lowest health|most injured|most repairs needed)\\s*(?:turrets?|zone|area)?\\b", RegexOptions.IgnoreCase)) metric = "missing_health";
            else if (Regex.IsMatch(source, "炮[塔台]最多|最多的?炮[塔台]|炮[塔台]最少|最少的?炮[塔台]|\\b(?:most|fewest|least)\\s+(?:turrets?|towers?|cannons?)\\b", RegexOptions.IgnoreCase)) metric = "turret_count";
            else if (Regex.IsMatch(source, "最危险|威胁最|压力最|\\b(?:most dangerous|highest threat|most threatened|worst pressure)\\b", RegexOptions.IgnoreCase)) metric = "enemy_pressure";
            return metric == null ? null : new AITargetSelector
                { metric = metric, order = Regex.IsMatch(source, "最少|最低|最小|\\b(?:fewest|least|lowest)\\b", RegexOptions.IgnoreCase) ? "asc" : "desc" };
        }
    }
}
