using System;
using UnityEngine;

namespace AnchorDefense
{
    public enum AntiAliasingSetting
    {
        Off,
        Fxaa,
        Smaa
    }

    [Serializable]
    public sealed class GameSettingsData
    {
        public int resolutionWidth;
        public int resolutionHeight;
        public int fullScreenMode;
        public int qualityLevel;
        public AntiAliasingSetting antiAliasing = AntiAliasingSetting.Smaa;
        public bool verticalSync = true;
        public int frameRateLimit = 120;
        public float brightness;
        public float masterVolume = 1f;
        public float musicVolume = 0.8f;
        public float soundEffectsVolume = 1f;
        public float ringDragSensitivity = 1f;
        public float cameraOrbitSensitivity = 1f;
        public bool showZoneBordersOutsideEdit;

        public GameSettingsData Clone()
        {
            return JsonUtility.FromJson<GameSettingsData>(JsonUtility.ToJson(this));
        }
    }
}
