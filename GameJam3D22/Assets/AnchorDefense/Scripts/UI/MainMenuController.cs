using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnchorDefense
{
    public static class SceneLoadRequest
    {
        public static string TargetScene { get; set; } = "Gameplay";
    }

    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private SettingsMenuController settingsMenu;
        [SerializeField] private InputActionAsset inputActions;

        public void Configure(
            Button start,
            Button settings,
            Button quit,
            SettingsMenuController menu,
            InputActionAsset actions)
        {
            startButton = start;
            settingsButton = settings;
            quitButton = quit;
            settingsMenu = menu;
            inputActions = actions;
        }

        private void Awake()
        {
            Time.timeScale = 1f;
            GameSettingsService.EnsureLoaded();
            InputBindingPersistence.Load(inputActions);
            startButton.onClick.AddListener(StartGame);
            settingsButton.onClick.AddListener(settingsMenu.Open);
            quitButton.onClick.AddListener(QuitGame);
        }

        private void Start()
        {
            EventSystem.current?.SetSelectedGameObject(startButton.gameObject);
        }

        private void OnDestroy()
        {
            startButton.onClick.RemoveListener(StartGame);
            settingsButton.onClick.RemoveListener(settingsMenu.Open);
            quitButton.onClick.RemoveListener(QuitGame);
        }

        private static void StartGame()
        {
            SceneLoadRequest.TargetScene = "Gameplay";
            SceneManager.LoadScene("Loading");
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
