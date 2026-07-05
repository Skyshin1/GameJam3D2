using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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
        [SerializeField] private Toggle showZoneBordersToggle;
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
        private int selectedCategoryIndex;
        private bool dropdownExpandedLastFrame;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
        public event Action Closed;

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
            Toggle showZoneBorders,
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
            showZoneBordersToggle = showZoneBorders;
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
            ConfigureCategoryNavigation();
            EnsureGamepadBindingRows();
            for (int i = 0; i < rebindRows.Length; i++)
            {
                rebindRows[i]?.Initialize(inputActions);
            }
            ControllerSelectionHighlight.EnsureInHierarchy(panelRoot.transform);
            panelRoot.SetActive(false);
        }

        private void EnsureGamepadBindingRows()
        {
            if (rebindRows == null || rebindRows.Length == 0) return;
            for (int i = 0; i < rebindRows.Length; i++)
            {
                if (rebindRows[i] != null && rebindRows[i].name.Contains("Gamepad Rebind")) return;
            }

            var rows = new List<InputRebindRow>(rebindRows);
            string[] gamepadActions =
            {
                "CycleRing", "CameraOrbitPress", "CycleCamera", "ToggleUpgrade", "Pause"
            };

            int sourceCount = Mathf.Min(5, rebindRows.Length);
            for (int i = 0; i < sourceCount; i++)
            {
                InputRebindRow keyboardRow = rebindRows[i];
                if (keyboardRow == null || keyboardRow.RebindButton == null) continue;
                PrepareKeyboardColumn(keyboardRow);
                rows.Add(CloneBindingButton(keyboardRow, gamepadActions[i], "Gamepad", -150f));
            }

            InputRebindRow pauseRow = FindRow("Pause");
            if (pauseRow != null && pauseRow.RebindButton != null)
            {
                RectTransform pauseRect = pauseRow.RebindButton.transform as RectTransform;
                float newY = pauseRect != null ? pauseRect.anchoredPosition.y - 58f : -550f;
                CloneNearestLabel(pauseRow.RebindButton.transform.parent, pauseRect, newY, "锚域编织");

                GameObject keyboardClone = Instantiate(pauseRow.RebindButton.gameObject,
                    pauseRow.RebindButton.transform.parent);
                keyboardClone.name = "ToggleZoneEdit Keyboard Rebind";
                RectTransform keyboardRect = keyboardClone.transform as RectTransform;
                keyboardRect.sizeDelta = new Vector2(230f, 42f);
                keyboardRect.anchoredPosition = new Vector2(-430f, newY);
                InputRebindRow keyboardZone = keyboardClone.GetComponent<InputRebindRow>();
                Button keyboardButton = keyboardClone.GetComponent<Button>();
                Text keyboardText = keyboardClone.GetComponentInChildren<Text>();
                keyboardZone.Configure("Gameplay", "ToggleZoneEdit", "Keyboard&Mouse", keyboardButton, keyboardText);
                rows.Add(keyboardZone);

                rows.Add(CloneBindingButton(keyboardZone, "ToggleZoneEdit", "Gamepad", -150f));
            }

            rebindRows = rows.ToArray();
        }

        private InputRebindRow FindRow(string action)
        {
            for (int i = 0; i < rebindRows.Length; i++)
            {
                if (rebindRows[i] != null && rebindRows[i].ActionName == action) return rebindRows[i];
            }
            return null;
        }

        private static void PrepareKeyboardColumn(InputRebindRow row)
        {
            RectTransform rect = row.RebindButton.transform as RectTransform;
            if (rect == null) return;
            rect.sizeDelta = new Vector2(230f, 42f);
            rect.anchoredPosition = new Vector2(-430f, rect.anchoredPosition.y);
            row.Configure("Gameplay", row.ActionName, "Keyboard&Mouse", row.RebindButton, row.BindingText);
        }

        private static InputRebindRow CloneBindingButton(InputRebindRow source, string action,
            string group, float x)
        {
            GameObject clone = Instantiate(source.RebindButton.gameObject, source.RebindButton.transform.parent);
            clone.name = action + " Gamepad Rebind";
            RectTransform sourceRect = source.RebindButton.transform as RectTransform;
            RectTransform rect = clone.transform as RectTransform;
            rect.sizeDelta = new Vector2(230f, 42f);
            rect.anchoredPosition = new Vector2(x, sourceRect != null ? sourceRect.anchoredPosition.y : 0f);
            Button button = clone.GetComponent<Button>();
            Text text = clone.GetComponentInChildren<Text>();
            InputRebindRow row = clone.GetComponent<InputRebindRow>();
            row.Configure("Gameplay", action, group, button, text);
            return row;
        }

        private static void CloneNearestLabel(Transform parent, RectTransform sourceButton,
            float targetY, string labelText)
        {
            if (parent == null || sourceButton == null) return;
            Text nearest = null;
            float nearestDistance = float.PositiveInfinity;
            Text[] texts = parent.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].transform.parent != parent) continue;
                RectTransform rect = texts[i].rectTransform;
                float distance = Mathf.Abs(rect.anchoredPosition.y - sourceButton.anchoredPosition.y);
                if (distance < nearestDistance)
                {
                    nearest = texts[i];
                    nearestDistance = distance;
                }
            }
            if (nearest == null) return;
            Text clone = Instantiate(nearest, parent);
            clone.name = labelText + " Label";
            clone.text = labelText;
            clone.rectTransform.anchoredPosition = new Vector2(nearest.rectTransform.anchoredPosition.x, targetY);
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
            SelectCurrentCategory();
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }
            panelRoot.SetActive(false);
            Closed?.Invoke();
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad != null)
            {
                if (gamepad.buttonEast.wasPressedThisFrame && !IsAnyBindingInProgress())
                {
                    if (!dropdownExpandedLastFrame)
                    {
                        Close();
                    }
                    dropdownExpandedLastFrame = false;
                    return;
                }
                if (gamepad.leftShoulder.wasPressedThisFrame)
                {
                    CycleCategory(-1);
                    return;
                }
                if (gamepad.rightShoulder.wasPressedThisFrame)
                {
                    CycleCategory(1);
                    return;
                }
            }

            EventSystem eventSystem = EventSystem.current;
            GameObject selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            Dropdown selectedDropdown = selected != null ? selected.GetComponent<Dropdown>() : null;
            dropdownExpandedLastFrame = selectedDropdown != null && GameObject.Find("Dropdown List") != null;
            if (selected == null || !selected.activeInHierarchy)
            {
                SelectCurrentCategory();
            }
        }

        private bool IsAnyBindingInProgress()
        {
            if (rebindRows == null) return false;
            for (int i = 0; i < rebindRows.Length; i++)
            {
                if (rebindRows[i] != null && rebindRows[i].IsRebinding) return true;
            }
            return false;
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
            masterVolumeSlider.onValueChanged.AddListener(_ => HandleAudioVolumeChanged());
            musicVolumeSlider.onValueChanged.AddListener(_ => HandleAudioVolumeChanged());
            soundEffectsVolumeSlider.onValueChanged.AddListener(_ => HandleAudioVolumeChanged());
            ringSensitivitySlider.onValueChanged.AddListener(_ => ringSensitivityValue.text = ringSensitivitySlider.value.ToString("0.00") + "×");
            cameraSensitivitySlider.onValueChanged.AddListener(_ => cameraSensitivityValue.text = cameraSensitivitySlider.value.ToString("0.00") + "×");
        }

        private void HandleAudioVolumeChanged()
        {
            masterVolumeValue.text = Mathf.RoundToInt(masterVolumeSlider.value * 100f) + "%";
            musicVolumeValue.text = Mathf.RoundToInt(musicVolumeSlider.value * 100f) + "%";
            soundEffectsVolumeValue.text = Mathf.RoundToInt(soundEffectsVolumeSlider.value * 100f) + "%";

            GameSettingsData settings = GameSettingsService.Current.Clone();
            settings.masterVolume = masterVolumeSlider.value;
            settings.musicVolume = musicVolumeSlider.value;
            settings.soundEffectsVolume = soundEffectsVolumeSlider.value;
            GameSettingsService.ApplyAndSave(settings);
        }

        private void Populate(GameSettingsData settings)
        {
            resolutionDropdown.value = FindResolution(settings.resolutionWidth, settings.resolutionHeight);
            screenModeDropdown.value = GetScreenModeIndex((FullScreenMode)settings.fullScreenMode);
            SetSteppedValue(brightnessSlider, settings.brightness);
            verticalSyncToggle.SetIsOnWithoutNotify(settings.verticalSync);
            showZoneBordersToggle.SetIsOnWithoutNotify(settings.showZoneBordersOutsideEdit);
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
            settings.showZoneBordersOutsideEdit = showZoneBordersToggle.isOn;
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
            selectedCategoryIndex = categoryPanels != null && categoryPanels.Length > 0
                ? Mathf.Clamp(selectedIndex, 0, categoryPanels.Length - 1)
                : 0;
            for (int i = 0; i < categoryPanels.Length; i++)
            {
                categoryPanels[i].SetActive(i == selectedCategoryIndex);
            }
        }

        private void CycleCategory(int direction)
        {
            if (categoryButtons == null || categoryButtons.Length == 0)
            {
                return;
            }

            selectedCategoryIndex =
                (selectedCategoryIndex + direction + categoryButtons.Length) % categoryButtons.Length;

            ShowCategory(selectedCategoryIndex);

            GameObject target = categoryButtons[selectedCategoryIndex] != null
                ? categoryButtons[selectedCategoryIndex].gameObject
                : null;

            if (target != null && target.activeInHierarchy)
            {
                EventSystem.current?.SetSelectedGameObject(target);
            }
        }

        private void SelectCurrentCategory()
        {
            GameObject target = categoryButtons != null && categoryButtons.Length > 0
                ? categoryButtons[Mathf.Clamp(selectedCategoryIndex, 0, categoryButtons.Length - 1)].gameObject
                : closeButton != null ? closeButton.gameObject : null;
            if (target != null && target.activeInHierarchy)
            {
                EventSystem.current?.SetSelectedGameObject(target);
            }
        }

        private void ConfigureCategoryNavigation()
        {
            if (categoryButtons == null || categoryButtons.Length == 0)
            {
                return;
            }

            for (int i = 0; i < categoryButtons.Length; i++)
            {
                Button current = categoryButtons[i];
                if (current == null)
                {
                    continue;
                }

                Button upButton = categoryButtons[(i - 1 + categoryButtons.Length) % categoryButtons.Length];
                Button downButton = categoryButtons[(i + 1) % categoryButtons.Length];

                Selectable firstPanelSelectable = categoryPanels != null && i < categoryPanels.Length
                    ? FindFirstSelectable(categoryPanels[i])
                    : null;

                Navigation navigation = current.navigation;
                navigation.mode = Navigation.Mode.Explicit;

                // 左侧分类是竖排，所以手柄上下应该切换分类
                navigation.selectOnUp = upButton;
                navigation.selectOnDown = downButton;

                // 向右进入当前分类内容区
                navigation.selectOnRight = firstPanelSelectable;

                // 向左留在自己，避免乱跳
                navigation.selectOnLeft = current;

                current.navigation = navigation;
            }

            if (closeButton != null)
            {
                Navigation closeNavigation = closeButton.navigation;
                closeNavigation.mode = Navigation.Mode.Explicit;
                closeNavigation.selectOnDown = categoryButtons[0];
                closeNavigation.selectOnLeft = categoryButtons[0];
                closeNavigation.selectOnRight = categoryButtons[0];
                closeButton.navigation = closeNavigation;
            }
        }

        private static Selectable FindFirstSelectable(GameObject root)
        {
            if (root == null)
            {
                return null;
            }

            Selectable[] selectables = root.GetComponentsInChildren<Selectable>(true);
            for (int i = 0; i < selectables.Length; i++)
            {
                if (selectables[i] != null && selectables[i].interactable)
                {
                    return selectables[i];
                }
            }
            return null;
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
