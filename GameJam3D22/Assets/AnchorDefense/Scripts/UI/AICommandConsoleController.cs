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
        private AICommandExecutionResult lastResult;
        private AICompanionService companion;
        private UnityEngine.InputSystem.Keyboard imeKeyboard;
        private bool imeComposing;
        private int imeCommitFrame = -10;
        private void OnImeComposition(UnityEngine.InputSystem.LowLevel.IMECompositionString composition)
        {
            imeComposing = composition.Count > 0;
            if (!imeComposing) imeCommitFrame = Time.frameCount;
        }
        public event System.Action<bool> OpenChanged;
        public RectTransform PanelRect => panelRoot != null ? panelRoot.transform as RectTransform : null;
        public Button Opener => openButton;
        public TMP_InputField InputField => inputField;

        public void AttachCompanion(AICompanionService value)
        {
            companion = value;
            companion.BusyChanged += HandleBusyChanged;
            RefreshLanguage();
        }
        public void ShowCompanionMessage(string message, bool systemMessage, bool concerned = false)
        {
            SetStatus(message, false);
            if (resultText != null) resultText.text = string.Empty;
            if (resultIcon != null) resultIcon.gameObject.SetActive(false);
            if (!systemMessage) portraitMotion?.React(!concerned);
        }
        public void OpenForRescue()
        {
            CancelAutoClose();
            panelRoot.SetActive(true);
            openButton.gameObject.SetActive(false);
            companion.ConversationOpen = true;
            if (input != null) input.IsTextEntryActive = true;
            inputField?.ActivateInputField();
            ShowCompanionMessage(GameLocalization.T("核心告急 · 是否需要救援？", "Core critical · Need rescue?"), true);
            portraitMotion?.SetMood(AICommandAssistantMood.Failure);
            SetCompanionLabels(true);
            OpenChanged?.Invoke(true);
        }
        public void CloseAfterRescue() { Close(); }

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
            GameLocalization.LanguageChanged += RefreshLanguage;
            RefreshBalance();
            SetStatus(service != null && service.IsConfigured
                ? GameLocalization.T("星核：想让我帮你做什么？", "Core: How can I help?")
                : GameLocalization.T("星核：还没有连上 AI 服务。", "Core: The AI service is not connected."), false);
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
            if (imeKeyboard != UnityEngine.InputSystem.Keyboard.current)
            {
                if (imeKeyboard != null) imeKeyboard.onIMECompositionChange -= OnImeComposition;
                imeKeyboard = UnityEngine.InputSystem.Keyboard.current;
                if (imeKeyboard != null) imeKeyboard.onIMECompositionChange += OnImeComposition;
            }
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
                var selected = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
                if (companion != null && companion.RescuePending && selected != null &&
                    selected.GetComponent<Button>() != null && selected != submitButton.gameObject) return;
                SubmitFromUi();
            }
        }

        private void OnDestroy()
        {
            if (imeKeyboard != null) imeKeyboard.onIMECompositionChange -= OnImeComposition;
            if (companion != null) companion.BusyChanged -= HandleBusyChanged;
            GameLocalization.LanguageChanged -= RefreshLanguage;
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

        public void Open()
        {
            if (!initialized || gameFlow == null || !gameFlow.IsPlaying || Time.timeScale <= 0f) return;
            CancelAutoClose();
            panelRoot.SetActive(true);
            openButton.gameObject.SetActive(false);
            portraitMotion?.SetMood(AICommandAssistantMood.Listening);
            if (input != null) input.IsTextEntryActive = true;
            inputField?.ActivateInputField();
            if (companion != null)
            {
                companion.ConversationOpen = true;
                SetCompanionLabels(false);
                ShowCompanionMessage(GameLocalization.T("聊天免费 · 游戏操作按原规则消耗指令点", "Chat is free · Game actions use command points"), true);
                OpenChanged?.Invoke(true);
                return;
            }
            SetStatus(service.IsConfigured
                ? GameLocalization.T("星核：想让我帮你做什么？", "Core: How can I help?")
                : GameLocalization.T("星核：还没有连上 AI 服务。", "Core: The AI service is not connected."), false);
        }

        private void Close()
        {
            if (!IsOpen) return;
            if (companion != null && companion.RescuePending) { companion.ResolveRescue(false); return; }
            CancelAutoClose();
            inputField?.DeactivateInputField();
            portraitMotion?.SetMood(AICommandAssistantMood.Idle);
            panelRoot.SetActive(false);
            openButton.gameObject.SetActive(gameFlow != null && gameFlow.IsPlaying);
            if (input != null) input.IsTextEntryActive = false;
            if (companion != null) companion.ConversationOpen = false;
            OpenChanged?.Invoke(false);
        }

        private async void SubmitFromUi()
        {
            if (companion != null)
            {
                if (companion.IsBusy || service.IsBusy) return;
                if (imeComposing || Time.frameCount <= imeCommitFrame + 1) return;
                string playerText = inputField != null ? inputField.text : string.Empty;
                if (string.IsNullOrWhiteSpace(playerText)) return;
                if (inputField != null) inputField.text = string.Empty;
                ShowCompanionMessage(GameLocalization.T("正在回应…", "Thinking…"), true);
                await companion.Submit(playerText);
                if (this != null && IsOpen) inputField?.ActivateInputField();
                return;
            }
            if (service == null || service.IsBusy) return;
            CancelAutoClose();
            string text = inputField != null ? inputField.text : string.Empty;
            SetStatus(GameLocalization.T("星核：让我想想怎么做……", "Core: Let me think…"), false);
            AICommandExecutionResult result = await service.Submit(text);
            lastResult = result;
            SetStatus(GameLocalization.T("星核：", "Core: ") + GameLocalization.AIResultMessage(result), result.Succeeded);
            portraitMotion?.React(result.Succeeded);
            if (result.Succeeded)
            {
                if (inputField != null) inputField.text = string.Empty;
                if (resultText != null)
                    resultText.text = GameLocalization.IsEnglish ? GameLocalization.AIOperationSummary(result) :
                        result.Operations.Count > 0 ? result.OperationSummary : result.ZoneId >= 0
                        ? $"{result.OperationSummary ?? result.Skill?.DisplayName}  /  C{result.ZoneId + 1:00}"
                        : result.Skill?.DisplayName ?? result.Message;
                if (resultIcon != null)
                {
                    Sprite icon = result.Skill != null ? result.Skill.Icon :
                        result.Operations.Count > 0 ? result.Operations[0].Upgrade?.Icon : null;
                    resultIcon.sprite = icon;
                    resultIcon.gameObject.SetActive(icon != null);
                }
            }
            RefreshBalance();
            if (IsOpen) inputField?.ActivateInputField();
            if (result.Succeeded && IsOpen)
                autoCloseRoutine = StartCoroutine(CloseAfterResult());
        }

        private void HandleExecutionUpdated(AICommandExecutionResult result)
        {
            if (companion != null) { RefreshBalance(); return; }
            lastResult = result;
            if (resultText != null) resultText.text = GameLocalization.AIOperationSummary(result);
            SetStatus(GameLocalization.T("星核：", "Core: ") + GameLocalization.AIResultMessage(result), result.Succeeded);
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
            busy = busy || (companion != null && companion.IsBusy) || (service != null && service.IsBusy);
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
            balanceText.text = GameLocalization.IsEnglish
                ? $"POINTS {available} · COST -{cost}"
                : $"指令点 {available} · 技能 -{cost}";
        }

        private void RefreshLanguage()
        {
            RefreshBalance();
            if (companion != null)
            {
                SetCompanionLabels(companion.RescuePending);
                ShowCompanionMessage(GameLocalization.T("聊天免费 · 游戏操作按原规则消耗指令点", "Chat is free · Game actions use command points"), true);
                return;
            }
            if (service != null && service.IsBusy)
                SetStatus(GameLocalization.T("星核：让我想想怎么做……", "Core: Let me think…"), false);
            else if (lastResult != null)
            {
                SetStatus(GameLocalization.T("星核：", "Core: ") + GameLocalization.AIResultMessage(lastResult), lastResult.Succeeded);
                if (resultText != null) resultText.text = GameLocalization.AIOperationSummary(lastResult);
            }
            else SetStatus(service != null && service.IsConfigured
                ? GameLocalization.T("星核：想让我帮你做什么？", "Core: How can I help?")
                : GameLocalization.T("星核：还没有连上 AI 服务。", "Core: The AI service is not connected."), false);
        }

        private void SetStatus(string message, bool success)
        {
            if (statusText == null) return;
            statusText.text = message;
            statusText.color = success
                ? new Color(0.12f, 0.44f, 0.31f)
                : new Color(0.17f, 0.13f, 0.23f);
        }

        private void SetCompanionLabels(bool rescue)
        {
            TMP_Text buttonLabel = submitButton != null ? submitButton.GetComponentInChildren<TMP_Text>(true) : null;
            if (buttonLabel != null) buttonLabel.text = GameLocalization.T("发送", "Send");
            TMP_Text placeholder = inputField != null ? inputField.placeholder as TMP_Text : null;
            if (placeholder != null) placeholder.text = rescue
                ? GameLocalization.T("回复星核，或使用上方选项…", "Reply, or choose an option above…")
                : GameLocalization.T("和星核聊聊，或告诉它你的计划…", "Chat, or tell me your plan…");
            TMP_Text title = panelRoot != null ? panelRoot.transform.Find("Title")?.GetComponent<TMP_Text>() : null;
            if (title != null) title.text = GameLocalization.T("ANCHOR / 星核助手", "ANCHOR / CORE");
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
