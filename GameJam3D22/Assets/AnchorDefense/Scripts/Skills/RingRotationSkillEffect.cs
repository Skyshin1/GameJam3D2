using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AnchorDefense
{
    [CreateAssetMenu(menuName = "Anchor Defense/AI/Effects/Ring Rotation", fileName = "RingRotationEffect")]
    public sealed class RingRotationSkillEffect : ActiveSkillEffect
    {
        private enum RingMode { Adaptive, Spin, Once, Repeat, Stop }

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

        public bool TryPrepare(string text, OrbitRingController[] rings, AIParsedCommand command,
            out OrbitRingController ring, out bool stopsAll, out string error)
        {
            stopsAll = false;
            if (!TryResolve(text, rings, command, out ring, out _, out error)) return false;
            RingMode mode = ResolveMode(text, command);
            stopsAll = mode == RingMode.Stop && !HasExplicitRing(text);
            command.ring_mode = mode.ToString().ToLowerInvariant();
            command.ring_count = TryReadRepeatCount(text, out int count) ? count : 0;
            command.ring_id = stopsAll ? null : ring.RingId == OrbitRingId.Inner ? "inner" :
                ring.RingId == OrbitRingId.Middle ? "middle" : "outer";
            return true;
        }

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
                ring.ClaimCommandOwnership(command);
                return;
            }
            if (mode == RingMode.Repeat)
            {
                TryReadRepeatCount(playerText, out int count);
                ring.RotateRepeatedlyByCommand(degrees, rotationDuration, count);
                ring.ClaimCommandOwnership(command);
                return;
            }

            float duration = ResolveSustainedDuration(playerText);
            float speed = ResolveSustainedSpeed(playerText);
            if (mode == RingMode.Spin)
                ring.RotateContinuouslyByCommand(Mathf.Sign(degrees) * speed, duration);
            else ring.DefendByCommand(speed, duration);
            ring.ClaimCommandOwnership(command);
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
            if (mode == RingMode.Repeat)
            {
                TryReadRepeatCount(playerText, out int count);
                return $"{ringName}{direction}每次转动 {Mathf.Abs(degrees):0.#}°，共 {count} 次后停止";
            }
            float duration = ResolveSustainedDuration(playerText);
            string action = mode == RingMode.Spin
                ? $"{ringName}{direction}持续旋转" : $"{ringName}由星核持续接管";
            return duration > 0f ? $"{action} {duration:0.#} 秒" : $"{action}，直到停止";
        }

        private static RingMode ResolveMode(string text, AIParsedCommand command)
        {
            if (IsExplicitStop(text)) return RingMode.Stop;
            if (TryReadRepeatCount(text, out int count)) return count == 1 ? RingMode.Once : RingMode.Repeat;
            if (HasExplicitOnce(text)) return RingMode.Once;
            if (IsExplicitSpin(text)) return RingMode.Spin;
            if (command?.RingMode == "spin") return RingMode.Spin;
            return RingMode.Adaptive;
        }

        public static bool IsExplicitStop(string text)
        {
            if (string.IsNullOrEmpty(text) || Regex.IsMatch(text, "不要停|不想停|不许停|不能停|别停")) return false;
            // A stop after a finite rotation describes its completion, not an immediate stop.
            if (TryReadRepeatCount(text, out _) || HasExplicitOnce(text)) return false;
            return Regex.IsMatch(text, @"(?:^|请|帮我|把|让|先)\s*(?:停止|停下|结束|别转|不再转)") ||
                   Regex.IsMatch(text, @"(?:轨道|星环|[内中外][环轨])\s*(?:停止|停下|不再转|别转)");
        }

        public static bool TryReadRepeatCount(string text, out int count)
        {
            count = 0;
            Match match = Regex.Match(text ?? string.Empty,
                @"(?:旋转|转动|重复|转)\s*(\d+|[一二两三四五六七八九十]+)\s*次");
            if (!match.Success) return false;
            string value = match.Groups[1].Value;
            if (int.TryParse(value, out count)) return true;
            string digits = "零一二三四五六七八九";
            value = value.Replace("两", "二");
            int ten = value.IndexOf('十');
            if (ten < 0 && value.Length == 1) count = digits.IndexOf(value[0]);
            else if (ten >= 0 && value.Length <= 3)
            {
                int tens = ten == 0 ? 1 : digits.IndexOf(value[0]);
                int units = ten == value.Length - 1 ? 0 : digits.IndexOf(value[value.Length - 1]);
                if (tens > 0 && units >= 0) count = tens * 10 + units;
            }
            return true; // An unrecognised written count is invalid, never an infinite spin.
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
            if (!AICommandValidator.HasRingControlIntent(text))
            {
                error = "原文没有操控星环的意图，未扣指令点";
                return false;
            }
            bool hasCount = TryReadRepeatCount(text, out int repeatCount);
            if ((hasCount && (repeatCount < 1 || repeatCount > 20)) ||
                (command?.RingMode == "repeat" && (!hasCount || command.ring_count != repeatCount)) ||
                (!hasCount && text.Contains("多次")))
            {
                error = "旋转次数必须来自原文且在 1～20 次之间，未扣指令点";
                return false;
            }
            if (command?.RingMode == "stop" && !IsExplicitStop(text))
            {
                error = "原文没有要求停止星环，未扣指令点";
                return false;
            }

            Match numericRing = Regex.Match(text, @"(?:第)?(\d+)(?:号|条|个)?(?:轨道|轨|星环|环)");
            int ringNumber = -1;
            if (numericRing.Success && (!int.TryParse(numericRing.Groups[1].Value, out ringNumber) ||
                ringNumber < 1 || ringNumber > 3))
            {
                error = "指定的星环不存在，未扣指令点";
                return false;
            }

            int requested = -1;
            if (numericRing.Success) requested = ringNumber - 1;
            else if (MatchesRing(text, "第一", "1", "一号", "内环", "内轨")) requested = 0;
            else if (MatchesRing(text, "第二", "2", "二号", "中环", "中轨")) requested = 1;
            else if (MatchesRing(text, "第三", "3", "三号", "外环", "外轨")) requested = 2;
            else if (Regex.IsMatch(text, @"第[四五六七八九十百4-9][条个]?(?:星环|轨道|轨|环)|[四五六七八九4-9]号(?:星环|轨道|轨|环)"))
            {
                error = "指定的星环不存在，未扣指令点";
                return false;
            }
            int explicitRings = (MatchesRing(text, "第一", "1", "一号", "内环", "内轨") ? 1 : 0) +
                (MatchesRing(text, "第二", "2", "二号", "中环", "中轨") ? 1 : 0) +
                (MatchesRing(text, "第三", "3", "三号", "外环", "外轨") ? 1 : 0);
            if (explicitRings > 1)
            {
                error = "每条星环操作必须对应唯一原文目标，未扣指令点";
                return false;
            }
            foreach (Match other in Regex.Matches(text, @"(?:第)?(\d+)(?:号|条|个)?(?:轨道|轨|星环|环)"))
            {
                if (!int.TryParse(other.Groups[1].Value, out int otherNumber) ||
                    otherNumber < 1 || otherNumber > 3 || (requested >= 0 && requested != otherNumber - 1))
                {
                    error = "每条星环操作必须对应唯一且有效的原文目标，未扣指令点";
                    return false;
                }
            }
            if (requested < 0)
            {
                if (text.Contains("最外")) requested = 2;
                else if (text.Contains("最内") || text.Contains("最里面")) requested = 0;
                else if (text.Contains("中间")) requested = 1;
            }

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
            Match angleMatch = Regex.Match(text, @"(?<![\d.])(-?\d+(?:\.\d+)?)\s*(?:度|°)");
            if (angleMatch.Success)
            {
                if (!float.TryParse(angleMatch.Groups[1].Value, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float requestedAngle) ||
                    requestedAngle < 5f || requestedAngle > 180f)
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
            return Regex.IsMatch(text, @"(?:第)?\d+(?:号|条|个)?(?:轨道|轨|星环|环)") ||
                   text.Contains("最外") || text.Contains("最内") || text.Contains("最里面") || text.Contains("中间") ||
                   MatchesRing(text, "第一", "1", "一号", "内环", "内轨") ||
                   MatchesRing(text, "第二", "2", "二号", "中环", "中轨") ||
                   MatchesRing(text, "第三", "3", "三号", "外环", "外轨");
        }

        private static bool MatchesRing(string text, string ordinal, string digit,
            string numbered, string shortName, string alternateName)
        {
            return text.Contains(ordinal + "轨") || text.Contains(ordinal + "环") ||
                   text.Contains(ordinal + "星环") || text.Contains("第" + digit + "星环") ||
                   text.Contains(ordinal + "条轨") || text.Contains(ordinal + "个环") ||
                   text.Contains("第" + digit + "轨") || text.Contains("第" + digit + "环") ||
                   text.Contains("第" + digit + "条轨") ||
                   text.Contains(digit + "号轨") || text.Contains(digit + "号环") ||
                   text.Contains(numbered + "轨") || text.Contains(numbered + "环") ||
                   text.Contains(shortName) || text.Contains(alternateName);
        }
    }
}
