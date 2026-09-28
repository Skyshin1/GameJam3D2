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
                "这里|这块|选中|当前区域|左上|左下|右上|右下|左侧|左边|左区|右侧|右边|右区|上方|上侧|上区|下方|下侧|下区");
            if (!explicitTarget) return true;
            if ((Regex.IsMatch(source, "左上|左下|左侧|左边|左区") &&
                 Regex.IsMatch(source, "右上|右下|右侧|右边|右区")) ||
                (Regex.IsMatch(source, "左上|右上|上方|上侧|上区") &&
                 Regex.IsMatch(source, "左下|右下|下方|下侧|下区")))
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
                playerText.Contains("选中") || playerText.Contains("当前区域"))
            {
                if (grid.SelectedCube == null) return false;
                zoneId = grid.SelectedCube.CubeId;
                return true;
            }

            bool left = playerText.Contains("左上") || playerText.Contains("左下") ||
                playerText.Contains("左侧") || playerText.Contains("左边") || playerText.Contains("左区");
            bool right = playerText.Contains("右上") || playerText.Contains("右下") ||
                playerText.Contains("右侧") || playerText.Contains("右边") || playerText.Contains("右区");
            bool upper = playerText.Contains("左上") || playerText.Contains("右上") ||
                playerText.Contains("上方") || playerText.Contains("上侧") || playerText.Contains("上区");
            bool lower = playerText.Contains("左下") || playerText.Contains("右下") ||
                playerText.Contains("下方") || playerText.Contains("下侧") || playerText.Contains("下区");
            if (camera == null || left == right && upper == lower) return false;

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
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                zoneId = i;
            }
            return zoneId >= 0;
        }
    }
}
