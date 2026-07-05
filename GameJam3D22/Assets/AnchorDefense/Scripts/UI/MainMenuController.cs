using System.Collections;
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
        private Coroutine restoreSelectionRoutine;

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

            if (startButton != null)
            {
                startButton.onClick.AddListener(StartGame);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OpenSettings);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(QuitGame);
            }

            if (settingsMenu != null)
            {
                settingsMenu.Closed += RestoreMainMenuSelection;
            }

            ConfigureButtonNavigation();
            ControllerSelectionHighlight.EnsureInHierarchy(transform);
        }

        private void Start()
        {
            ControllerSelectionHighlight.EnsureInHierarchy(transform);
            ControllerSelectionHighlight.RefreshAllInHierarchy(transform);

            SelectMainMenuButton(startButton != null ? startButton.gameObject : null);
        }

        private void Update()
        {
            if (settingsMenu != null && settingsMenu.IsOpen)
            {
                return;
            }

            EventSystem eventSystem = EventSystem.current;
            GameObject selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;

            if (selected != null && selected.activeInHierarchy && IsMainMenuObject(selected))
            {
                lastMainMenuSelection = selected;
                return;
            }

            SelectMainMenuButton(lastMainMenuSelection != null
                ? lastMainMenuSelection
                : startButton != null
                    ? startButton.gameObject
                    : null);
        }

        private void OnDestroy()
        {
            if (restoreSelectionRoutine != null)
            {
                StopCoroutine(restoreSelectionRoutine);
                restoreSelectionRoutine = null;
            }

            if (startButton != null)
            {
                startButton.onClick.RemoveListener(StartGame);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(OpenSettings);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(QuitGame);
            }

            if (settingsMenu != null)
            {
                settingsMenu.Closed -= RestoreMainMenuSelection;
            }
        }

        private void OpenSettings()
        {
            lastMainMenuSelection = settingsButton != null
                ? settingsButton.gameObject
                : startButton != null
                    ? startButton.gameObject
                    : null;

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                eventSystem.SetSelectedGameObject(null);
            }

            settingsMenu?.Open();
        }

        private void RestoreMainMenuSelection()
        {
            if (restoreSelectionRoutine != null)
            {
                StopCoroutine(restoreSelectionRoutine);
            }

            restoreSelectionRoutine = StartCoroutine(RestoreMainMenuSelectionNextFrame());
        }

        private IEnumerator RestoreMainMenuSelectionNextFrame()
        {
            yield return null;
            yield return null;

            ControllerSelectionHighlight.EnsureInHierarchy(transform);
            ControllerSelectionHighlight.RefreshAllInHierarchy(transform);

            GameObject target = settingsButton != null
                ? settingsButton.gameObject
                : startButton != null
                    ? startButton.gameObject
                    : null;

            SelectMainMenuButton(target);

            yield return null;

            ControllerSelectionHighlight.RefreshAllInHierarchy(transform);

            restoreSelectionRoutine = null;
        }

        private void SelectMainMenuButton(GameObject target)
        {
            if (target == null || !target.activeInHierarchy)
            {
                target = startButton != null ? startButton.gameObject : null;
            }

            if (target == null || !target.activeInHierarchy)
            {
                return;
            }

            lastMainMenuSelection = target;

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return;
            }

            eventSystem.sendNavigationEvents = true;

            eventSystem.SetSelectedGameObject(null);
            eventSystem.SetSelectedGameObject(target);

            ControllerSelectionHighlight highlight = target.GetComponent<ControllerSelectionHighlight>();
            if (highlight != null)
            {
                highlight.ForceRefreshPublic();
            }
        }

        private bool IsMainMenuObject(GameObject target)
        {
            if (target == null)
            {
                return false;
            }

            return target.transform == transform || target.transform.IsChildOf(transform);
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
            if (button == null)
            {
                return;
            }

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

            InputSystemUIInputModule inputModule =
                eventSystem.GetComponent<InputSystemUIInputModule>();

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