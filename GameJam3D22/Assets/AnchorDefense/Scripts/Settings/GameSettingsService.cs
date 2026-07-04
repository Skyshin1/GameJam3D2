using System;
using UnityEngine;

namespace AnchorDefense
{
    public static class GameSettingsService
    {
        private const string PlayerPrefsKey = "AnchorDefense.GameSettings";
        private static GameSettingsData current;

        public static GameSettingsData Current
        {
            get
            {
                EnsureLoaded();
                return current;
            }
        }

        public static event Action<GameSettingsData> Changed;

        public static void EnsureLoaded()
        {
            if (current != null)
            {
                return;
            }

            GameSettingsData defaults = CreateDefaults();
            if (PlayerPrefs.HasKey(PlayerPrefsKey))
            {
                string json = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
                current = string.IsNullOrEmpty(json)
                    ? defaults
                    : JsonUtility.FromJson<GameSettingsData>(json);
            }
            else
            {
                current = defaults;
            }

            Sanitize(current, defaults);
            ApplyGlobal(current);
        }

        public static void ApplyAndSave(GameSettingsData settings)
        {
            GameSettingsData defaults = CreateDefaults();
            current = settings != null ? settings.Clone() : defaults;
            Sanitize(current, defaults);
            ApplyGlobal(current);
            PlayerPrefs.SetString(PlayerPrefsKey, JsonUtility.ToJson(current));
            PlayerPrefs.Save();
            Changed?.Invoke(current);
        }

        public static GameSettingsData ResetToDefaults()
        {
            GameSettingsData defaults = CreateDefaults();
            ApplyAndSave(defaults);
            return current.Clone();
        }

        private static GameSettingsData CreateDefaults()
        {
            Resolution resolution = Screen.currentResolution;
            return new GameSettingsData
            {
                resolutionWidth = resolution.width > 0 ? resolution.width : 1920,
                resolutionHeight = resolution.height > 0 ? resolution.height : 1080,
                fullScreenMode = (int)FullScreenMode.FullScreenWindow,
                qualityLevel = Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, Mathf.Max(0, QualitySettings.names.Length - 1)),
                antiAliasing = AntiAliasingSetting.Smaa,
                verticalSync = true,
                frameRateLimit = 120,
                brightness = 0f,
                masterVolume = 1f,
                musicVolume = 0.8f,
                soundEffectsVolume = 1f,
                ringDragSensitivity = 1f,
                cameraOrbitSensitivity = 1f,
                showZoneBordersOutsideEdit = false
            };
        }

        private static void Sanitize(GameSettingsData settings, GameSettingsData defaults)
        {
            if (settings.resolutionWidth <= 0 || settings.resolutionHeight <= 0)
            {
                settings.resolutionWidth = defaults.resolutionWidth;
                settings.resolutionHeight = defaults.resolutionHeight;
            }
            settings.qualityLevel = Mathf.Clamp(settings.qualityLevel, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
            settings.brightness = Mathf.Clamp(settings.brightness, -2f, 2f);
            settings.masterVolume = Mathf.Clamp01(settings.masterVolume);
            settings.musicVolume = Mathf.Clamp01(settings.musicVolume);
            settings.soundEffectsVolume = Mathf.Clamp01(settings.soundEffectsVolume);
            settings.ringDragSensitivity = Mathf.Clamp(settings.ringDragSensitivity, 0.25f, 2f);
            settings.cameraOrbitSensitivity = Mathf.Clamp(settings.cameraOrbitSensitivity, 0.25f, 2f);
            if (settings.frameRateLimit != 30 && settings.frameRateLimit != 60 &&
                settings.frameRateLimit != 120 && settings.frameRateLimit != -1)
            {
                settings.frameRateLimit = 120;
            }
        }

        private static void ApplyGlobal(GameSettingsData settings)
        {
            QualitySettings.SetQualityLevel(settings.qualityLevel, true);
            QualitySettings.vSyncCount = settings.verticalSync ? 1 : 0;
            Application.targetFrameRate = settings.verticalSync ? -1 : settings.frameRateLimit;
            AudioListener.volume = settings.masterVolume;

            FullScreenMode screenMode = Enum.IsDefined(typeof(FullScreenMode), settings.fullScreenMode)
                ? (FullScreenMode)settings.fullScreenMode
                : FullScreenMode.FullScreenWindow;
            Screen.SetResolution(settings.resolutionWidth, settings.resolutionHeight, screenMode);
        }
    }
}
