using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AnchorDefense
{
    public sealed class AICommandConsoleController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private Image assistantPortrait;
        [SerializeField] private bool mirrorPortraitOnOpenButton = true;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button submitButton;
        [SerializeField] private TMP_Text balanceText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private Image resultIcon;
        [SerializeField] private GameObject loadingVisual;

        private AICommandService service;
        private GameInputController input;
        private GameFlowController gameFlow;
        private AICommandAssistantMotion portraitMotion;
        private bool initialized;
        private Coroutine autoCloseRoutine;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        public void Configure(GameObject panel, Button open, Button close, TMP_InputField commandInput,
            Button submit, TMP_Text balance, TMP_Text status, TMP_Text result,
            Image icon, GameObject loading)
        {
            panelRoot = panel;
            openButton = open;
            closeButton = close;
            inputField = commandInput;
            submitButton = submit;
            balanceText = balance;
            statusText = status;
            resultText = result;
            resultIcon = icon;
            loadingVisual = loading;
        }

        public void Initialize(AICommandService commandService, GameInputController inputController,
            GameFlowController flow)
        {
            service = commandService;
            input = inputController;
            gameFlow = flow;
            initialized = service != null;

            if (inputField != null) inputField.characterLimit = service != null ? service.MaximumInputLength : 120;
            if (service != null)
            {
                service.BusyChanged += HandleBusyChanged;
                service.ExecutionUpdated += HandleExecutionUpdated;
                service.Wallet.Changed += HandleWalletChanged;
            }
            if (gameFlow != null) gameFlow.StateChanged += HandleGameStateChanged;
            RefreshBalance();
            SetStatus(service != null && service.IsConfigured
                ? "星核：想让我帮你做什么？" : "星核：还没有连上 AI 服务。", false);
            HandleBusyChanged(false);
        }

        private void Awake()
        {
            if (assistantPortrait == null && panelRoot != null)
                assistantPortrait = panelRoot.GetComponentInChildren<AICommandAssistantMotion>(true)
                    ?.GetComponent<Image>();
            portraitMotion = assistantPortrait != null
                ? assistantPortrait.GetComponent<AICommandAssistantMotion>() : null;
            if (mirrorPortraitOnOpenButton && openButton != null &&
                openButton.image != null && assistantPortrait != null)
            {
                openButton.image.sprite = assistantPortrait.sprite;
                openButton.image.color = Color.white;
                openButton.image.preserveAspect = true;
            }
            openButton?.GetComponent<AICommandAssistantMotion>()?.CaptureAppearance();
            openButton?.onClick.AddListener(Open);
            closeButton?.onClick.AddListener(Close);
            submitButton?.onClick.AddListener(SubmitFromUi);
            if (panelRoot != null) panelRoot.SetActive(false);
            if (loadingVisual != null) loadingVisual.SetActive(false);
            if (resultIcon != null) resultIcon.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!initialized || input == null) return;

            if (!IsOpen)
            {
                if (input.ToggleAICommand != null && input.ToggleAICommand.WasPressedThisFrame()) Open();
                return;
            }

            if (input.CloseAICommand != null && input.CloseAICommand.WasPressedThisFrame())
            {
                Close();
                return;
            }

            if (input.SubmitAICommand != null && input.SubmitAICommand.WasPressedThisFrame())
            {
                SubmitFromUi();
            }
        }

        private void OnDestroy()
        {
            CancelAutoClose();
            openButton?.onClick.RemoveListener(Open);
            closeButton?.onClick.RemoveListener(Close);
            submitButton?.onClick.RemoveListener(SubmitFromUi);
            if (service != null)
            {
                service.BusyChanged -= HandleBusyChanged;
                service.ExecutionUpdated -= HandleExecutionUpdated;
                service.Wallet.Changed -= HandleWalletChanged;
            }
            if (gameFlow != null) gameFlow.StateChanged -= HandleGameStateChanged;
            if (input != null) input.IsTextEntryActive = false;
        }

        private void Open()
        {
            if (!initialized || gameFlow == null || !gameFlow.IsPlaying || Time.timeScale <= 0f) return;
            CancelAutoClose();
            panelRoot.SetActive(true);
            openButton.gameObject.SetActive(false);
            portraitMotion?.SetMood(AICommandAssistantMood.Listening);
            if (input != null) input.IsTextEntryActive = true;
            inputField?.ActivateInputField();
            SetStatus(service.IsConfigured
                ? "星核：想让我帮你做什么？" : "星核：还没有连上 AI 服务。", false);
        }

        private void Close()
        {
            if (!IsOpen) return;
            CancelAutoClose();
            inputField?.DeactivateInputField();
            portraitMotion?.SetMood(AICommandAssistantMood.Idle);
            panelRoot.SetActive(false);
            openButton.gameObject.SetActive(gameFlow != null && gameFlow.IsPlaying);
            if (input != null) input.IsTextEntryActive = false;
        }

        private async void SubmitFromUi()
        {
            if (service == null || service.IsBusy) return;
            CancelAutoClose();
            string text = inputField != null ? inputField.text : string.Empty;
            SetStatus("星核：让我想想怎么做……", false);
            AICommandExecutionResult result = await service.Submit(text);
            SetStatus($"星核：{result.Message}", result.Succeeded);
            portraitMotion?.React(result.Succeeded);
            if (result.Succeeded)
            {
                if (inputField != null) inputField.text = string.Empty;
                if (resultText != null)
                    resultText.text = result.Operations.Count > 0 ? result.OperationSummary : result.ZoneId >= 0
                        ? $"{result.OperationSummary ?? result.Skill.DisplayName}  /  C{result.ZoneId + 1:00}"
                        : result.Skill.DisplayName;
                if (resultIcon != null)
                {
                    resultIcon.sprite = result.Skill.Icon;
                    resultIcon.gameObject.SetActive(result.Skill.Icon != null);
                }
            }
            RefreshBalance();
            if (IsOpen) inputField?.ActivateInputField();
            if (result.Succeeded && IsOpen)
                autoCloseRoutine = StartCoroutine(CloseAfterResult());
        }

        private void HandleExecutionUpdated(AICommandExecutionResult result)
        {
            if (resultText != null) resultText.text = result.OperationSummary;
            SetStatus($"星核：{result.Message}", result.Succeeded);
            if (!result.Succeeded) CancelAutoClose();
        }

        private IEnumerator CloseAfterResult()
        {
            yield return new WaitForSecondsRealtime(3f);
            autoCloseRoutine = null;
            if (IsOpen && !service.IsBusy &&
                (inputField == null || string.IsNullOrWhiteSpace(inputField.text)))
                Close();
        }

        private void CancelAutoClose()
        {
            if (autoCloseRoutine == null) return;
            StopCoroutine(autoCloseRoutine);
            autoCloseRoutine = null;
        }

        private void HandleBusyChanged(bool busy)
        {
            if (submitButton != null) submitButton.interactable = !busy;
            if (inputField != null) inputField.interactable = !busy;
            if (loadingVisual != null) loadingVisual.SetActive(busy);
            if (busy) portraitMotion?.SetMood(AICommandAssistantMood.Thinking);
        }

        private void HandleWalletChanged(int totalKills, int availableKills) => RefreshBalance();

        private void RefreshBalance()
        {
            if (balanceText == null) return;
            int available = service?.Wallet != null ? service.Wallet.AvailableKills : 0;
            int cost = service != null ? service.CommandCost : 10;
            balanceText.text = $"指令点 {available} · -{cost}";
        }

        private void SetStatus(string message, bool success)
        {
            if (statusText == null) return;
            statusText.text = message;
            statusText.color = success
                ? new Color(0.12f, 0.44f, 0.31f)
                : new Color(0.17f, 0.13f, 0.23f);
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.GameOver)
            {
                Close();
                if (openButton != null) openButton.gameObject.SetActive(false);
            }
        }
    }
}
