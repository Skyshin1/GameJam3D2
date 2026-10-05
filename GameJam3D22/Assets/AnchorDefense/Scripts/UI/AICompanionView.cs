using System.Collections;
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
        private int pageStart;
        private Canvas canvas;
        private int normalSortingOrder;
        private AICommandAssistantMotion dockMotion;
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
                console.CompanionMessage += ShowConsoleMessage;
            }
            gameObject.AddComponent<AICompanionFontFallback>().Initialize(glyphFallback);
            canvas = GetComponent<Canvas>(); normalSortingOrder = canvas.sortingOrder;
            dockMotion = console.Opener.GetComponent<AICommandAssistantMotion>();
            service.Speech += Show;
            service.RescueChanged += OnRescue;
            service.RescueVisual += Flash;
            console.OpenChanged += OnConsoleOpen;
            GameLocalization.LanguageChanged += RefreshLabels;
            acceptButton.onClick.AddListener(Accept);
            declineButton.onClick.AddListener(Decline);
            recommendationButton.onClick.AddListener(FocusRecommendation);
            bubble.gameObject.SetActive(false); rescueChoices.SetActive(false);
            modalBackdrop.gameObject.SetActive(false); rescueFlash.gameObject.SetActive(false);
            speechText.richText = false;
            RefreshLabels();
        }
        private void Update()
        {
            if (service == null) return;
            if (!service.IsRunning)
            {
                revealing = false;
                bubble.gameObject.SetActive(false);
                rescueChoices.SetActive(false); modalBackdrop.gameObject.SetActive(false);
                rescueFlash.gameObject.SetActive(false);
                return;
            }
            if (courier != null && bubble.gameObject.activeSelf && !float.IsInfinity(hideAt) &&
                RectTransformUtility.RectangleContainsScreenPoint(bubble, Mouse.current?.position.ReadValue() ?? new Vector2(-1,-1),
                    canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera)) hideAt += Time.unscaledDeltaTime;
            AdvanceBubble(Time.unscaledTime);
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
            if (courier != null) { courier.ApplyLayout(); return; }
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
        private void Show(CompanionSpeech speech)
        {
            if (this == null || service == null) return;
            revealing = false;
            speechText.maxVisibleCharacters = int.MaxValue;
            if (console.IsOpen && courier == null)
            {
                console.ShowCompanionMessage(speech.Text, speech.SystemMessage,
                    speech.Purpose == CompanionPurpose.RescueQuestion || speech.Purpose == CompanionPurpose.RescueAnswer);
                bubble.gameObject.SetActive(false);
                return;
            }
            speechText.text = speech.Text;
            speechText.pageToDisplay = 1; pageStart = 0;
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
            if (courier != null) width = Mathf.Min(width, Mathf.Max(240f, speechText.GetPreferredValues(speech.Text).x + 40f));
            float textHeight = speechText.GetPreferredValues(speech.Text, width - 36f, 600f).y;
            bubble.sizeDelta = new Vector2(width, Mathf.Clamp(textHeight + (displayedNode != null ? 78f : 32f), 88f, 240f));
            if (courier != null) bubble.sizeDelta = new Vector2(width, 102f);
            speechText.rectTransform.offsetMin = new Vector2(20f, courier == null && displayedNode != null ? 62f : 18f);
            speechText.rectTransform.offsetMax = new Vector2(-18f, -16f);
            bubble.gameObject.SetActive(true);
            if (courier != null) courier.Present(bubble.sizeDelta);
            readingSeconds = Mathf.Max(config.bubbleSeconds, speech.Text.Length * 0.065f);
            // Layout uses the complete string; revealing characters must not move the bubble.
            speechText.ForceMeshUpdate();
            revealCharacters = speechText.textInfo.characterCount;
            if (courier != null && speechText.textInfo.pageCount > 1)
                revealCharacters = speechText.textInfo.pageInfo[0].lastCharacterIndex + 1;
            revealSpeed = Mathf.Max(1f, config.typewriterCharactersPerSecond);
            revealStartedAt = Time.unscaledTime;
            // All visible cat messages share the reveal, including rescue prompts
            // and local operation feedback. Never apply it to the player's input.
            revealing = config.typewriterEnabled && revealCharacters > 0;
            speechText.maxVisibleCharacters = revealing ? 0 : int.MaxValue;
            hideAt = revealing ? float.PositiveInfinity : Time.unscaledTime + readingSeconds;
            dockMotion?.React(speech.Purpose != CompanionPurpose.RescueQuestion);
            if (courier != null)
            {
                if (!speech.SystemMessage) courier.portrait.React(speech.Purpose != CompanionPurpose.RescueQuestion);
                if (!speech.SystemMessage && (speech.Purpose == CompanionPurpose.PlayerTurn || speech.Purpose == CompanionPurpose.CommandResult))
                    console.CloseAfterCompanionReply();
            }
        }
        private void ShowConsoleMessage(string text, bool system, bool concerned)
        {
            Show(new CompanionSpeech { Text = text, SystemMessage = system,
                Purpose = concerned ? CompanionPurpose.RescueQuestion : CompanionPurpose.PlayerTurn });
        }
        private void AdvanceBubble(float now)
        {
            if (!bubble.gameObject.activeSelf) { revealing = false; return; }
            if (revealing)
            {
                int visible = Mathf.Clamp(pageStart + Mathf.FloorToInt((now - revealStartedAt) * revealSpeed), pageStart, revealCharacters);
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
                if (courier != null && speechText.pageToDisplay < speechText.textInfo.pageCount)
                {
                    int page = speechText.pageToDisplay++;
                    pageStart = speechText.textInfo.pageInfo[page].firstCharacterIndex;
                    revealCharacters = speechText.textInfo.pageInfo[page].lastCharacterIndex + 1;
                    revealStartedAt = now; revealing = config.typewriterEnabled;
                    speechText.maxVisibleCharacters = revealing ? pageStart : int.MaxValue;
                    hideAt = revealing ? float.PositiveInfinity : now + readingSeconds;
                }
                else if (courier == null || courier.DismissBubble(now)) bubble.gameObject.SetActive(false);
            }
        }
        private void OnDisable()
        {
            revealing = false;
            if (speechText != null) speechText.maxVisibleCharacters = int.MaxValue;
            if (bubble != null) bubble.gameObject.SetActive(false);
        }
        private void OnConsoleOpen(bool open)
        {
            if (courier != null) { courier.ApplyLayout(); return; }
            if (!open) return;
            revealing = false;
            bubble.gameObject.SetActive(false);
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
        private void Decline() { acceptButton.interactable = declineButton.interactable = false; service.ResolveRescue(false); }
        private void FocusRecommendation()
        {
            if (displayedNode == null || service.RescuePending) return;
            bubble.gameObject.SetActive(false);
            tree?.FocusNode(displayedNode);
        }
        private void RefreshLabels()
        {
            acceptButton.GetComponentInChildren<TMP_Text>(true).text = GameLocalization.T("帮帮我", "Help me");
            declineButton.GetComponentInChildren<TMP_Text>(true).text = GameLocalization.T("我能应付", "I can handle it");
            if (bubble != null) bubble.gameObject.SetActive(false);
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
            if (service != null) { service.Speech -= Show; service.RescueChanged -= OnRescue; service.RescueVisual -= Flash; }
            if (console != null) console.OpenChanged -= OnConsoleOpen;
            if (console != null) console.CompanionMessage -= ShowConsoleMessage;
            GameLocalization.LanguageChanged -= RefreshLabels;
        }
    }
}
