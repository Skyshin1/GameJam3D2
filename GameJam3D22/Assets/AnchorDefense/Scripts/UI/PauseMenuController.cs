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
            resumeButton.onClick.AddListener(Resume);
            settingsButton.onClick.AddListener(OpenSettings);
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
            gameFlow.StateChanged += HandleGameStateChanged;
            panelRoot.SetActive(false);
        }

        private void Update()
        {
            if (input == null || gameFlow == null || !gameFlow.IsPlaying ||
                !input.Pause.WasPressedThisFrame())
            {
                return;
            }

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
        }

        private void OnDestroy()
        {
            resumeButton.onClick.RemoveListener(Resume);
            settingsButton.onClick.RemoveListener(OpenSettings);
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
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
            settingsMenu?.Open();
        }

        private void SetPaused(bool paused)
        {
            panelRoot.SetActive(paused);
            if (paused)
            {
                EventSystem.current?.SetSelectedGameObject(resumeButton.gameObject);
            }
            if (gameFlow != null && gameFlow.IsPlaying)
            {
                Time.timeScale = paused ? 0f : 1f;
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.GameOver)
            {
                settingsMenu?.Close();
                panelRoot.SetActive(false);
            }
        }

        private static void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
    }
}
