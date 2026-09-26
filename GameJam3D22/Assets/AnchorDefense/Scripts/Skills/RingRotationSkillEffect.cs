using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AnchorDefense
{
    [CreateAssetMenu(menuName = "Anchor Defense/AI/Effects/Ring Rotation", fileName = "RingRotationEffect")]
    public sealed class RingRotationSkillEffect : ActiveSkillEffect
    {
        private enum RingMode { Adaptive, Spin, Once, Stop }

        [SerializeField, Range(5f, 180f)] private float defaultDegrees = 45f;
        [SerializeField, Min(0.05f)] private float rotationDuration = 0.8f;
        [SerializeField, Range(5f, 120f)] private float sustainedSpeedDegreesPerSecond = 35f;
        [SerializeField, Min(0f), Tooltip("0 表示持续到停止、手动接管或游戏结束")]
        private float sustainedDurationSeconds;

        public override bool IsWorldOperation => true;

        public override bool ValidateCommand(string playerText, OrbitRingController[] rings,
            out string error)
        {
            return ValidateCommand(playerText, rings, null, out error);
        }

        public override bool ValidateCommand(string playerText, OrbitRingController[] rings,
            AIParsedCommand command, out string error) =>
            TryResolve(playerText, rings, command, out _, out _, out error);

        public override void ApplyOnActivation(ActiveSkillContext context,
            string playerText, OrbitRingController[] rings)
        {
            ApplyOnActivation(context, playerText, rings, null);
        }

        public override void ApplyOnActivation(ActiveSkillContext context,
            string playerText, OrbitRingController[] rings, AIParsedCommand command)
        {
            if (!TryResolve(playerText, rings, command, out OrbitRingController ring,
                    out float degrees, out string error))
                throw new InvalidOperationException(error);
            RingMode mode = ResolveMode(playerText, command);
            if (mode == RingMode.Stop)
            {
                if (command?.RingId == null && !HasExplicitRing(playerText))
                    for (int i = 0; rings != null && i < rings.Length; i++)
                        rings[i]?.StopCommandRotation();
                else ring.StopCommandRotation();
                return;
            }

            if (mode == RingMode.Once)
            {
                ring.RotateByCommand(degrees, rotationDuration);
                return;
            }

            float duration = ResolveSustainedDuration(playerText);
            float speed = ResolveSustainedSpeed(playerText);
            if (mode == RingMode.Spin)
                ring.RotateContinuouslyByCommand(Mathf.Sign(degrees) * speed, duration);
            else ring.DefendByCommand(speed, duration);
        }

        public override string DescribeCommand(string playerText, OrbitRingController[] rings)
        {
            return DescribeCommand(playerText, rings, null);
        }

        public override string DescribeCommand(string playerText, OrbitRingController[] rings,
            AIParsedCommand command)
        {
            if (!TryResolve(playerText, rings, command, out OrbitRingController ring,
                    out float degrees, out _)) return null;
            string ringName = ring.RingId == OrbitRingId.Inner ? "第一轨道" :
                ring.RingId == OrbitRingId.Middle ? "第二轨道" : "第三轨道";
            string direction = degrees > 0f ? "逆时针" : "顺时针";
            RingMode mode = ResolveMode(playerText, command);
            if (mode == RingMode.Stop)
                return command?.RingId == null && !HasExplicitRing(playerText)
                    ? "已停止所有星环" : $"已停止{ringName}";
            if (mode == RingMode.Once)
                return $"{ringName}{direction}转动 {Mathf.Abs(degrees):0.#}°";
            float duration = ResolveSustainedDuration(playerText);
            string action = mode == RingMode.Spin
                ? $"{ringName}{direction}持续旋转" : $"{ringName}由星核持续接管";
            return duration > 0f ? $"{action} {duration:0.#} 秒" : $"{action}，直到停止";
        }

        private static RingMode ResolveMode(string text, AIParsedCommand command)
        {
            if (command?.RingMode == "stop") return RingMode.Stop;
            if (HasExplicitOnce(text)) return RingMode.Once;
            if (command?.RingMode == "spin") return RingMode.Spin;
            if (command?.RingMode == "adaptive") return RingMode.Adaptive;
            if (IsExplicitSpin(text)) return RingMode.Spin;
            return RingMode.Adaptive;
        }

        private static bool HasExplicitOnce(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            if (text.Contains("只转一次") || text.Contains("转一次") ||
                text.Contains("转一下") ||
                text.Contains("就停") || text.Contains("后停")) return true;
            return false;
        }

        private static bool IsExplicitSpin(string text)
        {
            return !string.IsNullOrEmpty(text) &&
                   (text.Contains("持续旋转") || text.Contains("一直转") ||
                    text.Contains("不停转") || text.Contains("每秒") ||
                    text.Contains("顺时针") || text.Contains("逆时针"));
        }

        private float ResolveSustainedDuration(string text)
        {
            Match match = Regex.Match(text ?? string.Empty, @"(\d{1,2})\s*秒");
            return match.Success && float.TryParse(match.Groups[1].Value, out float seconds)
                ? Mathf.Clamp(seconds, 1f, 20f) : sustainedDurationSeconds;
        }

        private float ResolveSustainedSpeed(string text)
        {
            Match match = Regex.Match(text ?? string.Empty,
                @"(\d{1,3})\s*(?:度|°)\s*(?:每秒|/s)");
            return match.Success && float.TryParse(match.Groups[1].Value, out float speed)
                ? Mathf.Clamp(speed, 5f, 120f) : sustainedSpeedDegreesPerSecond;
        }

        private bool TryResolve(string text, OrbitRingController[] rings,
            AIParsedCommand command,
            out OrbitRingController selected, out float degrees, out string error)
        {
            selected = null;
            degrees = 0f;
            error = "当前没有可操控的星环，未扣指令点";
            if (rings == null || rings.Length == 0) return false;
            text = text ?? string.Empty;

            Match numericRing = Regex.Match(text, @"(?:第)?(\d+)(?:号|条|个)?[轨环]");
            if (numericRing.Success && int.TryParse(numericRing.Groups[1].Value, out int ringNumber) &&
                (ringNumber < 1 || ringNumber > 3))
            {
                error = "指定的星环不存在，未扣指令点";
                return false;
            }

            int requested = -1;
            if (MatchesRing(text, "第一", "1", "一号", "内环", "内轨")) requested = 0;
            else if (MatchesRing(text, "第二", "2", "二号", "中环", "中轨")) requested = 1;
            else if (MatchesRing(text, "第三", "3", "三号", "外环", "外轨")) requested = 2;
            else if (Regex.IsMatch(text, @"第[四五六七八九4-9][条个]?[轨环]|[4-9]号?[轨环]"))
            {
                error = "指定的星环不存在，未扣指令点";
                return false;
            }
            else if (command?.RingId == "inner") requested = 0;
            else if (command?.RingId == "middle") requested = 1;
            else if (command?.RingId == "outer") requested = 2;

            int mostTurrets = -1;
            for (int i = 0; i < rings.Length; i++)
            {
                OrbitRingController ring = rings[i];
                if (ring == null || !ring.gameObject.activeInHierarchy) continue;
                if (requested >= 0)
                {
                    if ((int)ring.RingId == requested) selected = ring;
                }
                else if (ring.ActiveTurretCount > mostTurrets)
                {
                    selected = ring;
                    mostTurrets = ring.ActiveTurretCount;
                }
            }
            if (selected == null)
            {
                error = "指定的星环当前不可用，未扣指令点";
                return false;
            }

            float angle = defaultDegrees;
            Match angleMatch = Regex.Match(text, @"(\d{1,3})\s*(?:度|°)");
            if (angleMatch.Success &&
                float.TryParse(angleMatch.Groups[1].Value, out float requestedAngle))
            {
                if (requestedAngle < 5f || requestedAngle > 180f)
                {
                    error = "单次旋转角度需在 5°～180° 之间，未扣指令点";
                    return false;
                }
                angle = requestedAngle;
            }
            bool counterClockwise = text.Contains("逆时针") || text.Contains("反时针") ||
                text.Contains("向左转") || text.Contains("左转");
            degrees = counterClockwise ? angle : -angle;
            error = null;
            return true;
        }

        private static bool HasExplicitRing(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return MatchesRing(text, "第一", "1", "一号", "内环", "内轨") ||
                   MatchesRing(text, "第二", "2", "二号", "中环", "中轨") ||
                   MatchesRing(text, "第三", "3", "三号", "外环", "外轨");
        }

        private static bool MatchesRing(string text, string ordinal, string digit,
            string numbered, string shortName, string alternateName)
        {
            return text.Contains(ordinal + "轨") || text.Contains(ordinal + "环") ||
                   text.Contains(ordinal + "条轨") || text.Contains(ordinal + "个环") ||
                   text.Contains("第" + digit + "轨") || text.Contains("第" + digit + "环") ||
                   text.Contains("第" + digit + "条轨") ||
                   text.Contains(digit + "号轨") || text.Contains(digit + "号环") ||
                   text.Contains(numbered + "轨") || text.Contains(numbered + "环") ||
                   text.Contains(shortName) || text.Contains(alternateName);
        }
    }
}
