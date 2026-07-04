using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AnchorDefense
{
    public sealed class SettingsMenuController : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button resetDefaultsButton;
        [SerializeField] private Button resetBindingsButton;
        [SerializeField] private Button[] categoryButtons;
        [SerializeField] private GameObject[] categoryPanels;

        [Header("Display")]
        [SerializeField] private Dropdown resolutionDropdown;
        [SerializeField] private Dropdown screenModeDropdown;
        [SerializeField] private Slider brightnessSlider;
        [SerializeField] private Text brightnessValue;
        [SerializeField] private Toggle verticalSyncToggle;
        [SerializeField] private Dropdown frameRateDropdown;

        [Header("Graphics")]
        [SerializeField] private Dropdown qualityDropdown;
        [SerializeField] private Dropdown antiAliasingDropdown;

        [Header("Audio")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Text masterVolumeValue;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Text musicVolumeValue;
        [SerializeField] private Slider soundEffectsVolumeSlider;
        [SerializeField] private Text soundEffectsVolumeValue;

        [Header("Controls")]
        [SerializeField] private Slider ringSensitivitySlider;
        [SerializeField] private Text ringSensitivityValue;
        [SerializeField] private Slider cameraSensitivitySlider;
        [SerializeField] private Text cameraSensitivityValue;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private InputRebindRow[] rebindRows;

        private readonly List<Resolution> resolutions = new List<Resolution>();
        private readonly int[] frameRates = { 30, 60, 120, -1 };

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        public void ConfigureView(
            GameObject root,
            Button close,
            Button apply,
            Button resetDefaults,
            Button resetBindings,
            Button[] tabs,
            GameObject[] panels,
            Dropdown resolutionsView,
            Dropdown screenModes,
            Slider brightness,
            Text brightnessLabel,
            Toggle verticalSync,
            Dropdown frameRatesView,
            Dropdown quality,
            Dropdown antiAliasing,
            Slider masterVolume,
            Text masterLabel,
            Slider musicVolume,
            Text musicLabel,
            Slider soundEffectsVolume,
            Text soundEffectsLabel,
            Slider ringSensitivity,
            Text ringLabel,
            Slider cameraSensitivity,
            Text cameraLabel,
            InputActionAsset actions,
            InputRebindRow[] bindingRows)
        {
            panelRoot = root;
            closeButton = close;
            applyButton = apply;
            resetDefaultsButton = resetDefaults;
            resetBindingsButton = resetBindings;
            categoryButtons = tabs;
            categoryPanels = panels;
            resolutionDropdown = resolutionsView;
            screenModeDropdown = screenModes;
            brightnessSlider = brightness;
            brightnessValue = brightnessLabel;
            verticalSyncToggle = verticalSync;
            frameRateDropdown = frameRatesView;
            qualityDropdown = quality;
            antiAliasingDropdown = antiAliasing;
            masterVolumeSlider = masterVolume;
            masterVolumeValue = masterLabel;
            musicVolumeSlider = musicVolume;
            musicVolumeValue = musicLabel;
            soundEffectsVolumeSlider = soundEffectsVolume;
            soundEffectsVolumeValue = soundEffectsLabel;
            ringSensitivitySlider = ringSensitivity;
            ringSensitivityValue = ringLabel;
            cameraSensitivitySlider = cameraSensitivity;
            cameraSensitivityValue = cameraLabel;
            inputActions = actions;
            rebindRows = bindingRows;
        }

        private void Awake()
        {
            GameSettingsService.EnsureLoaded();
            InputBindingPersistence.Load(inputActions);
            BuildOptions();
            WireEvents();
            for (int i = 0; i < rebindRows.Length; i++)
            {
                rebindRows[i]?.Initialize(inputActions);
            }
            panelRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            closeButton.onClick.RemoveListener(Close);
            applyButton.onClick.RemoveListener(Apply);
            resetDefaultsButton.onClick.RemoveListener(ResetDefaults);
            resetBindingsButton.onClick.RemoveListener(ResetBindings);
            for (int i = 0; i < categoryButtons.Length; i++)
            {
                categoryButtons[i].onClick.RemoveAllListeners();
            }
        }

        public void Open()
        {
            Populate(GameSettingsService.Current);
            ShowCategory(0);
            panelRoot.SetActive(true);
        }

        public void Close()
        {
            panelRoot.SetActive(false);
        }

        private void BuildOptions()
        {
            resolutionDropdown.ClearOptions();
            resolutions.Clear();
            var resolutionNames = new List<string>();
            Resolution[] available = Screen.resolutions;
            for (int i = 0; i < available.Length; i++)
            {
                Resolution candidate = available[i];
                bool duplicate = resolutions.Exists(item => item.width == candidate.width && item.height == candidate.height);
                if (duplicate)
                {
                    continue;
                }
                resolutions.Add(candidate);
                resolutionNames.Add($"{candidate.width} × {candidate.height}");
            }
            if (resolutions.Count == 0)
            {
                resolutions.Add(Screen.currentResolution);
                resolutionNames.Add($"{Screen.currentResolution.width} × {Screen.currentResolution.height}");
            }
            resolutionDropdown.AddOptions(resolutionNames);

            screenModeDropdown.ClearOptions();
            screenModeDropdown.AddOptions(new List<string> { "无边框全屏", "独占全屏", "窗口模式" });
            frameRateDropdown.ClearOptions();
            frameRateDropdown.AddOptions(new List<string> { "30 FPS", "60 FPS", "120 FPS", "不限制" });
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
            antiAliasingDropdown.ClearOptions();
            antiAliasingDropdown.AddOptions(new List<string> { "关闭", "FXAA", "SMAA（高质量）" });
        }

        private void WireEvents()
        {
            closeButton.onClick.AddListener(Close);
            applyButton.onClick.AddListener(Apply);
            resetDefaultsButton.onClick.AddListener(ResetDefaults);
            resetBindingsButton.onClick.AddListener(ResetBindings);
            for (int i = 0; i < categoryButtons.Length; i++)
            {
                int index = i;
                categoryButtons[i].onClick.AddListener(() => ShowCategory(index));
            }

            brightnessSlider.onValueChanged.AddListener(_ => brightnessValue.text = brightnessSlider.value.ToString("+0.0;-0.0;0.0"));
            masterVolumeSlider.onValueChanged.AddListener(_ => masterVolumeValue.text = Mathf.RoundToInt(masterVolumeSlider.value * 100f) + "%");
            musicVolumeSlider.onValueChanged.AddListener(_ => musicVolumeValue.text = Mathf.RoundToInt(musicVolumeSlider.value * 100f) + "%");
            soundEffectsVolumeSlider.onValueChanged.AddListener(_ => soundEffectsVolumeValue.text = Mathf.RoundToInt(soundEffectsVolumeSlider.value * 100f) + "%");
            ringSensitivitySlider.onValueChanged.AddListener(_ => ringSensitivityValue.text = ringSensitivitySlider.value.ToString("0.00") + "×");
            cameraSensitivitySlider.onValueChanged.AddListener(_ => cameraSensitivityValue.text = cameraSensitivitySlider.value.ToString("0.00") + "×");
        }

        private void Populate(GameSettingsData settings)
        {
            resolutionDropdown.value = FindResolution(settings.resolutionWidth, settings.resolutionHeight);
            screenModeDropdown.value = GetScreenModeIndex((FullScreenMode)settings.fullScreenMode);
            SetSteppedValue(brightnessSlider, settings.brightness);
            verticalSyncToggle.SetIsOnWithoutNotify(settings.verticalSync);
            frameRateDropdown.value = GetFrameRateIndex(settings.frameRateLimit);
            qualityDropdown.value = settings.qualityLevel;
            antiAliasingDropdown.value = (int)settings.antiAliasing;
            SetSteppedValue(masterVolumeSlider, settings.masterVolume);
            SetSteppedValue(musicVolumeSlider, settings.musicVolume);
            SetSteppedValue(soundEffectsVolumeSlider, settings.soundEffectsVolume);
            SetSteppedValue(ringSensitivitySlider, settings.ringDragSensitivity);
            SetSteppedValue(cameraSensitivitySlider, settings.cameraOrbitSensitivity);
            brightnessValue.text = brightnessSlider.value.ToString("+0.0;-0.0;0.0");
            masterVolumeValue.text = Mathf.RoundToInt(masterVolumeSlider.value * 100f) + "%";
            musicVolumeValue.text = Mathf.RoundToInt(musicVolumeSlider.value * 100f) + "%";
            soundEffectsVolumeValue.text = Mathf.RoundToInt(soundEffectsVolumeSlider.value * 100f) + "%";
            ringSensitivityValue.text = ringSensitivitySlider.value.ToString("0.00") + "×";
            cameraSensitivityValue.text = cameraSensitivitySlider.value.ToString("0.00") + "×";
        }

        private static void SetSteppedValue(Slider slider, float value)
        {
            SliderStepQuantizer quantizer = slider.GetComponent<SliderStepQuantizer>();
            if (quantizer != null)
            {
                quantizer.SetValueWithoutNotify(value);
                return;
            }
            slider.SetValueWithoutNotify(value);
        }

        private void Apply()
        {
            GameSettingsData settings = GameSettingsService.Current.Clone();
            Resolution selectedResolution = resolutions[Mathf.Clamp(resolutionDropdown.value, 0, resolutions.Count - 1)];
            settings.resolutionWidth = selectedResolution.width;
            settings.resolutionHeight = selectedResolution.height;
            settings.fullScreenMode = (int)GetScreenMode(screenModeDropdown.value);
            settings.brightness = brightnessSlider.value;
            settings.verticalSync = verticalSyncToggle.isOn;
            settings.frameRateLimit = frameRates[Mathf.Clamp(frameRateDropdown.value, 0, frameRates.Length - 1)];
            settings.qualityLevel = qualityDropdown.value;
            settings.antiAliasing = (AntiAliasingSetting)antiAliasingDropdown.value;
            settings.masterVolume = masterVolumeSlider.value;
            settings.musicVolume = musicVolumeSlider.value;
            settings.soundEffectsVolume = soundEffectsVolumeSlider.value;
            settings.ringDragSensitivity = ringSensitivitySlider.value;
            settings.cameraOrbitSensitivity = cameraSensitivitySlider.value;
            GameSettingsService.ApplyAndSave(settings);
        }

        private void ResetDefaults()
        {
            Populate(GameSettingsService.ResetToDefaults());
        }

        private void ResetBindings()
        {
            InputBindingPersistence.Reset(inputActions);
            for (int i = 0; i < rebindRows.Length; i++)
            {
                rebindRows[i]?.Refresh();
            }
        }

        private void ShowCategory(int selectedIndex)
        {
            for (int i = 0; i < categoryPanels.Length; i++)
            {
                categoryPanels[i].SetActive(i == selectedIndex);
            }
        }

        private int FindResolution(int width, int height)
        {
            for (int i = 0; i < resolutions.Count; i++)
            {
                if (resolutions[i].width == width && resolutions[i].height == height)
                {
                    return i;
                }
            }
            return Mathf.Max(0, resolutions.Count - 1);
        }

        private int GetFrameRateIndex(int value)
        {
            for (int i = 0; i < frameRates.Length; i++)
            {
                if (frameRates[i] == value)
                {
                    return i;
                }
            }
            return 2;
        }

        private static int GetScreenModeIndex(FullScreenMode mode)
        {
            if (mode == FullScreenMode.ExclusiveFullScreen) return 1;
            if (mode == FullScreenMode.Windowed) return 2;
            return 0;
        }

        private static FullScreenMode GetScreenMode(int index)
        {
            if (index == 1) return FullScreenMode.ExclusiveFullScreen;
            if (index == 2) return FullScreenMode.Windowed;
            return FullScreenMode.FullScreenWindow;
        }
    }
}
