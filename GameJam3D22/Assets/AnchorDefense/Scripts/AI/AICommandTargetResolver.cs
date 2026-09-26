using System;
using UnityEngine;

namespace AnchorDefense
{
    public static class AICommandTargetResolver
    {
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
