using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AnchorDefense
{
    public sealed class CatCourierLayout : MonoBehaviour
    {
        public int layoutVersion = 1;
        public RectTransform cat, bubble, inputStrip, sticker, rescueChoices, historyPanel, historyButton;
        public TMP_Text historyText, alertText;
        public CatCourierPortrait portrait;
        [Min(240f)] public float maximumBubbleWidth = 420f;
        [Min(300f)] public float maximumInputWidth = 530f;
        public bool reducedMotion;
        private AICommandConsoleController console;
        private AICompanionService companion;
        private CubeZoneEditModeController editor;
        private Canvas canvas;
        private CanvasGroup group;
        private float appeared, dismissAt = -1;
        private Vector2 bubbleSize;
        public bool IsPointerOverInput => inputStrip != null && RectTransformUtility.RectangleContainsScreenPoint(inputStrip,
            UnityEngine.InputSystem.Mouse.current?.position.ReadValue() ?? new Vector2(-1,-1), canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera);
        public float AvailableBubbleWidth => Mathf.Clamp(SafeRect().width - 180f, 240f, maximumBubbleWidth);
        public void Initialize(AICompanionService service, AICommandConsoleController controller)
        {
            companion = service; console = controller; canvas = GetComponent<Canvas>();
            group = GetComponent<CanvasGroup>();
            editor = FindObjectOfType<CubeZoneEditModeController>(true);
            historyButton.GetComponent<Button>().onClick.AddListener(ToggleHistory);
            historyPanel.gameObject.SetActive(false);
            GameLocalization.LanguageChanged += RefreshLanguage;
            RefreshLanguage(); ApplyLayout();
        }
        public void Present(Vector2 size)
        {
            bubbleSize = size; appeared = Time.unscaledTime; dismissAt = -1;
            historyPanel.gameObject.SetActive(false);
            ApplyLayout();
        }
        public bool DismissBubble(float now)
        {
            if (reducedMotion) return true;
            if (dismissAt < 0) dismissAt = now;
            float t = Mathf.Clamp01((now - dismissAt) / .18f);
            bubble.localScale = new Vector3(1f - t * .06f, 1f - t * .22f, 1f);
            bubble.GetComponent<CanvasGroup>().alpha = 1f - t;
            return t >= 1f;
        }
        private void LateUpdate()
        {
            if (companion == null) return;
            bool hidden = !companion.IsRunning || (GamePause.IsPaused && !companion.RescuePending && !(editor != null && editor.IsEditing));
            group.alpha = hidden ? 0f : 1f;
            group.blocksRaycasts = !hidden;
            portrait.Listening = console.IsOpen;
            portrait.Thinking = companion.IsBusy;
            portrait.Worried = companion.RescuePending;
            portrait.reducedMotion = reducedMotion;
            alertText.gameObject.SetActive(companion.RescuePending || companion.IsBusy);
            alertText.text = companion.RescuePending ? "!" : "…";
            if (companion.RescuePending) historyPanel.gameObject.SetActive(false);
            if (bubble.gameObject.activeSelf && dismissAt < 0)
            {
                float t = reducedMotion ? 1f : Mathf.Clamp01((Time.unscaledTime - appeared) / .22f);
                float overshoot = Mathf.Sin(t * Mathf.PI) * .035f;
                bubble.localScale = Vector3.one * (Mathf.Lerp(.94f, 1f, t) + overshoot);
                bubble.GetComponent<CanvasGroup>().alpha = t;
            }
            ApplyLayout();
        }
        public void ApplyLayout()
        {
            if (canvas == null) canvas = GetComponent<Canvas>();
            Rect safe = SafeRect();
            Rect root = ((RectTransform)transform).rect;
            float left = safe.xMin - root.xMin + 18f, right = safe.xMax - root.xMin - 18f;
            float bottom = safe.yMin - root.yMin + 16f;
            bool leftDock = editor != null && editor.IsEditing;
            float catX = leftDock ? left + 58f : right - 58f;
            Place(cat, catX, bottom + 57f, 116f, 114f);
            bool open = console != null && console.IsOpen;
            float width = Mathf.Min(maximumInputWidth, safe.width - 36f);
            float inputX = leftDock ? left + width * .5f : right - width * .5f - 124f;
            inputX = Mathf.Clamp(inputX, left + width * .5f, right - width * .5f);
            Place(inputStrip, inputX, bottom + 47f, width, 94f);
            float bw = bubbleSize.x > 0 ? Mathf.Min(bubbleSize.x, AvailableBubbleWidth) : maximumBubbleWidth;
            float bh = bubbleSize.y > 0 ? bubbleSize.y : 112f;
            float bx = leftDock ? left + 128f + bw * .5f : right - 128f - bw * .5f;
            bx = Mathf.Clamp(bx, left + bw * .5f, right - bw * .5f);
            float bubbleBottom = bottom + (open ? 108f : sticker.gameObject.activeSelf ? 76f : 12f);
            Place(bubble, bx, bubbleBottom + bh * .5f, bw, bh);
            Place(sticker, bx, bottom + (open ? 135f : 35f), Mathf.Min(300f, bw), 58f);
            if (open && sticker.gameObject.activeSelf) Place(bubble, bx, bottom + 178f + bh * .5f, bw, bh);
            float choicesWidth = Mathf.Min(400f, safe.width - 36f);
            Place(rescueChoices, Mathf.Clamp(bx, left + choicesWidth * .5f, right - choicesWidth * .5f), bubbleBottom + bh + 36f, choicesWidth, 54f);
            Place(historyButton, catX, bottom + 125f, 52f, 30f);
            float historyWidth = Mathf.Min(500f, safe.width - 36f), historyHeight = Mathf.Min(320f, safe.height - 210f);
            Place(historyPanel, Mathf.Clamp(bx, left + historyWidth * .5f, right - historyWidth * .5f), bottom + 162f + historyHeight * .5f, historyWidth, historyHeight);
            historyButton.gameObject.SetActive(companion == null || !companion.RescuePending);
        }
        private Rect SafeRect()
        {
            var root = (RectTransform)transform;
            if (canvas == null || canvas.renderMode == RenderMode.WorldSpace) return root.rect;
            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, Screen.safeArea.min, cam, out Vector2 min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, Screen.safeArea.max, cam, out Vector2 max);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = Vector2.zero; rect.pivot = Vector2.one * .5f;
            rect.anchoredPosition = new Vector2(x,y); rect.sizeDelta = new Vector2(width,height);
        }
        private void ToggleHistory()
        {
            if (companion == null || companion.RescuePending) return;
            bool open = !historyPanel.gameObject.activeSelf;
            historyPanel.gameObject.SetActive(open);
            if (!open) return;
            bubble.gameObject.SetActive(false);
            var messages = companion.Memory.Recent();
            var text = new StringBuilder();
            for (int i = Mathf.Max(0, messages.Length - 6); i < messages.Length; i++)
                text.Append(messages[i].role == "user" ? GameLocalization.T("你：", "You: ") : GameLocalization.T("猫咪：", "Cat: "))
                    .AppendLine(messages[i].content).AppendLine();
            historyText.text = text.Length > 0 ? text.ToString() : GameLocalization.T("这一页还空着，和我聊聊吧。", "A fresh page. Tell me what's on your mind.");
            Canvas.ForceUpdateCanvases();
            historyPanel.GetComponentInChildren<ScrollRect>().verticalNormalizedPosition = 0f;
        }
        private void RefreshLanguage()
        { historyButton.GetComponentInChildren<TMP_Text>().text = GameLocalization.T("信笺", "Log"); }
        private void OnDestroy() => GameLocalization.LanguageChanged -= RefreshLanguage;
    }
}
