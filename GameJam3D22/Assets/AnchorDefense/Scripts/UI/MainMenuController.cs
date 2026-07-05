using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
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

        private GameObject lastMainMenuSelection;

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
            EnsureUiInputModule();
            startButton.onClick.AddListener(StartGame);
            settingsButton.onClick.AddListener(OpenSettings);
            quitButton.onClick.AddListener(QuitGame);
            settingsMenu.Closed += RestoreMainMenuSelection;
            ConfigureButtonNavigation();
            ControllerSelectionHighlight.EnsureInHierarchy(transform);
        }

        private void Start()
        {
            SelectMainMenuButton(startButton.gameObject);
        }

        private void Update()
        {
            if (settingsMenu != null && settingsMenu.IsOpen)
            {
                return;
            }

            EventSystem eventSystem = EventSystem.current;
            GameObject selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            if (selected != null && selected.activeInHierarchy)
            {
                lastMainMenuSelection = selected;
                return;
            }

            SelectMainMenuButton(lastMainMenuSelection != null
                ? lastMainMenuSelection
                : startButton.gameObject);
        }

        private void OnDestroy()
        {
            startButton.onClick.RemoveListener(StartGame);
            settingsButton.onClick.RemoveListener(OpenSettings);
            quitButton.onClick.RemoveListener(QuitGame);
            if (settingsMenu != null)
            {
                settingsMenu.Closed -= RestoreMainMenuSelection;
            }
        }

        private void OpenSettings()
        {
            lastMainMenuSelection = settingsButton.gameObject;
            settingsMenu.Open();
        }

        private void RestoreMainMenuSelection()
        {
            SelectMainMenuButton(settingsButton.gameObject);
        }

        private void SelectMainMenuButton(GameObject target)
        {
            if (target == null || !target.activeInHierarchy)
            {
                target = startButton != null ? startButton.gameObject : null;
            }
            if (target == null)
            {
                return;
            }

            lastMainMenuSelection = target;
            EventSystem.current?.SetSelectedGameObject(target);
        }

        private void ConfigureButtonNavigation()
        {
            if (startButton == null || settingsButton == null || quitButton == null)
            {
                return;
            }

            SetVerticalNavigation(startButton, quitButton, settingsButton);
            SetVerticalNavigation(settingsButton, startButton, quitButton);
            SetVerticalNavigation(quitButton, settingsButton, startButton);
        }

        private static void SetVerticalNavigation(Button button, Selectable up, Selectable down)
        {
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = up;
            navigation.selectOnDown = down;
            navigation.selectOnLeft = up;
            navigation.selectOnRight = down;
            button.navigation = navigation;
        }

        private static void EnsureUiInputModule()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return;
            }

            eventSystem.sendNavigationEvents = true;
            InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
            if (inputModule.actionsAsset == null)
            {
                inputModule.AssignDefaultActions();
            }
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
