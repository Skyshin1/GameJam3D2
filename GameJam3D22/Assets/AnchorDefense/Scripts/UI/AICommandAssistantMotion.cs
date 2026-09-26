using UnityEngine;
using UnityEngine.UI;

namespace AnchorDefense
{
    public enum AICommandAssistantMood
    {
        Idle,
        Listening,
        Thinking,
        Success,
        Failure
    }

    // Presentation only. A single portrait animates immediately; optional frame sets
    // let art replace each state later without changing the command flow.
    [RequireComponent(typeof(Image))]
    public sealed class AICommandAssistantMotion : MonoBehaviour
    {
        [Header("Motion")]
        [SerializeField] private bool dockMode;
        [SerializeField, Min(0f)] private float bobPixels = 5f;
        [SerializeField, Min(0f)] private float bobSpeed = 1.8f;
        [SerializeField, Min(0f)] private float tiltDegrees = 3f;
        [SerializeField, Range(0f, 0.15f)] private float breatheScale = 0.035f;
        [SerializeField, Min(0.1f)] private float responseDuration = 1.35f;
        [SerializeField, Min(1f)] private float frameRate = 8f;

        [Header("Optional Animation Frames")]
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] listeningFrames;
        [SerializeField] private Sprite[] thinkingFrames;
        [SerializeField] private Sprite[] successFrames;
        [SerializeField] private Sprite[] failureFrames;

        private RectTransform rect;
        private Image image;
        private Vector2 basePosition;
        private Vector3 baseScale;
        private Quaternion baseRotation;
        private Color baseColor;
        private Sprite baseSprite;
        private AICommandAssistantMood mood;
        private AICommandAssistantMood returnMood;
        private float moodStartedAt;
        private bool captured;

        public AICommandAssistantMood Mood => mood;

        private void Awake() => CaptureAppearance();

        private void OnEnable()
        {
            if (!captured) CaptureAppearance();
            moodStartedAt = Time.unscaledTime;
        }

        private void OnDisable()
        {
            if (!captured || rect == null || image == null) return;
            rect.anchoredPosition = basePosition;
            rect.localScale = baseScale;
            rect.localRotation = baseRotation;
            image.color = baseColor;
            image.sprite = baseSprite;
        }

        public void CaptureAppearance()
        {
            rect = transform as RectTransform;
            image = GetComponent<Image>();
            if (rect == null || image == null) return;
            basePosition = rect.anchoredPosition;
            baseScale = rect.localScale;
            baseRotation = rect.localRotation;
            baseColor = image.color;
            baseSprite = image.sprite;
            captured = true;
        }

        public void SetMood(AICommandAssistantMood nextMood)
        {
            if (dockMode) return;
            mood = nextMood;
            moodStartedAt = Time.unscaledTime;
        }

        public void React(bool success, AICommandAssistantMood afterward = AICommandAssistantMood.Listening)
        {
            if (dockMode) return;
            returnMood = afterward;
            SetMood(success ? AICommandAssistantMood.Success : AICommandAssistantMood.Failure);
        }

        private void Update()
        {
            if (!captured || rect == null || image == null) return;
            float now = Time.unscaledTime;
            float age = now - moodStartedAt;
            if ((mood == AICommandAssistantMood.Success || mood == AICommandAssistantMood.Failure) &&
                age >= responseDuration)
            {
                SetMood(returnMood);
                age = 0f;
            }

            float phase = now * bobSpeed;
            float bob = Mathf.Sin(phase) * bobPixels;
            float tilt = Mathf.Sin(phase * 0.7f) * tiltDegrees;
            float breathe = 1f + Mathf.Sin(phase * 1.2f) * breatheScale;
            float horizontal = 0f;
            Color tint = baseColor;

            if (!dockMode)
            {
                switch (mood)
                {
                    case AICommandAssistantMood.Listening:
                        tilt -= 4f;
                        horizontal = 2f;
                        break;
                    case AICommandAssistantMood.Thinking:
                        bob += Mathf.Sin(now * 7f) * 2f;
                        tilt += Mathf.Sin(now * 5f) * 5f;
                        breathe += 0.035f;
                        tint = Color.Lerp(baseColor, new Color(0.74f, 0.96f, 1f, baseColor.a), 0.32f);
                        break;
                    case AICommandAssistantMood.Success:
                        float bounce = Mathf.Exp(-age * 3f) * Mathf.Abs(Mathf.Sin(age * 11f));
                        bob += bounce * 17f;
                        breathe += bounce * 0.12f;
                        tilt += bounce * 8f;
                        tint = Color.Lerp(baseColor, new Color(1f, 0.88f, 0.52f, baseColor.a),
                            bounce * 0.65f);
                        break;
                    case AICommandAssistantMood.Failure:
                        float shake = Mathf.Exp(-age * 5f) * Mathf.Sin(age * 35f);
                        horizontal = shake * 7f;
                        tilt += shake * 5f;
                        tint = Color.Lerp(baseColor, new Color(1f, 0.65f, 0.7f, baseColor.a),
                            Mathf.Abs(shake) * 0.55f);
                        break;
                }
            }

            float smoothing = 1f - Mathf.Exp(-15f * Time.unscaledDeltaTime);
            Vector2 targetPosition = basePosition + new Vector2(horizontal, bob);
            Vector3 targetScale = Vector3.Scale(baseScale, Vector3.one * breathe);
            Quaternion targetRotation = baseRotation * Quaternion.Euler(0f, 0f, tilt);
            rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, targetPosition, smoothing);
            rect.localScale = Vector3.Lerp(rect.localScale, targetScale, smoothing);
            rect.localRotation = Quaternion.Slerp(rect.localRotation, targetRotation, smoothing);
            // The dock image is a Button target graphic; leave its hover/focus tint alone.
            if (!dockMode) image.color = Color.Lerp(image.color, tint, smoothing);

            Sprite[] frames = GetFrames();
            if (frames != null && frames.Length > 0)
            {
                int index = Mathf.FloorToInt(age * frameRate) % frames.Length;
                image.sprite = frames[index] != null ? frames[index] : baseSprite;
            }
            else if (image.sprite != baseSprite)
            {
                image.sprite = baseSprite;
            }
        }

        private Sprite[] GetFrames()
        {
            if (dockMode) return idleFrames;
            switch (mood)
            {
                case AICommandAssistantMood.Listening: return listeningFrames;
                case AICommandAssistantMood.Thinking: return thinkingFrames;
                case AICommandAssistantMood.Success: return successFrames;
                case AICommandAssistantMood.Failure: return failureFrames;
                default: return idleFrames;
            }
        }
    }
}
