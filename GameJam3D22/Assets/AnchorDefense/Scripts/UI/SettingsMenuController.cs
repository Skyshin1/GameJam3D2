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
        [SerializeField] private Dropdown languageDropdown;

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
        private CanvasGroup panelCanvasGroup;

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
            EnsureLanguageDropdown();
            WireEvents();
            ConfigureCategoryNavigation();
            EnsureGamepadBindingRows();

            if (rebindRows != null)
            {
                for (int i = 0; i < rebindRows.Length; i++)
                {
                    rebindRows[i]?.Initialize(inputActions);
                }
            }

            if (panelRoot != null)
            {
                ControllerSelectionHighlight.EnsureInHierarchy(panelRoot.transform);

                panelCanvasGroup = panelRoot.GetComponent<CanvasGroup>();
                if (panelCanvasGroup == null)
                {
                    panelCanvasGroup = panelRoot.AddComponent<CanvasGroup>();
                }
            }

            SetPanelVisible(false);
            GameLocalization.LanguageChanged += HandleLanguageChanged;
        }

        private void EnsureLanguageDropdown()
        {
            if (languageDropdown != null || screenModeDropdown == null || panelRoot == null) return;
            Transform existing = panelRoot.transform.Find("Language Dropdown");
            if (existing != null) languageDropdown = existing.GetComponent<Dropdown>();
            if (languageDropdown == null)
            {
                GameObject copy = Instantiate(screenModeDropdown.gameObject, panelRoot.transform);
                copy.name = "Language Dropdown";
                languageDropdown = copy.GetComponent<Dropdown>();
                RectTransform rect = (RectTransform)copy.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.anchoredPosition = new Vector2(-92f, -76f);
                rect.sizeDelta = new Vector2(270f, 42f);
                rect.localScale = Vector3.one;

                GameObject labelObject = new GameObject("Language Label", typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(panelRoot.transform, false);
                RectTransform labelRect = (RectTransform)labelObject.transform;
                labelRect.anchorMin = labelRect.anchorMax = new Vector2(1f, 1f);
                labelRect.pivot = new Vector2(1f, 1f);
                labelRect.anchoredPosition = new Vector2(-377f, -76f);
                labelRect.sizeDelta = new Vector2(170f, 42f);
                Text label = labelObject.GetComponent<Text>();
                label.text = GameLocalization.T("语言", "Language");
                label.font = languageDropdown.captionText != null && languageDropdown.captionText.font != null
                    ? languageDropdown.captionText.font : Resources.GetBuiltinResource<Font>("Arial.ttf");
                label.fontSize = 24;
                label.alignment = TextAnchor.MiddleRight;
                label.color = Color.white;
                label.raycastTarget = false;
            }
            Text existingLabel = panelRoot.transform.Find("Language Label")?.GetComponent<Text>();
            if (existingLabel != null) existingLabel.text = GameLocalization.T("语言", "Language");
            languageDropdown.onValueChanged.RemoveAllListeners();
            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(new List<string> { "简体中文", "English" });
            languageDropdown.SetValueWithoutNotify((int)GameLocalization.Language);
            languageDropdown.RefreshShownValue();
        }

        private void HandleLanguageChanged()
        {
            int resolution = resolutionDropdown != null ? resolutionDropdown.value : 0;
            int screenMode = screenModeDropdown != null ? screenModeDropdown.value : 0;
            int frameRate = frameRateDropdown != null ? frameRateDropdown.value : 0;
            int quality = qualityDropdown != null ? qualityDropdown.value : 0;
            int antiAliasing = antiAliasingDropdown != null ? antiAliasingDropdown.value : 0;
            BuildOptions();
            resolutionDropdown?.SetValueWithoutNotify(resolution);
            resolutionDropdown?.RefreshShownValue();
            screenModeDropdown?.SetValueWithoutNotify(screenMode);
            frameRateDropdown?.SetValueWithoutNotify(frameRate);
            qualityDropdown?.SetValueWithoutNotify(quality);
            antiAliasingDropdown?.SetValueWithoutNotify(antiAliasing);
            screenModeDropdown?.RefreshShownValue();
            frameRateDropdown?.RefreshShownValue();
            qualityDropdown?.RefreshShownValue();
            antiAliasingDropdown?.RefreshShownValue();
            if (languageDropdown != null)
            {
                languageDropdown.SetValueWithoutNotify((int)GameLocalization.Language);
                languageDropdown.RefreshShownValue();
            }
        }

        private void EnsureGamepadBindingRows()
        {
            if (rebindRows == null || rebindRows.Length == 0)
            {
                return;
            }

            for (int i = 0; i < rebindRows.Length; i++)
            {
                if (rebindRows[i] != null && rebindRows[i].name.Contains("Gamepad Rebind"))
                {
                    return;
                }
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
                if (keyboardRow == null || keyboardRow.RebindButton == null)
                {
                    continue;
                }

                PrepareKeyboardColumn(keyboardRow);
                rows.Add(CloneBindingButton(keyboardRow, gamepadActions[i], "Gamepad", -150f));
            }

            InputRebindRow pauseRow = FindRow("Pause");
            if (pauseRow != null && pauseRow.RebindButton != null)
            {
                RectTransform pauseRect = pauseRow.RebindButton.transform as RectTransform;
                float newY = pauseRect != null ? pauseRect.anchoredPosition.y - 58f : -550f;

                CloneNearestLabel(pauseRow.RebindButton.transform.parent, pauseRect, newY, "锚域编织");

                GameObject keyboardClone = Instantiate(
                    pauseRow.RebindButton.gameObject,
                    pauseRow.RebindButton.transform.parent);

                keyboardClone.name = "ToggleZoneEdit Keyboard Rebind";

                RectTransform keyboardRect = keyboardClone.transform as RectTransform;
                if (keyboardRect != null)
                {
                    keyboardRect.sizeDelta = new Vector2(230f, 42f);
                    keyboardRect.anchoredPosition = new Vector2(-430f, newY);
                }

                InputRebindRow keyboardZone = keyboardClone.GetComponent<InputRebindRow>();
                Button keyboardButton = keyboardClone.GetComponent<Button>();
                Text keyboardText = keyboardClone.GetComponentInChildren<Text>();

                if (keyboardZone != null)
                {
                    keyboardZone.Configure("Gameplay", "ToggleZoneEdit", "Keyboard&Mouse", keyboardButton, keyboardText);
                    rows.Add(keyboardZone);
                    rows.Add(CloneBindingButton(keyboardZone, "ToggleZoneEdit", "Gamepad", -150f));

                    float commandY = newY - 58f;
                    CloneNearestLabel(pauseRow.RebindButton.transform.parent, pauseRect, commandY, "锚核指令终端");

                    GameObject commandClone = Instantiate(
                        pauseRow.RebindButton.gameObject,
                        pauseRow.RebindButton.transform.parent);
                    commandClone.name = "ToggleAICommand Keyboard Rebind";

                    RectTransform commandRect = commandClone.transform as RectTransform;
                    if (commandRect != null)
                    {
                        commandRect.sizeDelta = new Vector2(230f, 42f);
                        commandRect.anchoredPosition = new Vector2(-430f, commandY);
                    }

                    InputRebindRow keyboardCommand = commandClone.GetComponent<InputRebindRow>();
                    Button commandButton = commandClone.GetComponent<Button>();
                    Text commandText = commandClone.GetComponentInChildren<Text>();
                    if (keyboardCommand != null)
                    {
                        keyboardCommand.Configure(
                            "CommandConsole", "Toggle", "Keyboard&Mouse", commandButton, commandText);
                        rows.Add(keyboardCommand);
                        rows.Add(CloneBindingButton(
                            keyboardCommand, "CommandConsole", "Toggle", "Gamepad", -150f));
                    }
                }
            }

            rebindRows = rows.ToArray();
        }

        private InputRebindRow FindRow(string action)
        {
            if (rebindRows == null)
            {
                return null;
            }

            for (int i = 0; i < rebindRows.Length; i++)
            {
                if (rebindRows[i] != null && rebindRows[i].ActionName == action)
                {
                    return rebindRows[i];
                }
            }

            return null;
        }

        private static void PrepareKeyboardColumn(InputRebindRow row)
        {
            if (row == null || row.RebindButton == null)
            {
                return;
            }

            RectTransform rect = row.RebindButton.transform as RectTransform;
            if (rect == null)
            {
                return;
            }

            rect.sizeDelta = new Vector2(230f, 42f);
            rect.anchoredPosition = new Vector2(-430f, rect.anchoredPosition.y);
            row.Configure("Gameplay", row.ActionName, "Keyboard&Mouse", row.RebindButton, row.BindingText);
        }

        private static InputRebindRow CloneBindingButton(
            InputRebindRow source,
            string action,
            string group,
            float x)
        {
            return CloneBindingButton(source, "Gameplay", action, group, x);
        }

        private static InputRebindRow CloneBindingButton(
            InputRebindRow source,
            string mapName,
            string action,
            string group,
            float x)
        {
            if (source == null || source.RebindButton == null)
            {
                return null;
            }

            GameObject clone = Instantiate(
                source.RebindButton.gameObject,
                source.RebindButton.transform.parent);

            clone.name = action + " Gamepad Rebind";

            RectTransform sourceRect = source.RebindButton.transform as RectTransform;
            RectTransform rect = clone.transform as RectTransform;

            if (rect != null)
            {
                rect.sizeDelta = new Vector2(230f, 42f);
                rect.anchoredPosition = new Vector2(
                    x,
                    sourceRect != null ? sourceRect.anchoredPosition.y : 0f);
            }

            Button button = clone.GetComponent<Button>();
            Text text = clone.GetComponentInChildren<Text>();
            InputRebindRow row = clone.GetComponent<InputRebindRow>();

            if (row != null)
            {
                row.Configure(mapName, action, group, button, text);
            }

            return row;
        }

        private static void CloneNearestLabel(
            Transform parent,
            RectTransform sourceButton,
            float targetY,
            string labelText)
        {
            if (parent == null || sourceButton == null)
            {
                return;
            }

            Text nearest = null;
            float nearestDistance = float.PositiveInfinity;
            Text[] texts = parent.GetComponentsInChildren<Text>(true);

            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].transform.parent != parent)
                {
                    continue;
                }

                RectTransform rect = texts[i].rectTransform;
                float distance = Mathf.Abs(rect.anchoredPosition.y - sourceButton.anchoredPosition.y);

                if (distance < nearestDistance)
                {
                    nearest = texts[i];
                    nearestDistance = distance;
                }
            }

            if (nearest == null)
            {
                return;
            }

            Text clone = Instantiate(nearest, parent);
            clone.name = labelText + " Label";
            clone.text = GameLocalization.Translate(labelText);
            clone.rectTransform.anchoredPosition =
                new Vector2(nearest.rectTransform.anchoredPosition.x, targetY);
        }

        private void OnDestroy()
        {
            GameLocalization.LanguageChanged -= HandleLanguageChanged;
            languageDropdown?.onValueChanged.RemoveAllListeners();
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }

            if (applyButton != null)
            {
                applyButton.onClick.RemoveListener(Apply);
            }

            if (resetDefaultsButton != null)
            {
                resetDefaultsButton.onClick.RemoveListener(ResetDefaults);
            }

            if (resetBindingsButton != null)
            {
                resetBindingsButton.onClick.RemoveListener(ResetBindings);
            }

            if (categoryButtons != null)
            {
                for (int i = 0; i < categoryButtons.Length; i++)
                {
                    if (categoryButtons[i] != null)
                    {
                        categoryButtons[i].onClick.RemoveAllListeners();
                    }
                }
            }
        }

        public void Open()
        {
            Populate(GameSettingsService.Current);
            ShowCategory(0);

            SetPanelVisible(true);

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                eventSystem.SetSelectedGameObject(null);
            }

            SelectCurrentCategory();

            if (panelRoot != null)
            {
                ControllerSelectionHighlight.RefreshAllInHierarchy(panelRoot.transform);
            }
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            ClearSelectionIfInsidePanel();
            SetPanelVisible(false);

            Closed?.Invoke();
        }

        private void SetPanelVisible(bool visible)
        {
            if (panelRoot == null)
            {
                return;
            }

            panelRoot.SetActive(visible);

            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 1f;
                panelCanvasGroup.interactable = visible;
                panelCanvasGroup.blocksRaycasts = visible;
            }
        }

        private void ClearSelectionIfInsidePanel()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null || panelRoot == null)
            {
                return;
            }

            GameObject selected = eventSystem.currentSelectedGameObject;
            if (selected == null)
            {
                return;
            }

            if (selected.transform == panelRoot.transform ||
                selected.transform.IsChildOf(panelRoot.transform))
            {
                eventSystem.SetSelectedGameObject(null);
            }
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

            dropdownExpandedLastFrame =
                selectedDropdown != null &&
                GameObject.Find("Dropdown List") != null;

            if (selected == null || !selected.activeInHierarchy)
            {
                SelectCurrentCategory();
            }
        }

        private bool IsAnyBindingInProgress()
        {
            if (rebindRows == null)
            {
                return false;
            }

            for (int i = 0; i < rebindRows.Length; i++)
            {
                if (rebindRows[i] != null && rebindRows[i].IsRebinding)
                {
                    return true;
                }
            }

            return false;
        }

        private void BuildOptions()
        {
            if (resolutionDropdown != null)
            {
                resolutionDropdown.ClearOptions();
            }

            resolutions.Clear();

            var resolutionNames = new List<string>();
            Resolution[] available = Screen.resolutions;

            for (int i = 0; i < available.Length; i++)
            {
                Resolution candidate = available[i];

                bool duplicate = resolutions.Exists(
                    item => item.width == candidate.width &&
                            item.height == candidate.height);

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

            resolutionDropdown?.AddOptions(resolutionNames);

            if (screenModeDropdown != null)
            {
                screenModeDropdown.ClearOptions();
                screenModeDropdown.AddOptions(new List<string> { GameLocalization.T("无边框全屏", "Borderless Fullscreen"), GameLocalization.T("独占全屏", "Exclusive Fullscreen"), GameLocalization.T("窗口模式", "Windowed") });
            }

            if (frameRateDropdown != null)
            {
                frameRateDropdown.ClearOptions();
                frameRateDropdown.AddOptions(new List<string> { "30 FPS", "60 FPS", "120 FPS", GameLocalization.T("不限制", "Unlimited") });
            }

            if (qualityDropdown != null)
            {
                qualityDropdown.ClearOptions();
                var qualityNames = new List<string>();
                foreach (string name in QualitySettings.names)
                    qualityNames.Add(GameLocalization.IsEnglish ? name :
                        name == "Performant" ? "性能优先" : name == "Balanced" ? "均衡" :
                        name == "High Fidelity" ? "高画质" : name);
                qualityDropdown.AddOptions(qualityNames);
            }

            if (antiAliasingDropdown != null)
            {
                antiAliasingDropdown.ClearOptions();
                antiAliasingDropdown.AddOptions(new List<string> { GameLocalization.T("关闭", "Off"), "FXAA", GameLocalization.T("SMAA（高质量）", "SMAA (High Quality)") });
            }
        }

        private void WireEvents()
        {
            languageDropdown?.onValueChanged.AddListener(value =>
                GameLocalization.SetLanguage(value == 1 ? GameLanguage.English : GameLanguage.SimplifiedChinese));
            closeButton?.onClick.AddListener(Close);
            applyButton?.onClick.AddListener(Apply);
            resetDefaultsButton?.onClick.AddListener(ResetDefaults);
            resetBindingsButton?.onClick.AddListener(ResetBindings);

            if (categoryButtons != null)
            {
                for (int i = 0; i < categoryButtons.Length; i++)
                {
                    int index = i;

                    if (categoryButtons[i] != null)
                    {
                        categoryButtons[i].onClick.AddListener(() => ShowCategory(index));
                    }
                }
            }

            if (brightnessSlider != null && brightnessValue != null)
            {
                brightnessSlider.onValueChanged.AddListener(
                    _ => brightnessValue.text = brightnessSlider.value.ToString("+0.0;-0.0;0.0"));
            }

            masterVolumeSlider?.onValueChanged.AddListener(_ => HandleAudioVolumeChanged());
            musicVolumeSlider?.onValueChanged.AddListener(_ => HandleAudioVolumeChanged());
            soundEffectsVolumeSlider?.onValueChanged.AddListener(_ => HandleAudioVolumeChanged());

            if (ringSensitivitySlider != null && ringSensitivityValue != null)
            {
                ringSensitivitySlider.onValueChanged.AddListener(
                    _ => ringSensitivityValue.text = ringSensitivitySlider.value.ToString("0.00") + "×");
            }

            if (cameraSensitivitySlider != null && cameraSensitivityValue != null)
            {
                cameraSensitivitySlider.onValueChanged.AddListener(
                    _ => cameraSensitivityValue.text = cameraSensitivitySlider.value.ToString("0.00") + "×");
            }
        }

        private void HandleAudioVolumeChanged()
        {
            if (masterVolumeValue != null && masterVolumeSlider != null)
            {
                masterVolumeValue.text = Mathf.RoundToInt(masterVolumeSlider.value * 100f) + "%";
            }

            if (musicVolumeValue != null && musicVolumeSlider != null)
            {
                musicVolumeValue.text = Mathf.RoundToInt(musicVolumeSlider.value * 100f) + "%";
            }

            if (soundEffectsVolumeValue != null && soundEffectsVolumeSlider != null)
            {
                soundEffectsVolumeValue.text = Mathf.RoundToInt(soundEffectsVolumeSlider.value * 100f) + "%";
            }

            GameSettingsData settings = GameSettingsService.Current.Clone();

            if (masterVolumeSlider != null)
            {
                settings.masterVolume = masterVolumeSlider.value;
            }

            if (musicVolumeSlider != null)
            {
                settings.musicVolume = musicVolumeSlider.value;
            }

            if (soundEffectsVolumeSlider != null)
            {
                settings.soundEffectsVolume = soundEffectsVolumeSlider.value;
            }

            GameSettingsService.ApplyAndSave(settings);
        }

        private void Populate(GameSettingsData settings)
        {
            if (settings == null)
            {
                return;
            }

            if (languageDropdown != null)
            {
                languageDropdown.SetValueWithoutNotify((int)settings.language);
                languageDropdown.RefreshShownValue();
            }

            if (resolutionDropdown != null)
            {
                resolutionDropdown.value = FindResolution(settings.resolutionWidth, settings.resolutionHeight);
            }

            if (screenModeDropdown != null)
            {
                screenModeDropdown.value = GetScreenModeIndex((FullScreenMode)settings.fullScreenMode);
            }

            SetSteppedValue(brightnessSlider, settings.brightness);

            if (verticalSyncToggle != null)
            {
                verticalSyncToggle.SetIsOnWithoutNotify(settings.verticalSync);
            }

            if (showZoneBordersToggle != null)
            {
                showZoneBordersToggle.SetIsOnWithoutNotify(settings.showZoneBordersOutsideEdit);
            }

            if (frameRateDropdown != null)
            {
                frameRateDropdown.value = GetFrameRateIndex(settings.frameRateLimit);
            }

            if (qualityDropdown != null)
            {
                qualityDropdown.value = settings.qualityLevel;
            }

            if (antiAliasingDropdown != null)
            {
                antiAliasingDropdown.value = (int)settings.antiAliasing;
            }

            SetSteppedValue(masterVolumeSlider, settings.masterVolume);
            SetSteppedValue(musicVolumeSlider, settings.musicVolume);
            SetSteppedValue(soundEffectsVolumeSlider, settings.soundEffectsVolume);
            SetSteppedValue(ringSensitivitySlider, settings.ringDragSensitivity);
            SetSteppedValue(cameraSensitivitySlider, settings.cameraOrbitSensitivity);

            if (brightnessValue != null && brightnessSlider != null)
            {
                brightnessValue.text = brightnessSlider.value.ToString("+0.0;-0.0;0.0");
            }

            if (masterVolumeValue != null && masterVolumeSlider != null)
            {
                masterVolumeValue.text = Mathf.RoundToInt(masterVolumeSlider.value * 100f) + "%";
            }

            if (musicVolumeValue != null && musicVolumeSlider != null)
            {
                musicVolumeValue.text = Mathf.RoundToInt(musicVolumeSlider.value * 100f) + "%";
            }

            if (soundEffectsVolumeValue != null && soundEffectsVolumeSlider != null)
            {
                soundEffectsVolumeValue.text = Mathf.RoundToInt(soundEffectsVolumeSlider.value * 100f) + "%";
            }

            if (ringSensitivityValue != null && ringSensitivitySlider != null)
            {
                ringSensitivityValue.text = ringSensitivitySlider.value.ToString("0.00") + "×";
            }

            if (cameraSensitivityValue != null && cameraSensitivitySlider != null)
            {
                cameraSensitivityValue.text = cameraSensitivitySlider.value.ToString("0.00") + "×";
            }
        }

        private static void SetSteppedValue(Slider slider, float value)
        {
            if (slider == null)
            {
                return;
            }

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

            if (resolutions.Count > 0 && resolutionDropdown != null)
            {
                Resolution selectedResolution =
                    resolutions[Mathf.Clamp(resolutionDropdown.value, 0, resolutions.Count - 1)];

                settings.resolutionWidth = selectedResolution.width;
                settings.resolutionHeight = selectedResolution.height;
            }

            if (screenModeDropdown != null)
            {
                settings.fullScreenMode = (int)GetScreenMode(screenModeDropdown.value);
            }

            if (brightnessSlider != null)
            {
                settings.brightness = brightnessSlider.value;
            }

            if (verticalSyncToggle != null)
            {
                settings.verticalSync = verticalSyncToggle.isOn;
            }

            if (showZoneBordersToggle != null)
            {
                settings.showZoneBordersOutsideEdit = showZoneBordersToggle.isOn;
            }

            if (frameRateDropdown != null)
            {
                settings.frameRateLimit =
                    frameRates[Mathf.Clamp(frameRateDropdown.value, 0, frameRates.Length - 1)];
            }

            if (qualityDropdown != null)
            {
                settings.qualityLevel = qualityDropdown.value;
            }

            if (antiAliasingDropdown != null)
            {
                settings.antiAliasing = (AntiAliasingSetting)antiAliasingDropdown.value;
            }

            if (masterVolumeSlider != null)
            {
                settings.masterVolume = masterVolumeSlider.value;
            }

            if (musicVolumeSlider != null)
            {
                settings.musicVolume = musicVolumeSlider.value;
            }

            if (soundEffectsVolumeSlider != null)
            {
                settings.soundEffectsVolume = soundEffectsVolumeSlider.value;
            }

            if (ringSensitivitySlider != null)
            {
                settings.ringDragSensitivity = ringSensitivitySlider.value;
            }

            if (cameraSensitivitySlider != null)
            {
                settings.cameraOrbitSensitivity = cameraSensitivitySlider.value;
            }

            GameSettingsService.ApplyAndSave(settings);
        }

        private void ResetDefaults()
        {
            Populate(GameSettingsService.ResetToDefaults());
        }

        private void ResetBindings()
        {
            InputBindingPersistence.Reset(inputActions);

            if (rebindRows == null)
            {
                return;
            }

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

            if (categoryPanels == null)
            {
                return;
            }

            for (int i = 0; i < categoryPanels.Length; i++)
            {
                if (categoryPanels[i] != null)
                {
                    categoryPanels[i].SetActive(i == selectedCategoryIndex);
                }
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
                : closeButton != null
                    ? closeButton.gameObject
                    : null;

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

                Button upButton =
                    categoryButtons[(i - 1 + categoryButtons.Length) % categoryButtons.Length];

                Button downButton =
                    categoryButtons[(i + 1) % categoryButtons.Length];

                Selectable firstPanelSelectable =
                    categoryPanels != null && i < categoryPanels.Length
                        ? FindFirstSelectable(categoryPanels[i])
                        : null;

                Navigation navigation = current.navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnUp = i == 0 && languageDropdown != null ? languageDropdown : upButton;
                navigation.selectOnDown = downButton;
                navigation.selectOnRight = firstPanelSelectable;
                navigation.selectOnLeft = current;
                current.navigation = navigation;
            }

            if (closeButton != null)
            {
                Navigation closeNavigation = closeButton.navigation;
                closeNavigation.mode = Navigation.Mode.Explicit;
                closeNavigation.selectOnDown = categoryButtons[0];
                closeNavigation.selectOnLeft = languageDropdown != null ? languageDropdown : categoryButtons[0];
                closeNavigation.selectOnRight = categoryButtons[0];
                closeButton.navigation = closeNavigation;
            }
            if (languageDropdown != null)
            {
                Navigation languageNavigation = languageDropdown.navigation;
                languageNavigation.mode = Navigation.Mode.Explicit;
                languageNavigation.selectOnDown = categoryButtons[0];
                languageNavigation.selectOnLeft = categoryButtons[0];
                languageNavigation.selectOnRight = closeButton;
                languageNavigation.selectOnUp = closeButton;
                languageDropdown.navigation = languageNavigation;
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
            if (mode == FullScreenMode.ExclusiveFullScreen)
            {
                return 1;
            }

            if (mode == FullScreenMode.Windowed)
            {
                return 2;
            }

            return 0;
        }

        private static FullScreenMode GetScreenMode(int index)
        {
            if (index == 1)
            {
                return FullScreenMode.ExclusiveFullScreen;
            }

            if (index == 2)
            {
                return FullScreenMode.Windowed;
            }

            return FullScreenMode.FullScreenWindow;
        }
    }
}
