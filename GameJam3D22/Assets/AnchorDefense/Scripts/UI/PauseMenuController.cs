using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnchorDefense
{
    public sealed class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private SettingsMenuController settingsMenu;
        [SerializeField] private GameInputController input;
        [SerializeField] private GameFlowController gameFlow;
        [SerializeField] private UpgradeTreeController upgradeTree;
        [SerializeField] private CubeZoneEditModeController zoneEditor;

        private GameObject lastPauseMenuSelection;

        public bool IsPaused => panelRoot != null && panelRoot.activeSelf;

        public void ConfigureView(
            GameObject panel,
            Button resume,
            Button settings,
            Button mainMenu,
            SettingsMenuController settingsController)
        {
            panelRoot = panel;
            resumeButton = resume;
            settingsButton = settings;
            mainMenuButton = mainMenu;
            settingsMenu = settingsController;
        }

        public void ConfigureRuntime(
            GameInputController inputController,
            GameFlowController flow,
            UpgradeTreeController tree)
        {
            input = inputController;
            gameFlow = flow;
            upgradeTree = tree;
        }

        public void ConfigureZoneEditor(CubeZoneEditModeController editor)
        {
            zoneEditor = editor;
        }

        private void Awake()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(Resume);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OpenSettings);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);
            }

            if (settingsMenu != null)
            {
                settingsMenu.Closed += HandleSettingsClosed;
            }

            if (gameFlow != null)
            {
                gameFlow.StateChanged += HandleGameStateChanged;
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void Update()
        {
            if (input == null || gameFlow == null || !gameFlow.IsPlaying)
            {
                return;
            }

            if (input.Pause.WasPressedThisFrame())
            {
                if (settingsMenu != null && settingsMenu.IsOpen)
                {
                    settingsMenu.Close();
                    return;
                }

                if (zoneEditor != null && zoneEditor.IsEditing)
                {
                    zoneEditor.ExitFromExternal();
                    return;
                }

                if (upgradeTree != null && upgradeTree.IsOpen)
                {
                    upgradeTree.CloseFromExternal();
                    return;
                }

                SetPaused(!IsPaused);
                return;
            }

            if (IsPaused && (settingsMenu == null || !settingsMenu.IsOpen))
            {
                EnsurePauseMenuSelection();
            }
        }

        private void OnDestroy()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(Resume);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(OpenSettings);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
            }

            if (settingsMenu != null)
            {
                settingsMenu.Closed -= HandleSettingsClosed;
            }

            if (gameFlow != null)
            {
                gameFlow.StateChanged -= HandleGameStateChanged;
            }
        }

        private void Resume()
        {
            SetPaused(false);
        }

        private void OpenSettings()
        {
            lastPauseMenuSelection = settingsButton != null
                ? settingsButton.gameObject
                : resumeButton != null
                    ? resumeButton.gameObject
                    : null;

            settingsMenu?.Open();
        }

        private void HandleSettingsClosed()
        {
            if (!IsPaused)
            {
                return;
            }

            SelectPauseMenuButton(lastPauseMenuSelection != null
                ? lastPauseMenuSelection
                : settingsButton != null
                    ? settingsButton.gameObject
                    : resumeButton != null
                        ? resumeButton.gameObject
                        : null);
        }

        private void SetPaused(bool paused)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(paused);
            }

            if (paused)
            {
                lastPauseMenuSelection = resumeButton != null ? resumeButton.gameObject : null;
                SelectPauseMenuButton(lastPauseMenuSelection);
            }
            else
            {
                if (settingsMenu != null && settingsMenu.IsOpen)
                {
                    settingsMenu.Close();
                }

                EventSystem.current?.SetSelectedGameObject(null);
            }

            if (gameFlow != null && gameFlow.IsPlaying)
            {
                Time.timeScale = paused ? 0f : 1f;
            }
        }

        private void EnsurePauseMenuSelection()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return;
            }

            GameObject selected = eventSystem.currentSelectedGameObject;

            if (selected != null && selected.activeInHierarchy)
            {
                if (IsPauseMenuObject(selected))
                {
                    lastPauseMenuSelection = selected;
                    return;
                }
            }

            SelectPauseMenuButton(lastPauseMenuSelection != null
                ? lastPauseMenuSelection
                : resumeButton != null
                    ? resumeButton.gameObject
                    : null);
        }

        private bool IsPauseMenuObject(GameObject target)
        {
            if (target == null || panelRoot == null)
            {
                return false;
            }

            return target.transform == panelRoot.transform ||
                   target.transform.IsChildOf(panelRoot.transform);
        }

        private void SelectPauseMenuButton(GameObject target)
        {
            if (target == null || !target.activeInHierarchy)
            {
                target = resumeButton != null ? resumeButton.gameObject : null;
            }

            if (target == null || !target.activeInHierarchy)
            {
                return;
            }

            lastPauseMenuSelection = target;

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return;
            }

            eventSystem.sendNavigationEvents = true;
            eventSystem.SetSelectedGameObject(null);
            eventSystem.SetSelectedGameObject(target);
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.GameOver)
            {
                settingsMenu?.Close();

                if (panelRoot != null)
                {
                    panelRoot.SetActive(false);
                }

                EventSystem.current?.SetSelectedGameObject(null);
            }
        }

        private static void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
    }
}