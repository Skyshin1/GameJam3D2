using System.Collections.Generic;
using UnityEngine;

namespace AnchorDefense
{
    // Each overlay releases only its own pause. A lost core cannot be resumed by closing UI.
    public static class GamePause
    {
        private static readonly HashSet<object> owners = new HashSet<object>();
        private static bool ended;
        public static bool IsPaused => ended || owners.Count != 0;
        public static bool RescueActive { get; set; }
        public static void Set(object owner, bool paused)
        {
            if (owner == null) return;
            if (paused) owners.Add(owner); else owners.Remove(owner);
            Time.timeScale = IsPaused ? 0f : 1f;
        }
        public static void EndGame() { ended = true; Time.timeScale = 0f; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            owners.Clear(); ended = false; RescueActive = false; Time.timeScale = 1f;
        }
    }
}
