using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AnchorDefense
{
    public sealed class AICompanionView : MonoBehaviour
    {
        [SerializeField] private AICompanionConfig config;
        [Tooltip("Bundled font used only for missing CJK glyphs; the primary art typeface is preserved.")]
        [SerializeField] private TMP_FontAsset glyphFallback;
        [SerializeField] private RectTransform bubble;
        [SerializeField] private TMP_Text speechText;
        [SerializeField] private Button recommendationButton;
        [SerializeField] private TMP_Text recommendationText;
        [SerializeField] private Image recommendationIcon;
        [SerializeField] private GameObject rescueChoices;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button declineButton;
        [SerializeField] private Image modalBackdrop;
        [SerializeField] private Image rescueFlash;
        private AICompanionService service;
        private AICommandConsoleController console;
        private UpgradeTreeController tree;
        private UpgradeNodeDefinition displayedNode;
        private float hideAt;
        private float revealStartedAt, revealSpeed, readingSeconds;
        private int revealCharacters;
        private bool revealing;
        private CatCourierLayout courier;
        private CatCourierSpeechBubble expandingBubble;
        private Canvas canvas;
        private int normalSortingOrder;
        private AICommandAssistantMotion dockMotion;
        private readonly List<CompanionSpeech> pendingSpeech = new List<CompanionSpeech>();
        private CompanionSpeech currentSpeech;
        private CourierPaperGraphic bubblePaper;
        private Color ordinaryPaperColor;
        private const float MaximumHoverReadingExtension = 12f;
        private float hoverReadingExtension;
        public AICompanionConfig Config => config;
        public TMP_FontAsset GlyphFallback => glyphFallback;
        public void SetGlyphFallback(TMP_FontAsset value) => glyphFallback = value;

        public void Configure(AICompanionConfig settings, RectTransform speechBubble, TMP_Text speech,
            Button recommended, TMP_Text nodeText, Image nodeIcon, GameObject choices,
            Button accept, Button decline, Image backdrop, Image flash)
        {
            config = settings; bubble = speechBubble; speechText = speech;
            recommendationButton = recommended; recommendationText = nodeText; recommendationIcon = nodeIcon;
            rescueChoices = choices; acceptButton = accept; declineButton = decline;
            modalBackdrop = backdrop; rescueFlash = flash;
        }
        public void Initialize(AICompanionService companion, AICommandConsoleController commandConsole,
            UpgradeTreeController upgradeTree)
        {
            service = companion; console = commandConsole; tree = upgradeTree;
            courier = GetComponent<CatCourierLayout>();
            if (courier != null)
            {
                courier.Initialize(service, console);
                expandingBubble = CatCourierSpeechBubble.Configure(bubble, speechText);
                console.CompanionMessage += ShowConsoleMessage;
            }
            gameObject.AddComponent<AICompanionFontFallback>().Initialize(glyphFallback);
            canvas = GetComponent<Canvas>(); normalSortingOrder = canvas.sortingOrder;
            dockMotion = console.Opener.GetComponent<AICommandAssistantMotion>();
            bubblePaper = bubble.GetComponentInChildren<CourierPaperGraphic>(true);
            if (bubblePaper != null) ordinaryPaperColor = bubblePaper.color;
            service.Speech += ReceiveSpeech;
            service.RescueChanged += OnRescue;
            service.RescueVisual += Flash;
            service.CommandExecutionFinished += OnCommandExecutionFinished;
            console.OpenChanged += OnConsoleOpen;
            GameLocalization.LanguageChanged += RefreshLabels;
            acceptButton.onClick.AddListener(Accept);
            declineButton.onClick.AddListener(Decline);
            recommendationButton.onClick.AddListener(FocusRecommendation);
            HideBubble(); rescueChoices.SetActive(false);
            modalBackdrop.gameObject.SetActive(false); rescueFlash.gameObject.SetActive(false);
            speechText.richText = false;
            RefreshLabels();
        }
        private void Update()
        {
            if (service == null) return;
            if (!service.IsRunning)
            {
                pendingSpeech.Clear();
                revealing = false;
                HideBubble();
                rescueChoices.SetActive(false); modalBackdrop.gameObject.SetActive(false);
                rescueFlash.gameObject.SetActive(false);
                return;
            }
            if (courier != null)
                ExtendReadingForHover(Time.unscaledDeltaTime,
                    RectTransformUtility.RectangleContainsScreenPoint(bubble, Mouse.current?.position.ReadValue() ?? new Vector2(-1,-1),
                        canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera));
            AdvanceBubble(Time.unscaledTime);
            UpdatePresentationLock();
            if (courier != null && displayedNode != null && service.RecommendedNode != displayedNode)
                recommendationButton.gameObject.SetActive(false);
            if (service.RescuePending && Gamepad.current != null)
            {
                if (Gamepad.current.dpad.left.wasPressedThisFrame) EventSystem.current?.SetSelectedGameObject(acceptButton.gameObject);
                if (Gamepad.current.dpad.right.wasPressedThisFrame) EventSystem.current?.SetSelectedGameObject(declineButton.gameObject);
            }
        }
        private void LateUpdate()
        {
            if (service == null) return;
            if (courier != null)
            {
                if (bubble.gameObject.activeSelf && expandingBubble != null)
                    courier.ResizeBubble(expandingBubble.Fit(SpeechWidth(), courier.AvailableBubbleHeight));
                courier.ApplyLayout(); return;
            }
            if (bubble.gameObject.activeSelf) KeepOnScreen(bubble);
            if (console.IsOpen) KeepOnScreen(console.PanelRect);
            if (rescueChoices.activeSelf)
            {
                // Align choices above the actual artist-edited conversation panel.
                var choicesRect = rescueChoices.transform as RectTransform;
                Vector3[] corners = new Vector3[4]; console.PanelRect.GetWorldCorners(corners);
                choicesRect.position = (corners[1] + corners[2]) * 0.5f + Vector3.up * (12f * canvas.scaleFactor);
                KeepOnScreen(choicesRect);
            }
        }
        private static bool IsBackgroundSpeech(CompanionSpeech speech) => !speech.SystemMessage &&
            (speech.Purpose == CompanionPurpose.Chatter || speech.Purpose == CompanionPurpose.Recommendation ||
             speech.Purpose == CompanionPurpose.EnemyIntroduction || speech.Purpose == CompanionPurpose.PurchaseFeedback);

        private bool BackgroundIsBlocked => service == null || !service.IsRunning || service.IsBusy ||
            service.RescuePending || GamePause.IsPaused || console.IsOpen ||
            (courier != null && courier.historyPanel.gameObject.activeSelf);

        private void ReceiveSpeech(CompanionSpeech speech)
        {
            if (this == null || service == null || speech == null) return;
            if (IsBackgroundSpeech(speech) && (bubble.gameObject.activeSelf || BackgroundIsBlocked || pendingSpeech.Count > 0))
            {
                pendingSpeech.Add(speech);
                UpdatePresentationLock();
                return;
            }
            // Player replies and rescue can interrupt, but an unfinished ambient message is retained.
            if (!IsBackgroundSpeech(speech) && bubble.gameObject.activeSelf && currentSpeech != null && IsBackgroundSpeech(currentSpeech))
                pendingSpeech.Insert(0, currentSpeech);
            Show(speech);
        }

        private void TryShowNextSpeech()
        {
            if (bubble.gameObject.activeSelf || BackgroundIsBlocked) return;
            while (pendingSpeech.Count > 0)
            {
                CompanionSpeech next = pendingSpeech[0]; pendingSpeech.RemoveAt(0);
                // Do not revive an expired or already purchased recommendation after a long conversation.
                if (next.Purpose == CompanionPurpose.Recommendation &&
                    (next.Recommendation == null || next.Recommendation != service.RecommendedNode)) continue;
                Show(next);
                break;
            }
            UpdatePresentationLock();
        }

        private void UpdatePresentationLock()
        {
            if (service == null) return;
            bool historyOpen = courier != null && courier.historyPanel.gameObject.activeSelf;
            service.SpeechPresentationBusy = bubble.gameObject.activeSelf || pendingSpeech.Count > 0 || historyOpen;
            service.SpeechPresentationReason = historyOpen ? "正在查看 Log 历史" : bubble.gameObject.activeSelf
                ? (revealing ? "当前气泡正在逐字显示" : "等待当前气泡阅读结束")
                : pendingSpeech.Count > 0 ? "等待显示队列（" + pendingSpeech.Count + " 条）" : "显示空闲";
        }

        private void ExtendReadingForHover(float seconds, bool hovering)
        {
            if (!hovering || !bubble.gameObject.activeSelf || revealing || float.IsInfinity(hideAt)) return;
            // A parked cursor must not hold the entire autonomous conversation forever.
            float extension = Mathf.Min(Mathf.Max(0f, seconds), MaximumHoverReadingExtension - hoverReadingExtension);
            hoverReadingExtension += extension;
            hideAt += extension;
        }

        private void Show(CompanionSpeech speech)
        {
            if (this == null || service == null) return;
            currentSpeech = speech;
            hoverReadingExtension = 0f;
            if (bubblePaper != null) bubblePaper.color = speech.Purpose == CompanionPurpose.Chatter && !speech.SystemMessage
                ? config.proactiveChatterBubbleColor : ordinaryPaperColor;
            revealing = false;
            speechText.maxVisibleCharacters = int.MaxValue;
            if (console.IsOpen && courier == null)
            {
                console.ShowCompanionMessage(speech.Text, speech.SystemMessage,
                    speech.Purpose == CompanionPurpose.RescueQuestion || speech.Purpose == CompanionPurpose.RescueAnswer);
                HideBubble();
                return;
            }
            speechText.text = speech.Text;
            speechText.pageToDisplay = 1;
            speechText.overflowMode = TextOverflowModes.Overflow;
            displayedNode = speech.Recommendation != null ? speech.Recommendation :
                courier != null && service.RecommendedNode == displayedNode ? displayedNode : null;
            recommendationButton.gameObject.SetActive(displayedNode != null);
            if (displayedNode != null)
            {
                recommendationText.text = displayedNode.LocalizedDisplayName + " · " + displayedNode.KillCost;
                recommendationIcon.sprite = displayedNode.Icon;
                recommendationIcon.enabled = displayedNode.Icon != null;
            }
            float width = courier != null ? courier.AvailableBubbleWidth : Mathf.Min(420f, ((RectTransform)transform).rect.width - 130f);
            float textHeight = speechText.GetPreferredValues(speech.Text, width - 38f, Mathf.Infinity).y;
            if (courier != null)
            {
                expandingBubble.Fit(width, courier.AvailableBubbleHeight);
                expandingBubble.ResetReadingPosition();
            }
            else
            {
                bubble.sizeDelta = new Vector2(width, Mathf.Clamp(textHeight + (displayedNode != null ? 78f : 32f), 88f, 240f));
                speechText.rectTransform.offsetMin = new Vector2(20f, displayedNode != null ? 62f : 18f);
                speechText.rectTransform.offsetMax = new Vector2(-18f, -16f);
            }
            bubble.gameObject.SetActive(true);
            UpdatePresentationLock();
            if (courier != null) courier.Present(bubble.sizeDelta);
            readingSeconds = Mathf.Max(config.bubbleSeconds, speech.Text.Length * 0.065f);
            // Layout uses the complete string; revealing characters must not move the bubble.
            speechText.ForceMeshUpdate();
            revealCharacters = speechText.textInfo.characterCount;
            revealSpeed = Mathf.Max(1f, config.typewriterCharactersPerSecond);
            revealStartedAt = Time.unscaledTime;
            // All visible cat messages share the reveal, including rescue prompts
            // and local operation feedback. Never apply it to the player's input.
            revealing = config.typewriterEnabled && revealCharacters > 0;
            speechText.maxVisibleCharacters = revealing ? 0 : int.MaxValue;
            hideAt = revealing ? float.PositiveInfinity : Time.unscaledTime + readingSeconds;
            if (courier == null) dockMotion?.React(speech.Purpose != CompanionPurpose.RescueQuestion);
            if (courier != null)
            {
                courier.portrait.PresentEmotion(CompanionEmotions.Resolve(speech));
                if (!speech.SystemMessage && (speech.Purpose == CompanionPurpose.PlayerTurn || speech.Purpose == CompanionPurpose.CommandResult))
                    console.CloseAfterCompanionReply();
            }
        }
        private float SpeechWidth() => courier.AvailableBubbleWidth;
        private void HideBubble()
        {
            currentSpeech = null;
            revealing = false;
            if (bubble != null) bubble.gameObject.SetActive(false);
            if (courier != null && courier.portrait != null) courier.portrait.ClearSpeechEmotion();
            UpdatePresentationLock();
        }
        private void ShowConsoleMessage(string text, bool system, bool concerned)
        {
            ReceiveSpeech(new CompanionSpeech { Text = text, SystemMessage = system,
                Purpose = concerned ? CompanionPurpose.RescueQuestion : CompanionPurpose.PlayerTurn });
        }
        private void AdvanceBubble(float now)
        {
            if (!bubble.gameObject.activeSelf) { revealing = false; currentSpeech = null; TryShowNextSpeech(); return; }
            if (revealing)
            {
                int visible = Mathf.Clamp(Mathf.FloorToInt((now - revealStartedAt) * revealSpeed), 0, revealCharacters);
                speechText.maxVisibleCharacters = visible;
                if (visible >= revealCharacters)
                {
                    revealing = false;
                    speechText.maxVisibleCharacters = int.MaxValue;
                    hideAt = now + readingSeconds;
                }
            }
            if (!revealing && now >= hideAt)
            {
                if (courier == null || courier.DismissBubble(now)) { HideBubble(); TryShowNextSpeech(); }
            }
        }
        private void OnDisable()
        {
            pendingSpeech.Clear();
            revealing = false;
            if (speechText != null) speechText.maxVisibleCharacters = int.MaxValue;
            if (bubble != null) HideBubble();
            if (service != null) { service.SpeechPresentationBusy = false; service.SpeechPresentationReason = "界面已关闭"; }
        }
        private void OnConsoleOpen(bool open)
        {
            if (courier != null) { courier.ApplyLayout(); return; }
            if (!open) return;
            revealing = false;
            HideBubble();
        }
        private void OnRescue(bool active)
        {
            modalBackdrop.gameObject.SetActive(active);
            rescueChoices.SetActive(active);
            canvas.sortingOrder = active ? 100 : normalSortingOrder;
            if (active)
            {
                acceptButton.interactable = declineButton.interactable = true;
                console.OpenForRescue();
                EventSystem.current?.SetSelectedGameObject(console.InputField.gameObject);
            }
            else console.CloseAfterRescue();
        }
        private void Accept() { acceptButton.interactable = declineButton.interactable = false; service.ResolveRescue(true); }
        private void OnCommandExecutionFinished(bool succeeded)
        {
            if (succeeded) console.CloseAfterCompanionReply();
        }
        private void Decline() { acceptButton.interactable = declineButton.interactable = false; service.ResolveRescue(false); }
        private void FocusRecommendation()
        {
            if (displayedNode == null || service.RescuePending) return;
            HideBubble();
            tree?.FocusNode(displayedNode);
        }
        private void RefreshLabels()
        {
            pendingSpeech.Clear();
            acceptButton.GetComponentInChildren<TMP_Text>(true).text = GameLocalization.T("帮帮我", "Help me");
            declineButton.GetComponentInChildren<TMP_Text>(true).text = GameLocalization.T("我能应付", "I can handle it");
            if (bubble != null) HideBubble();
        }
        private void Flash(float seconds) => StartCoroutine(PlayFlash(seconds));
        private IEnumerator PlayFlash(float seconds)
        {
            rescueFlash.gameObject.SetActive(true);
            float start = Time.unscaledTime;
            while (Time.unscaledTime - start < Mathf.Max(0.01f, seconds))
            {
                float t = (Time.unscaledTime - start) / Mathf.Max(0.01f, seconds);
                rescueFlash.color = new Color(0.8f, 0.95f, 1f, 0.32f * (1f - t));
                yield return null;
            }
            rescueFlash.gameObject.SetActive(false);
        }
        private void KeepOnScreen(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            Rect safe = Screen.safeArea;
            float dx = corners[0].x < safe.xMin + 8 ? safe.xMin + 8 - corners[0].x :
                corners[2].x > safe.xMax - 8 ? safe.xMax - 8 - corners[2].x : 0;
            float dy = corners[0].y < safe.yMin + 8 ? safe.yMin + 8 - corners[0].y :
                corners[2].y > safe.yMax - 8 ? safe.yMax - 8 - corners[2].y : 0;
            rect.position += new Vector3(dx, dy, 0f);
        }
        private void OnDestroy()
        {
            if (service != null) { service.SpeechPresentationBusy = false; service.Speech -= ReceiveSpeech; service.RescueChanged -= OnRescue; service.RescueVisual -= Flash; }
            if (service != null) service.CommandExecutionFinished -= OnCommandExecutionFinished;
            if (console != null) console.OpenChanged -= OnConsoleOpen;
            if (console != null) console.CompanionMessage -= ShowConsoleMessage;
            GameLocalization.LanguageChanged -= RefreshLabels;
        }
    }
}
