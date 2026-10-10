using UnityEngine;
using UnityEngine.UI;

namespace AnchorDefense
{
    [RequireComponent(typeof(Image))]
    public sealed class CatCourierPortrait : MonoBehaviour
    {
        [Tooltip("Idle, blink, listening, thinking, proud, worried.")]
        public Sprite[] expressions;
        public Sprite helplessExpression;
        public bool reducedMotion;
        private Image portrait;
        private float nextBlink, blinkUntil, transitionStarted;
        private int displayedState = -1;
        private Vector3 restingScale = Vector3.one;
        private Quaternion restingRotation = Quaternion.identity;
        private bool initialized;
        public CompanionEmotion? SpeechEmotion { get; private set; }
        public bool Listening { get; set; }
        public bool Thinking { get; set; }
        public bool Worried { get; set; }
        private void Awake() => EnsureInitialized();
        private void EnsureInitialized()
        {
            // Bootstrap can call the view before this child's Awake has run.
            if (initialized) return;
            portrait = GetComponent<Image>(); nextBlink = Time.unscaledTime + 3f;
            restingScale = transform.localScale; restingRotation = transform.localRotation;
            // Recover instances that an earlier initialization already collapsed.
            if (Mathf.Abs(restingScale.x) < .001f || Mathf.Abs(restingScale.y) < .001f)
                restingScale = Vector3.one;
            initialized = true;
        }
        public void React(bool positive) => PresentEmotion(positive ? CompanionEmotion.Happy : CompanionEmotion.Helpless);
        public void PresentEmotion(CompanionEmotion emotion) { SpeechEmotion = emotion; Update(); }
        public void ClearSpeechEmotion() { SpeechEmotion = null; Update(); }
        private void Update()
        {
            EnsureInitialized();
            float now = Time.unscaledTime;
            if (now > nextBlink) { blinkUntil = now + .14f; nextBlink = now + Random.Range(3f, 6f); }
            int state = Worried ? 5 : SpeechEmotion.HasValue ? SpriteIndex(SpeechEmotion.Value) :
                Thinking ? 3 : Listening ? 2 : now < blinkUntil ? 1 : 0;
            if (state != displayedState)
            {
                bool blink = (state == 1 || displayedState == 1) && state <= 1 && displayedState <= 1;
                if (!blink) transitionStarted = now;
                displayedState = state;
                Sprite sprite = state == 6 ? helplessExpression : expressions != null && expressions.Length > state ? expressions[state] : null;
                if (sprite != null) portrait.sprite = sprite;
            }
            // Two small damped springs, then a completely steady pose for reading.
            float t = Mathf.Clamp01((now - transitionStarted) / .6f);
            float spring = reducedMotion ? 0f : Mathf.Sin(t * Mathf.PI * 4f) * (1f - t) * (1f - t);
            float strength = state == 4 ? .10f : .065f;
            transform.localScale = Vector3.Scale(restingScale, new Vector3(1f - spring * strength * .55f, 1f + spring * strength, 1f));
            transform.localRotation = restingRotation * Quaternion.Euler(0, 0, spring * (state == 6 ? -5f : 3f));
        }
        private static int SpriteIndex(CompanionEmotion emotion)
        {
            switch (emotion)
            {
                case CompanionEmotion.Happy: return 4;
                case CompanionEmotion.Helpless: return 6;
                case CompanionEmotion.Worried: return 5;
                case CompanionEmotion.Thinking: return 3;
                case CompanionEmotion.Curious: return 2;
                default: return 0;
            }
        }
        private void OnDisable()
        {
            if (!initialized) return;
            SpeechEmotion = null; displayedState = -1;
            transform.localScale = restingScale; transform.localRotation = restingRotation;
        }
    }
}
