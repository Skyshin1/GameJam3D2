using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AnchorDefense
{
    public sealed class CatCourierSpeechBubble : MonoBehaviour
    {
        public const float CompactHeight = 102f;
        [SerializeField] private TMP_Text text;
        [SerializeField] private ScrollRect scroll;
        private string measuredText;
        private float measuredWidth = -1, measuredHeight = -1;

        public static CatCourierSpeechBubble Configure(RectTransform bubble, TMP_Text speech)
        {
            var result = bubble.GetComponent<CatCourierSpeechBubble>() ?? bubble.gameObject.AddComponent<CatCourierSpeechBubble>();
            var viewport = bubble.Find("Speech Viewport") as RectTransform;
            if (viewport == null)
                viewport = (RectTransform)new GameObject("Speech Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D)).transform;
            viewport.SetParent(bubble, false);
            viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(20, 18); viewport.offsetMax = new Vector2(-18, -16);
            viewport.GetComponent<Image>().color = Color.clear;
            speech.rectTransform.SetParent(viewport, false);
            speech.rectTransform.anchorMin = new Vector2(0, 1); speech.rectTransform.anchorMax = Vector2.one;
            speech.rectTransform.pivot = new Vector2(.5f, 1);
            speech.rectTransform.anchoredPosition = Vector2.zero;
            speech.overflowMode = TextOverflowModes.Overflow;
            speech.enableWordWrapping = true; speech.enableAutoSizing = false;
            speech.alignment = TextAlignmentOptions.TopLeft; speech.raycastTarget = true;
            var scroll = viewport.GetComponent<ScrollRect>() ?? viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = speech.rectTransform;
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 28;
            ConfigureScrollIndicator(scroll, bubble);
            result.text = speech; result.scroll = scroll;
            return result;
        }

        public static void ConfigureScrollIndicator(ScrollRect scroll, RectTransform paper)
        {
            if (scroll.verticalScrollbar != null) return;
            var track = (RectTransform)new GameObject("Reading Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar)).transform;
            track.SetParent(paper, false);
            track.gameObject.layer = paper.gameObject.layer;
            track.anchorMin = new Vector2(1, 0); track.anchorMax = Vector2.one;
            track.pivot = new Vector2(1, .5f); track.offsetMin = new Vector2(-11, 18); track.offsetMax = new Vector2(-6, -16);
            track.GetComponent<Image>().color = new Color(.38f, .26f, .43f, .12f);
            var thumb = (RectTransform)new GameObject("Thumb", typeof(RectTransform), typeof(Image)).transform;
            thumb.SetParent(track, false); thumb.anchorMin = Vector2.zero; thumb.anchorMax = Vector2.one;
            thumb.gameObject.layer = paper.gameObject.layer;
            thumb.offsetMin = thumb.offsetMax = Vector2.zero;
            thumb.GetComponent<Image>().color = new Color(.48f, .32f, .51f, .65f);
            var bar = track.GetComponent<Scrollbar>(); bar.handleRect = thumb;
            bar.targetGraphic = thumb.GetComponent<Image>(); bar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }

        public Vector2 Fit(float width, float maximumHeight)
        {
            var rect = (RectTransform)transform;
            if (measuredText == text.text && Mathf.Approximately(measuredWidth, width) && Mathf.Approximately(measuredHeight, maximumHeight))
                return rect.sizeDelta;
            float position = scroll.verticalNormalizedPosition;
            float contentHeight = text.GetPreferredValues(text.text, width - 38f, Mathf.Infinity).y + 2f;
            float height = Mathf.Min(CompactHeight, maximumHeight);
            rect.sizeDelta = new Vector2(width, height);
            text.rectTransform.sizeDelta = new Vector2(0, Mathf.Max(contentHeight, height - 34f));
            scroll.enabled = contentHeight > height - 34f + .1f;
            if (scroll.verticalScrollbar != null) scroll.verticalScrollbar.gameObject.SetActive(scroll.enabled);
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = scroll.enabled ? position : 1f;
            if (!scroll.enabled) text.rectTransform.anchoredPosition = Vector2.zero;
            measuredText = text.text; measuredWidth = width; measuredHeight = maximumHeight;
            return rect.sizeDelta;
        }

        public void ResetReadingPosition()
        {
            scroll.StopMovement(); scroll.verticalNormalizedPosition = 1f;
            text.rectTransform.anchoredPosition = Vector2.zero;
        }
    }
}
