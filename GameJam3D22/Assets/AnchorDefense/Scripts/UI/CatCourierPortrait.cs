using UnityEngine;
using UnityEngine.UI;

namespace AnchorDefense
{
    [RequireComponent(typeof(Image))]
    public sealed class CatCourierPortrait : MonoBehaviour
    {
        [Tooltip("Idle, blink, listening, thinking, proud, worried.")]
        public Sprite[] expressions;
        public bool reducedMotion;
        private Image portrait;
        private float nextBlink, blinkUntil, reactionUntil;
        private bool proud = true;
        public bool Listening { get; set; }
        public bool Thinking { get; set; }
        public bool Worried { get; set; }
        private void Awake() { portrait = GetComponent<Image>(); nextBlink = Time.unscaledTime + 3f; }
        public void React(bool positive) { proud = positive; reactionUntil = Time.unscaledTime + 1.6f; }
        private void Update()
        {
            if (portrait == null) portrait = GetComponent<Image>();
            float now = Time.unscaledTime;
            if (now > nextBlink) { blinkUntil = now + .14f; nextBlink = now + Random.Range(3f, 6f); }
            int state = Worried ? 5 : now < reactionUntil ? (proud ? 4 : 5) : Thinking ? 3 : Listening ? 2 : now < blinkUntil ? 1 : 0;
            if (expressions != null && expressions.Length > state && expressions[state] != null) portrait.sprite = expressions[state];
            // Resting paws stay near the screen edge. No perpetual whole-character floating.
            float bounce = !reducedMotion && now < reactionUntil ? Mathf.Sin((reactionUntil - now) * 9f) * .025f : 0f;
            transform.localScale = new Vector3(1f + bounce, 1f - bounce * .5f, 1f);
        }
        private void OnDisable() => transform.localScale = Vector3.one;
    }
}
