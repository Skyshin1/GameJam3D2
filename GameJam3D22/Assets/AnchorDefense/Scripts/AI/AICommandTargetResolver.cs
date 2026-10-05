using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AnchorDefense
{
    public static class AICommandTargetResolver
    {
        public static bool TryResolveOperationTarget(string source, CubeZoneGridController grid,
            Camera camera, out int zoneId, out bool explicitTarget, out string error)
        {
            zoneId = -1;
            explicitTarget = false;
            error = null;
            var ids = new HashSet<int>();
            foreach (Match match in Regex.Matches(source ?? string.Empty, @"(?i)(?<![a-z0-9])C\d+"))
            {
                explicitTarget = true;
                if (!AICommandValidator.TryParseZoneId(match.Value, out int parsed) ||
                    grid == null || grid.GetCubeById(parsed) == null)
                { error = $"{match.Value} 不存在，未扣指令点"; return false; }
                ids.Add(parsed);
            }
            if (ids.Count > 1)
            { error = "每条区域操作必须对应唯一原文目标，未扣指令点"; return false; }
            foreach (int id in ids) { zoneId = id; return true; }
            source = source ?? string.Empty;
            explicitTarget = Regex.IsMatch(source,
                "这里|这块|选中|当前区域|左上|左下|右上|右下|左侧|左边|左区|右侧|右边|右区|上方|上侧|上区|下方|下侧|下区|前方|后方|\\b(?:here|this zone|selected zone|current zone|left|right|upper|top|lower|bottom|front|back)\\b", RegexOptions.IgnoreCase);
            if (!explicitTarget) return true;
            if ((HasLeft(source) && HasRight(source)) || (HasUpper(source) && HasLower(source)) ||
                (HasFront(source) && HasBack(source)))
            { error = "区域方位不明确，未扣指令点"; return false; }
            if (TryResolveExplicitTarget(source, grid, camera, out zoneId)) return true;
            error = "指定区域当前不存在或不可定位，未扣指令点";
            return false;
        }

        // The player's explicit spatial wording wins over a guessed model ID.
        public static bool TryResolveExplicitTarget(string playerText, CubeZoneGridController grid,
            Camera camera, out int zoneId)
        {
            zoneId = -1;
            if (grid == null || string.IsNullOrWhiteSpace(playerText)) return false;

            for (int i = 0; i < CubeZoneConfig.ZoneCount; i++)
            {
                string token = $"C{i + 1:00}";
                int position = playerText.IndexOf(token, StringComparison.OrdinalIgnoreCase);
                if (position < 0 || position + token.Length < playerText.Length &&
                    char.IsDigit(playerText[position + token.Length])) continue;
                if (grid.GetCubeById(i) == null) return false;
                zoneId = i;
                return true;
            }

            if (playerText.Contains("这里") || playerText.Contains("这块") ||
                playerText.Contains("选中") || playerText.Contains("当前区域") ||
                Regex.IsMatch(playerText, @"\b(?:here|this zone|selected zone|current zone)\b", RegexOptions.IgnoreCase))
            {
                if (grid.SelectedCube == null) return false;
                zoneId = grid.SelectedCube.CubeId;
                return true;
            }

            bool left = HasLeft(playerText);
            bool right = HasRight(playerText);
            bool upper = HasUpper(playerText);
            bool lower = HasLower(playerText);
            bool front = HasFront(playerText);
            bool back = HasBack(playerText);
            if (camera == null || left == right && upper == lower && front == back) return false;

            Vector2 desired = new Vector2(left ? 0.18f : right ? 0.82f : 0.5f,
                lower ? 0.18f : upper ? 0.82f : 0.5f);
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < CubeZoneConfig.ZoneCount; i++)
            {
                CubeZoneVolume cube = grid.GetCubeById(i);
                if (cube == null) continue;
                Vector3 viewport = camera.WorldToViewportPoint(cube.transform.position);
                if (viewport.z <= 0f) continue;
                float distance = (new Vector2(viewport.x, viewport.y) - desired).sqrMagnitude;
                if (front || back)
                {
                    float coreDepth = camera.WorldToViewportPoint(grid.transform.position).z;
                    float depthDifference = viewport.z - coreDepth;
                    distance += front ? Mathf.Max(0f, depthDifference) * 0.2f :
                        Mathf.Max(0f, -depthDifference) * 0.2f;
                }
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                zoneId = i;
            }
            return zoneId >= 0;
        }

        private static bool HasLeft(string text) => Regex.IsMatch(text, "左上|左下|左侧|左边|左区|\\bleft\\b", RegexOptions.IgnoreCase);
        private static bool HasRight(string text) => Regex.IsMatch(text, "右上|右下|右侧|右边|右区|\\bright\\b", RegexOptions.IgnoreCase);
        private static bool HasUpper(string text) => Regex.IsMatch(text, "左上|右上|上方|上侧|上区|\\b(?:top|upper|above)\\b", RegexOptions.IgnoreCase);
        private static bool HasLower(string text) => Regex.IsMatch(text, "左下|右下|下方|下侧|下区|\\b(?:bottom|lower|below)\\b", RegexOptions.IgnoreCase);
        private static bool HasFront(string text) => Regex.IsMatch(text, "前方|靠前|\\bfront\\b", RegexOptions.IgnoreCase);
        private static bool HasBack(string text) => Regex.IsMatch(text, "后方|靠后|\\b(?:back|rear|far)\\b", RegexOptions.IgnoreCase);
    }
}
